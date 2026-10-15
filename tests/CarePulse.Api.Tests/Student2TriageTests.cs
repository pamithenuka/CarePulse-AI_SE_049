using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using CarePulse.Api.Services;
using CarePulse.Api.Services.Common;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CarePulse.Api.Tests;

public class Student2TriageTests : IDisposable
{
    private readonly CarePulseDbContext _dbContext;
    private readonly Mock<ITriageAiAgent> _mockAiAgent;
    private readonly TriageService _triageService;

    public Student2TriageTests()
    {
        var options = new DbContextOptionsBuilder<CarePulseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid().ToString());

        _dbContext = new CarePulseDbContext(options, mockCurrentUserService.Object);
        _mockAiAgent = new Mock<ITriageAiAgent>();
        _triageService = new TriageService(_dbContext, _mockAiAgent.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task SubmitTriage_NormalLowRisk_ReturnsCompletedStatus_Normal()
    {
        // Arrange
        var request = new TriageSubmitRequestDto { PatientProfileId = Guid.NewGuid(), Symptoms = "Mild headache", Latitude = 0, Longitude = 0 };
        _mockAiAgent.Setup(a => a.AnalyzeSymptomsAsync(request)).ReturnsAsync(new TriageAssessmentResult
        {
            RiskScore = 2,
            RiskLevel = "LOW",
            Reason = "Minor symptoms",
            RecommendedAction = "SELF_CARE_MONITORING",
            RecommendedSpecialty = "GENERAL_MEDICINE"
        });

        // Act
        var result = await _triageService.SubmitTriageAsync(request);

        // Assert
        Assert.Equal("COMPLETED", result.Status);
        Assert.False(result.RequiresDoctorApproval);
        Assert.Equal(2, result.RiskScore);
        Assert.Equal("LOW", result.RiskLevel);
        Assert.Equal("GENERAL_MEDICINE", result.RecommendedSpecialty);

        var queueItem = await _dbContext.ApprovalQueues.FirstOrDefaultAsync(q => q.TriageTicketId == result.Id);
        Assert.Null(queueItem);
    }

    [Fact]
    public async Task SubmitTriage_NormalMediumRisk_ReturnsConsultationRecommended_Normal()
    {
        // Arrange
        var request = new TriageSubmitRequestDto { PatientProfileId = Guid.NewGuid(), Symptoms = "Fever and persistent pain", Latitude = 0, Longitude = 0 };
        _mockAiAgent.Setup(a => a.AnalyzeSymptomsAsync(request)).ReturnsAsync(new TriageAssessmentResult
        {
            RiskScore = 5,
            RiskLevel = "MEDIUM",
            Reason = "Distressing symptoms",
            RecommendedAction = "DOCTOR_CONSULTATION",
            RecommendedSpecialty = "GENERAL_MEDICINE"
        });

        // Act
        var result = await _triageService.SubmitTriageAsync(request);

        // Assert
        Assert.Equal("DOCTOR_CONSULTATION_RECOMMENDED", result.Status);
        Assert.False(result.RequiresDoctorApproval);
        Assert.Equal(5, result.RiskScore);
        Assert.Equal("MEDIUM", result.RiskLevel);

        var queueItem = await _dbContext.ApprovalQueues.FirstOrDefaultAsync(q => q.TriageTicketId == result.Id);
        Assert.Null(queueItem);
    }

    [Fact]
    public async Task SubmitTriage_HighRiskEmergency_ReturnsNeedsApproval_Normal()
    {
        // Arrange
        var request = new TriageSubmitRequestDto { PatientProfileId = Guid.NewGuid(), Symptoms = "Severe chest pain", Latitude = 0, Longitude = 0 };
        _mockAiAgent.Setup(a => a.AnalyzeSymptomsAsync(request)).ReturnsAsync(new TriageAssessmentResult
        {
            RiskScore = 9,
            RiskLevel = "HIGH",
            Reason = "Life-threatening symptoms",
            RecommendedAction = "DOCTOR_APPROVAL_REQUIRED",
            RecommendedSpecialty = "CARDIOLOGY"
        });

        // Act
        var result = await _triageService.SubmitTriageAsync(request);

        // Assert
        Assert.Equal("NEEDS_DOCTOR_APPROVAL", result.Status);
        Assert.True(result.RequiresDoctorApproval);
        Assert.Equal(9, result.RiskScore);
        Assert.Equal("HIGH", result.RiskLevel);
        Assert.Equal("CARDIOLOGY", result.RecommendedSpecialty);
    }

    [Fact]
    public async Task SubmitTriage_HighRisk_EnforcesDoctorApproval_AndCreatesQueue_Boundary()
    {
        // Arrange
        var request = new TriageSubmitRequestDto { PatientProfileId = Guid.NewGuid(), Symptoms = "Severe pain", Latitude = 0, Longitude = 0 };
        _mockAiAgent.Setup(a => a.AnalyzeSymptomsAsync(request)).ReturnsAsync(new TriageAssessmentResult
        {
            RiskScore = 7, // Boundary for High
            RiskLevel = "HIGH",
            Reason = "Severe pain",
            RecommendedAction = "DOCTOR_APPROVAL_REQUIRED",
            RecommendedSpecialty = "GENERAL_MEDICINE"
        });

        // Act
        var result = await _triageService.SubmitTriageAsync(request);

        // Assert
        Assert.True(result.RequiresDoctorApproval);
        var queueItem = await _dbContext.ApprovalQueues.FirstOrDefaultAsync(q => q.TriageTicketId == result.Id);
        Assert.NotNull(queueItem);
        Assert.Equal("PENDING", queueItem.ReviewStatus);
    }

    [Fact]
    public void Validator_InvalidOrEmptySymptoms_ReturnsErrors_Invalid()
    {
        // Arrange
        var validator = new TriageSubmitValidator();
        var request = new TriageSubmitRequestDto { PatientProfileId = Guid.NewGuid(), Symptoms = "", Latitude = 0, Longitude = 0 };
        
        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Symptoms");
    }

    [Fact]
    public async Task AiAgentFallback_ProviderFailure_ReturnsSafeFallback_Failure()
    {
        // Arrange
        var request = new TriageSubmitRequestDto { PatientProfileId = Guid.NewGuid(), Symptoms = "Unknown", Latitude = 0, Longitude = 0 };
        
        // Simulating AiAgent fallback manually since we mock it, 
        // but let's test how Service handles the fallback result:
        _mockAiAgent.Setup(a => a.AnalyzeSymptomsAsync(request)).ReturnsAsync(new TriageAssessmentResult
        {
            AssessmentFailed = true,
            RiskScore = 10,
            RiskLevel = "HIGH",
            Reason = "AI assessment unavailable. Manual doctor review is required.",
            RecommendedAction = "Immediate manual review",
            FollowUpRecommended = true,
            RecommendedSpecialty = string.Empty
        });

        // Act
        var result = await _triageService.SubmitTriageAsync(request);

        // Assert
        Assert.True(result.AssessmentFailed);
        Assert.Equal(10, result.RiskScore);
        Assert.Equal("HIGH", result.RiskLevel);
        Assert.Equal("NEEDS_DOCTOR_APPROVAL", result.Status);
        Assert.True(result.RequiresDoctorApproval);
        Assert.Equal(string.Empty, result.RecommendedSpecialty);
        
        var queueItem = await _dbContext.ApprovalQueues.FirstOrDefaultAsync(q => q.TriageTicketId == result.Id);
        Assert.NotNull(queueItem);
    }

    [Fact]
    public async Task DoctorApproval_ValidTicket_UpdatesStatusAndQueue_Normal()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var doctorId = "doc-123";
        var ticket = new TriageTicket { Id = ticketId, Status = "NEEDS_DOCTOR_APPROVAL", RequiresDoctorApproval = true };
        var queue = new ApprovalQueue { TriageTicketId = ticketId, ReviewStatus = "PENDING" };
        _dbContext.TriageTickets.Add(ticket);
        _dbContext.ApprovalQueues.Add(queue);
        await _dbContext.SaveChangesAsync();

        // Act
        var request = new ApproveTriageRequestDto { Notes = "Looks good" };
        var result = await _triageService.ApproveTriageAsync(ticketId, request, doctorId);

        // Assert
        Assert.True(result);
        var updatedTicket = await _dbContext.TriageTickets.Include(t => t.ApprovalQueue).FirstAsync(t => t.Id == ticketId);
        Assert.Equal("APPROVED_BY_DOCTOR", updatedTicket.Status);
        Assert.Equal("APPROVED", updatedTicket.ApprovalQueue.ReviewStatus);
        Assert.Equal(doctorId, updatedTicket.ApprovalQueue.ReviewedByDoctorId);
    }

