using System.Net;
using System.Text.Json;
using CarePulse.Api.Data;
using CarePulse.Api.Entities;
using CarePulse.Api.Services;
using CarePulse.Api.Services.Common;
using CarePulse.Api.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CarePulse.Api.Tests;

[Collection("Database collection")]
public class SchedulingSearchTests(TestDatabaseFixture database)
{
    private sealed class GeminiReply(string day, string timeOfDay) : HttpMessageHandler, IHttpClientFactory
    {
        private int count;
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            object part = count++ == 0 ? new { functionCall = new { name = "search_available_slots", args = new {
                specialty = "ENT", startDate = day, endDate = day, timeOfDay } } } : new { text = "Synthetic slot search result" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new {
                candidates = new[] { new { content = new { parts = new[] { part } } } }
            })) });
        }
    }

    [Theory]
    [InlineData("morning", 1)]
    [InlineData("evening", 18)]
    public async Task ToolSearch_UsesClinicDates_FiltersBeforeLimit_AndExcludesBookedSlots(string timeOfDay, int hour)
    {
        using var db = database.CreateContext();
        var doctor = new DoctorProfile { FullName = "Synthetic ENT", Specialty = "ENT", UserId = Guid.NewGuid().ToString() };
        db.DoctorProfiles.Add(doctor);
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var expectedStart = ClinicTime.ToUtc(day, TimeSpan.FromHours(hour));
        var wanted = new AppointmentSlot { DoctorId = doctor.Id, SlotStart = expectedStart, SlotEnd = expectedStart.AddMinutes(10) };
        var bookedStart = ClinicTime.ToUtc(day, TimeSpan.FromHours(hour + 1));
        var booked = new AppointmentSlot { DoctorId = doctor.Id, SlotStart = bookedStart, SlotEnd = bookedStart.AddMinutes(10), Status = SlotStatus.Booked };
        db.AppointmentSlots.AddRange(wanted, booked);
        for (var i = 0; i < 25; i++)
        {
            var start = ClinicTime.ToUtc(day, TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(i * 10)));
            db.AppointmentSlots.Add(new AppointmentSlot { DoctorId = doctor.Id, SlotStart = start, SlotEnd = start.AddMinutes(10) });
        }
        await db.SaveChangesAsync();
        using var reader = new ReadOnlyCarePulseDbContext(new DbContextOptionsBuilder<CarePulseDbContext>().UseNpgsql(database.ConnectionString).Options, new FakeCurrentUserService());
        using var transport = new GeminiReply(day.ToString("yyyy-MM-dd"), timeOfDay);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["AI:GeminiApiKey"] = "synthetic-key" }).Build();
        var result = await new GeminiAgentService(reader, transport, config).HandleMessageAsync("Synthetic availability query");
        Assert.Contains(result.MatchingSlots, s => s.SlotId == wanted.Id);
        Assert.DoesNotContain(result.MatchingSlots, s => s.SlotId == booked.Id);
        Assert.All(result.MatchingSlots, s => Assert.Equal(day, DateOnly.FromDateTime(s.SlotStart.AddMinutes(330))));
    }
}
