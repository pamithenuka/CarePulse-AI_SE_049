using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarePulse.Api.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static CarePulse.Api.DTOs.TriageConstants;

namespace CarePulse.Api.Services;

public class TriageAiAgent : ITriageAiAgent
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly ILogger<TriageAiAgent> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public TriageAiAgent(IConfiguration configuration, ILogger<TriageAiAgent> logger)
    {
        _logger = logger;
        _apiKey = configuration["AI:GeminiApiKey"] ?? "";
        _model = configuration["AI:GeminiModel"] ?? "gemini-3.5-flash";

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini API Key is missing. TriageAiAgent will fail.");
        }
    }

    public async Task<TriageAssessmentResult> AnalyzeSymptomsAsync(TriageSubmitRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("API key not configured.");
            }

            var systemPrompt = @"You are a medical triage domain analysis agent.
Your ONLY job is to analyze the patient's symptoms and output a strictly formatted JSON risk assessment.
You do NOT diagnose diseases. You do NOT book appointments. You do NOT make final medical decisions.

Risk Scoring Rules (1-10):
- LOW (1-3): Minor symptoms, no immediate threat (e.g., mild headache, small cut).
- MEDIUM (4-6): Distressing but non-life-threatening symptoms (e.g., fever, persistent pain).
- HIGH (7-10): Severe, potentially life-threatening symptoms (e.g., severe chest pain, difficulty breathing, stroke signs).

Risk Level mapping MUST be consistent:
- 1-3 = LOW
- 4-6 = MEDIUM
- 7-10 = HIGH

Specialty Selection:
Based on the symptoms, you MUST select the most appropriate medical specialty from these exact values:
- GENERAL_MEDICINE: General health concerns, flu, minor illnesses
- DERMATOLOGY: Skin rashes, itching, moles, skin conditions
- CARDIOLOGY: Chest pain, heart issues, blood pressure concerns
- NEUROLOGY: Headaches, seizures, nerve issues, brain-related symptoms
- ORTHOPEDICS: Bone, joint, muscle injuries, back pain
- PEDIATRICS: Child-specific medical concerns
- ENT: Ear, nose, throat issues
- OPHTHALMOLOGY: Eye-related problems
- GYNECOLOGY: Women's health issues
- PSYCHIATRY: Mental health concerns

You MUST select exactly one specialty from this list. Do not invent or modify specialty names.";

            var userMessage = $@"Analyze these patient details:
Symptoms: {request.Symptoms}
Duration: {(string.IsNullOrWhiteSpace(request.Duration) ? "Unknown" : request.Duration)}
Severity: {(string.IsNullOrWhiteSpace(request.Severity) ? "Unknown" : request.Severity)}
Additional Symptoms: {(request.AdditionalSymptoms != null && request.AdditionalSymptoms.Any() ? string.Join(", ", request.AdditionalSymptoms) : "None")}";

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userMessage } }
                    }
                },
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                generation_config = new
                {
                    response_mime_type = "application/json",
                    response_schema = new
                    {
                        type = "OBJECT",
                        properties = new Dictionary<string, object>
                        {
                            { "RiskScore", new { type = "INTEGER" } },
                            { "RiskLevel", new { type = "STRING", @enum = new[] { "LOW", "MEDIUM", "HIGH" } } },
                            { "Reason", new { type = "STRING" } },
                            { "RecommendedAction", new { type = "STRING" } },
                            { "FollowUpRecommended", new { type = "BOOLEAN" } },
                            { "RecommendedSpecialty", new { type = "STRING", @enum = new[] { "GENERAL_MEDICINE", "DERMATOLOGY", "CARDIOLOGY", "NEUROLOGY", "ORTHOPEDICS", "PEDIATRICS", "ENT", "OPHTHALMOLOGY", "GYNECOLOGY", "PSYCHIATRY" } } }
                        },
                        required = new[] { "RiskScore", "RiskLevel", "Reason", "RecommendedAction", "FollowUpRecommended", "RecommendedSpecialty" }
                    }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            
            var response = await _httpClient.PostAsJsonAsync(url, requestPayload);

            if (!response.IsSuccessStatusCode)
            {
                // Log error details internally but do not expose them
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API returned {StatusCode}. Response: {ErrorBody}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Gemini API request failed with status {response.StatusCode}.");
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            var candidates = responseJson.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("No candidates returned from Gemini.");
            }

            var contentText = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(contentText))
            {
                throw new InvalidOperationException("Empty response content from Gemini.");
            }

            var result = JsonSerializer.Deserialize<TriageAssessmentResult>(contentText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize AI response.");
            }

            // Post-validation: Normalize score and level consistency to avoid hallucinations
            result.RiskScore = Math.Clamp(result.RiskScore, 1, 10);
            
            if (result.RiskScore >= 7) result.RiskLevel = "HIGH";
            else if (result.RiskScore >= 4) result.RiskLevel = "MEDIUM";
            else result.RiskLevel = "LOW";

            // Validate RecommendedSpecialty
            if (string.IsNullOrWhiteSpace(result.RecommendedSpecialty) ||
                !AllowedSpecialties.Contains(result.RecommendedSpecialty.ToUpper()))
            {
                throw new InvalidOperationException($"Invalid specialty '{result.RecommendedSpecialty}'. Must be one of: {string.Join(", ", AllowedSpecialties)}");
            }

            result.RecommendedSpecialty = result.RecommendedSpecialty.ToUpper();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Assessment failed.");
            
            // Fallback strategy: Fail safely into manual review (HIGH Risk)
            // Note: RecommendedSpecialty is left empty as AI assessment failed
            return new TriageAssessmentResult
            {
                RiskScore = 10,
                RiskLevel = "HIGH",
                Reason = "AI assessment unavailable. Manual doctor review is required.",
                RecommendedAction = "Immediate manual review",
                FollowUpRecommended = true,
                RecommendedSpecialty = string.Empty
            };
        }
    }
}
