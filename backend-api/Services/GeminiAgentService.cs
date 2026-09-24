using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Services;

// Agent 3 (Action / Tool Agent), per the project spec: "Uses C# Function
// Calling / EF Core tools to search doctor/clinic availability and match
// patient needs." It never books anything itself (read-only, per the
// project's AI safety rule) - it only searches and describes results in
// plain language. Booking still happens through the normal, tested
// POST /api/v1/appointments/book endpoint, same as before.
public class GeminiAgentService
{
    private readonly ReadOnlyCarePulseDbContext _readOnlyDb;
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    // The ONE tool this agent is allowed to use. Gemini can only ever ask
    // to run this specific search - it cannot invent other actions.
    private const string ToolDefinitionJson = """
    {
      "functionDeclarations": [
        {
          "name": "search_available_slots",
          "description": "Searches for OPEN (bookable) appointment slots. Use this whenever the patient is looking for an appointment, a doctor, or asks about availability.",
          "parameters": {
            "type": "OBJECT",
            "properties": {
              "specialty": {
                "type": "STRING",
                "description": "Medical specialty to filter by, e.g. 'Cardiology'. Omit if the patient didn't mention one."
              },
              "startDate": {
                "type": "STRING",
                "description": "Earliest date to search from, in YYYY-MM-DD format. Defaults to today if omitted."
              },
              "endDate": {
                "type": "STRING",
                "description": "Latest date to search up to, in YYYY-MM-DD format. Defaults to 14 days from startDate if omitted."
              },
              "timeOfDay": {
                "type": "STRING",
                "enum": ["morning", "afternoon", "evening"],
                "description": "Preferred time of day, if the patient mentioned one. Morning = before 12:00, afternoon = 12:00-17:00, evening = after 17:00."
              }
            }
          }
        }
      ]
    }
    """;

    public GeminiAgentService(ReadOnlyCarePulseDbContext readOnlyDb, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _readOnlyDb = readOnlyDb;
        _http = httpClientFactory.CreateClient();
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured in appsettings.json.");
        _model = config["Gemini:Model"] ?? "gemini-2.5-flash";
    }

    public async Task<AgentSearchResponseDto> HandleMessageAsync(string userMessage)
    {
        // Turn 1: send the patient's message plus the tool definition.
        var firstRequestBody = BuildRequestBody(new JsonArray
        {
            BuildContentTurn("user", userMessage)
        });

        var firstResponse = await CallGeminiAsync(firstRequestBody);

        // Grab the WHOLE part that contains the functionCall (not just the
        // functionCall itself) - Gemini 3.x attaches a "thoughtSignature"
        // alongside it that must be echoed back untouched, or the next
        // request is rejected.
        var functionCallPart = ExtractFunctionCallPart(firstResponse);

        // The model decided this wasn't a search request (e.g. patient
        // said "hello") - just relay its plain-text reply, no DB search.
        if (functionCallPart is null)
        {
            var plainText = ExtractText(firstResponse) ?? "Sorry, I didn't understand that. Could you rephrase?";
            return new AgentSearchResponseDto(plainText, new List<SlotSearchResultDto>());
        }

        var functionCall = functionCallPart["functionCall"] as JsonObject
            ?? throw new InvalidOperationException("Malformed functionCall part from Gemini.");

        // Run the REAL search against the real (read-only) database.
        var args = functionCall["args"] as JsonObject ?? new JsonObject();
        var matchingSlots = await SearchAvailableSlotsAsync(
            specialty: args["specialty"]?.GetValue<string>(),
            startDate: args["startDate"]?.GetValue<string>(),
            endDate: args["endDate"]?.GetValue<string>(),
            timeOfDay: args["timeOfDay"]?.GetValue<string>()
        );

        // Turn 2: tell Gemini what the search actually returned, so it can
        // write a natural-language reply grounded in real data.
        var functionResultJson = JsonSerializer.Serialize(new
        {
            resultCount = matchingSlots.Count,
            slots = matchingSlots.Select(s => new
            {
                doctorName = s.DoctorName,
                specialty = s.Specialty,
                date = s.SlotStart.ToString("yyyy-MM-dd"),
                time = s.SlotStart.ToString("HH:mm")
            })
        });

        var secondRequestBody = BuildRequestBody(new JsonArray
        {
            BuildContentTurn("user", userMessage),
            BuildModelFunctionCallTurn(functionCallPart),
            BuildFunctionResponseTurn("search_available_slots", functionResultJson, functionCall["id"]?.GetValue<string>())
        });

        var secondResponse = await CallGeminiAsync(secondRequestBody);
        var finalReply = ExtractText(secondResponse)
            ?? (matchingSlots.Count > 0
                ? $"I found {matchingSlots.Count} matching appointment(s)."
                : "I couldn't find any matching open appointments.");

        return new AgentSearchResponseDto(finalReply, matchingSlots);
    }

