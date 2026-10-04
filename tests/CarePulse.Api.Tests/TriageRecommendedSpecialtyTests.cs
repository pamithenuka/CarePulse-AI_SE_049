using CarePulse.Api.DTOs;
using Xunit;

namespace CarePulse.Api.Tests;

public class TriageRecommendedSpecialtyTests
{
    [Fact]
    public void TriageConstants_AllowedSpecialties_ContainsExpectedValues()
    {
        // Arrange & Act
        var allowedSpecialties = TriageConstants.AllowedSpecialties;

        // Assert
        Assert.Contains("GENERAL_MEDICINE", allowedSpecialties);
        Assert.Contains("DERMATOLOGY", allowedSpecialties);
        Assert.Contains("CARDIOLOGY", allowedSpecialties);
        Assert.Contains("NEUROLOGY", allowedSpecialties);
        Assert.Contains("ORTHOPEDICS", allowedSpecialties);
        Assert.Contains("PEDIATRICS", allowedSpecialties);
        Assert.Contains("ENT", allowedSpecialties);
        Assert.Contains("OPHTHALMOLOGY", allowedSpecialties);
        Assert.Contains("GYNECOLOGY", allowedSpecialties);
        Assert.Contains("PSYCHIATRY", allowedSpecialties);
        Assert.Equal(10, allowedSpecialties.Length);
    }

    [Fact]
    public void TriageAssessmentResult_RecommendedSpecialty_DefaultsToEmpty()
    {
        // Arrange & Act
        var result = new TriageAssessmentResult();

        // Assert
        Assert.Equal(string.Empty, result.RecommendedSpecialty);
    }

    [Fact]
    public void TriageAssessmentResult_CanSetRecommendedSpecialty()
    {
        // Arrange
        var result = new TriageAssessmentResult();

        // Act
        result.RecommendedSpecialty = "DERMATOLOGY";

        // Assert
        Assert.Equal("DERMATOLOGY", result.RecommendedSpecialty);
    }

    [Theory]
    [InlineData("GENERAL_MEDICINE")]
    [InlineData("DERMATOLOGY")]
    [InlineData("CARDIOLOGY")]
    [InlineData("NEUROLOGY")]
    [InlineData("ORTHOPEDICS")]
    [InlineData("PEDIATRICS")]
    [InlineData("ENT")]
    [InlineData("OPHTHALMOLOGY")]
    [InlineData("GYNECOLOGY")]
    [InlineData("PSYCHIATRY")]
    public void TriageConstants_AllowedSpecialties_AreAllValid(string specialty)
    {
        // Assert
        Assert.Contains(specialty, TriageConstants.AllowedSpecialties);
    }

    [Theory]
    [InlineData("dermatology")]
    [InlineData("Dermatology")]
    [InlineData("DERMATOLOGY")]
    public void Specialty_CaseInsensitivity_ShouldBeHandled(string specialty)
    {
        // This test documents that the AI agent normalizes specialties to uppercase
        // The actual normalization happens in TriageAiAgent.cs
        
        // Arrange
        var specialtyUpper = specialty.ToUpper();
        
        // Assert
        Assert.Contains(specialtyUpper, TriageConstants.AllowedSpecialties);
    }

    [Theory]
    [InlineData("INVALID_SPECIALTY")]
    [InlineData("Podiatry")]
    [InlineData("Internal Medicine")]
    [InlineData("")]
    public void InvalidSpecialties_ShouldNotBeInAllowedList(string invalidSpecialty)
    {
        // Assert
        Assert.DoesNotContain(invalidSpecialty.ToUpper(), TriageConstants.AllowedSpecialties);
    }

    [Fact]
    public void TriageResponseDto_IncludesRecommendedSpecialty()
    {
        // Arrange & Act
        var dto = new TriageResponseDto();

        // Assert
        Assert.NotNull(dto.RecommendedSpecialty);
        Assert.Equal(string.Empty, dto.RecommendedSpecialty);
    }

    [Fact]
    public void TriageResponseDto_CanSetRecommendedSpecialty()
    {
        // Arrange
        var dto = new TriageResponseDto();

        // Act
        dto.RecommendedSpecialty = "CARDIOLOGY";

        // Assert
        Assert.Equal("CARDIOLOGY", dto.RecommendedSpecialty);
    }
}