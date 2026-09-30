using System;
using System.Threading.Tasks;
using CarePulse.Api.Data;
using CarePulse.Api.Services.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CarePulse.Ai.GoldenTests;

public class ValidationAgentGoldenTests
{
    private class DummyCurrentUserService : CarePulse.Api.Services.Common.ICurrentUserService
    {
        public string? UserId => "test-user";
    }

    private CarePulseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CarePulseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CarePulseDbContext(options, new DummyCurrentUserService());
    }

    private IConfiguration GetConfiguration(string? apiKey = null)
    {
        var builder = new ConfigurationBuilder();
        if (apiKey != null)
        {
            builder.AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("AI:GeminiApiKey", apiKey)
            });
        }
        return builder.Build();
    }

    [Fact]
    public async Task EvaluateDispatchSafetyAsync_FallbackRecovery_EnforcesDoctorApproval()
    {
        // Arrange: No API key provided, triggering fallback recovery
        var dbContext = GetInMemoryDbContext();
        var config = GetConfiguration(apiKey: null);
        var logger = NullLogger<ValidationAgent>.Instance;
        
        var agent = new ValidationAgent(dbContext, config, logger);
        
        var request = new ValidationAgentRequest
        {
            TriageId = Guid.NewGuid(),
            SeverityScore = 9.0, // High severity, should flag rules
            EtaMinutes = 45,     // High ETA, should flag rules
            RecommendedNurseId = Guid.NewGuid()
        };

        // Act
        var response = await agent.EvaluateDispatchSafetyAsync(request);

        // Assert
        Assert.True(response.RequiresHumanApproval, "Fallback recovery must strictly enforce Doctor Approval.");
        Assert.Contains(response.FlaggedRules, r => r.Contains("Severity Score"));
        Assert.Contains(response.FlaggedRules, r => r.Contains("ETA"));
        Assert.Contains("Fallback Execution", response.VerdictReason);
    }

    [Fact]
    public async Task EvaluateDispatchSafetyAsync_MissingConfiguration_LowRiskStillRequiresApproval()
    {
        // Deterministic low-risk fallback; adversarial text is covered in planner tests.
        var dbContext = GetInMemoryDbContext();
        var config = GetConfiguration(apiKey: null);
        var logger = NullLogger<ValidationAgent>.Instance;
        
        var agent = new ValidationAgent(dbContext, config, logger);
        
        var request = new ValidationAgentRequest
        {
            TriageId = Guid.NewGuid(),
            SeverityScore = 1.0, 
            EtaMinutes = 5,
            RecommendedNurseId = Guid.NewGuid()
        };

        // Act
        var response = await agent.EvaluateDispatchSafetyAsync(request);

        Assert.True(response.RequiresHumanApproval, "Low-risk scores cannot remove the approval gate.");
    }
}
