using CarePulse.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CarePulseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CarePulseDb")));

// Allow the React dev server and Flutter (web/dev) to call this API locally.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
