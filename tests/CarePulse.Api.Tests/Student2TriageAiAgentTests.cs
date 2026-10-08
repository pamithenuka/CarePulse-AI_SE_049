using CarePulse.Api.DTOs;
using CarePulse.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace CarePulse.Api.Tests;

public class Student2TriageAiAgentTests
{
    [Fact]
    public async Task AiAgent_MissingConfiguration_ReturnsFallbackResult_Failure()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["AI:GeminiApiKey"]).Returns(string.Empty);
        
        var mockLogger = new Mock<ILogger<TriageAiAgent>>();
        
        var agent = new TriageAiAgent(mockConfig.Object, mockLogger.Object);
        var request = new TriageSubmitRequestDto { Symptoms = "Headache", Latitude = 0, Longitude = 0 };

        // Act
        var result = await agent.AnalyzeSymptomsAsync(request);

        // Assert
        Assert.True(result.AssessmentFailed);
        Assert.Equal(10, result.RiskScore);
        Assert.Equal("HIGH", result.RiskLevel);
        Assert.Equal("Immediate manual review", result.RecommendedAction);
        Assert.Equal(string.Empty, result.RecommendedSpecialty);
        Assert.True(result.FollowUpRecommended);
    }
}
