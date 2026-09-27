using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CarePulse.Api.Data;

var optionsBuilder = new DbContextOptionsBuilder<CarePulseDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=carepulse_dev_db;Username=postgres;Password=6629");

using var context = new CarePulseDbContext(optionsBuilder.Options);

var ticketId = Guid.Parse(System.IO.File.ReadAllText("persisted_ticket.txt").Trim());
Console.WriteLine($"Looking for logs for Ticket ID: {ticketId}");

var rawLogs = context.Database.SqlQueryRaw<int>($"SELECT count(*) as Value FROM \"AiTriageLogs\" WHERE \"TriageTicketId\" = '{ticketId}'").ToList();
Console.WriteLine($"Raw SQL Count: {rawLogs[0]}");

var logs = context.AiTriageLogs.IgnoreQueryFilters().Where(l => l.TriageTicketId == ticketId).ToList();
Console.WriteLine($"EF Core Count: {logs.Count}");

var softDeletedLogs = context.AiTriageLogs.IgnoreQueryFilters().Where(l => l.TriageTicketId == ticketId && l.IsDeleted).ToList();
Console.WriteLine($"Soft Deleted logs: {softDeletedLogs.Count}");