    [Fact]
    public async Task DoctorRejection_ValidTicket_UpdatesStatusAndQueue_Normal()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var doctorId = "doc-123";
        var ticket = new TriageTicket { Id = ticketId, Status = "NEEDS_DOCTOR_APPROVAL", RequiresDoctorApproval = true };
        var queue = new ApprovalQueue { TriageTicketId = ticketId, ReviewStatus = "PENDING" };
        _dbContext.TriageTickets.Add(ticket);
        _dbContext.ApprovalQueues.Add(queue);
        await _dbContext.SaveChangesAsync();

        // Act
        var request = new ApproveTriageRequestDto { Notes = "Cannot treat here" };
        var result = await _triageService.RejectTriageAsync(ticketId, request, doctorId);

        // Assert
        Assert.True(result);
        var updatedTicket = await _dbContext.TriageTickets.Include(t => t.ApprovalQueue).FirstAsync(t => t.Id == ticketId);
        Assert.Equal("REJECTED", updatedTicket.Status);
        Assert.Equal("REJECTED", updatedTicket.ApprovalQueue.ReviewStatus);
    }

    [Fact]
    public async Task DoctorApproval_DuplicateOrInvalid_ReturnsFalse_Invalid()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var doctorId = "doc-123";
        var ticket = new TriageTicket { Id = ticketId, Status = "APPROVED_BY_DOCTOR", RequiresDoctorApproval = true };
        var queue = new ApprovalQueue { TriageTicketId = ticketId, ReviewStatus = "APPROVED" };
        _dbContext.TriageTickets.Add(ticket);
        _dbContext.ApprovalQueues.Add(queue);
        await _dbContext.SaveChangesAsync();

        // Act
        var request = new ApproveTriageRequestDto { Notes = "Already done" };
        var result = await _triageService.ApproveTriageAsync(ticketId, request, doctorId);

        // Assert
        Assert.False(result); // Cannot approve again
    }
}