    // The actual "tool" - a normal, read-only EF Core query. This is the
    // whole point of function calling: the AI never touches the database
    // itself, it only ever asks OUR code to run THIS specific method.
    private async Task<List<SlotSearchResultDto>> SearchAvailableSlotsAsync(
        string? specialty, string? startDate, string? endDate, string? timeOfDay)
    {
        var query = _readOnlyDb.AppointmentSlots
            .Include(s => s.Doctor)
            .Where(s => s.Status == SlotStatus.Open);

        if (!string.IsNullOrWhiteSpace(specialty))
        {
            query = query.Where(s => s.Doctor!.Specialty.ToLower() == specialty.ToLower());
        }

        var start = DateTime.TryParse(startDate, out var parsedStart)
            ? DateTime.SpecifyKind(parsedStart.Date, DateTimeKind.Utc)
            : DateTime.UtcNow.Date;

        var end = DateTime.TryParse(endDate, out var parsedEnd)
            ? DateTime.SpecifyKind(parsedEnd.Date.AddDays(1), DateTimeKind.Utc)
            : start.AddDays(14);

        query = query.Where(s => s.SlotStart >= start && s.SlotStart < end);

        var results = await query
            .OrderBy(s => s.SlotStart)
            .Take(20)
            .Select(s => new SlotSearchResultDto(
                s.Id, s.DoctorId, s.Doctor!.FullName, s.Doctor!.Specialty, s.SlotStart, s.SlotEnd))
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(timeOfDay))
        {
            results = timeOfDay.ToLower() switch
            {
                "morning" => results.Where(s => s.SlotStart.Hour < 12).ToList(),
                "afternoon" => results.Where(s => s.SlotStart.Hour is >= 12 and < 17).ToList(),
                "evening" => results.Where(s => s.SlotStart.Hour >= 17).ToList(),
                _ => results
            };
        }

        return results.Take(10).ToList();
    }

    // ---- Gemini request/response plumbing below ----

    private JsonObject BuildRequestBody(JsonArray contents)
    {
        return new JsonObject
        {
            ["contents"] = contents,
            ["tools"] = new JsonArray { JsonNode.Parse(ToolDefinitionJson) }
        };
    }

    private static JsonObject BuildContentTurn(string role, string text) => new()
    {
        ["role"] = role,
        ["parts"] = new JsonArray { new JsonObject { ["text"] = text } }
    };

    private static JsonObject BuildModelFunctionCallTurn(JsonObject functionCallPart) => new()
    {
        ["role"] = "model",
        ["parts"] = new JsonArray { functionCallPart.DeepClone() }
    };

    private static JsonObject BuildFunctionResponseTurn(string functionName, string resultJson, string? functionCallId)
    {
        var functionResponseObj = new JsonObject
        {
            ["name"] = functionName,
            ["response"] = JsonNode.Parse(resultJson)
        };
        if (!string.IsNullOrEmpty(functionCallId))
        {
            functionResponseObj["id"] = functionCallId;
        }

        return new JsonObject
        {
            // Gemini 3.x requires the function result to be sent back as a
            // "user" turn (not "function" - that role name was retired),
            // and the "id" must match the original functionCall's id.
            ["role"] = "user",
            ["parts"] = new JsonArray
            {
                new JsonObject { ["functionResponse"] = functionResponseObj }
            }
        };
    }

    private async Task<JsonObject> CallGeminiAsync(JsonObject requestBody)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini API error ({response.StatusCode}): {responseText}");
        }

        return JsonNode.Parse(responseText) as JsonObject
            ?? throw new InvalidOperationException("Gemini returned an unexpected response shape.");
    }

    private static JsonObject? ExtractFunctionCallPart(JsonObject response)
    {
        var parts = response["candidates"]?[0]?["content"]?["parts"] as JsonArray;
        foreach (var part in parts ?? new JsonArray())
        {
            if (part is JsonObject partObj && partObj["functionCall"] is JsonObject) return partObj;
        }
        return null;
    }

    private static string? ExtractText(JsonObject response)
    {
        var parts = response["candidates"]?[0]?["content"]?["parts"] as JsonArray;
        foreach (var part in parts ?? new JsonArray())
        {
            if (part?["text"] is JsonValue textVal) return textVal.GetValue<string>();
        }
        return null;
    }
}