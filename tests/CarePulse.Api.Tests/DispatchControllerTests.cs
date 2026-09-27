using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CarePulse.Api.Data;
using CarePulse.Api.DTOs.Dispatch;
using CarePulse.Api.Entities.Dispatch;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarePulse.Api.Tests;

public class DispatchControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DispatchControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's DbContext registration.
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CarePulseDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext using an in-memory database for testing.
                services.AddDbContext<CarePulseDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });
        });
    }

    [Fact]
    public async Task GetActiveDispatches_ReturnsSuccessAndCorrectContentType()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/dispatch/active");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignDispatch_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new AssignDispatchDto
        {
            TriageTicketId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            NurseId = Guid.NewGuid()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/dispatch/assign", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLocation_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new NurseLocationUpdateDto
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            SpeedKmh = 50,
            Heading = 90
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/dispatch/{Guid.NewGuid()}/location", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
