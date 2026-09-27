using System;
using System.Threading.Tasks;
using CarePulse.Api.DTOs;
using CarePulse.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TestApp;

// Simple console logger to see log output
class ConsoleLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        if (exception != null) Console.WriteLine($"  Exception: {exception.Message}");
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Gemini AI Agent Independent Test ===");
        
        var config = new ConfigurationBuilder()
            .AddUserSecrets("00a4b333-2ef1-480c-9acf-3f532ccf0bc1")
            .Build();

        // Verify config is loaded
        var apiKey = config["AI:GeminiApiKey"];
        Console.WriteLine($"API Key loaded: {!string.IsNullOrWhiteSpace(apiKey)} (length: {apiKey?.Length ?? 0})");
        var model = config["AI:GeminiModel"] ?? "gemini-2.5-flash";
        Console.WriteLine($"Model: {model}");

        var logger = new ConsoleLogger<TriageAiAgent>();
        var agent = new TriageAiAgent(config, logger);

        Console.WriteLine("\n--- Input 1: LOW Risk (Mild headache) ---");
        var lowResult = await agent.AnalyzeSymptomsAsync(new TriageSubmitRequestDto { Symptoms = "Mild headache since this morning." });
        Console.WriteLine($"  Score: {lowResult.RiskScore}, Level: {lowResult.RiskLevel}");
        Console.WriteLine($"  Reason: {lowResult.Reason}");

        Console.WriteLine("\n--- Input 2: MEDIUM Risk (Fever and headache) ---");
        var medResult = await agent.AnalyzeSymptomsAsync(new TriageSubmitRequestDto { Symptoms = "Fever and headache for two days." });
        Console.WriteLine($"  Score: {medResult.RiskScore}, Level: {medResult.RiskLevel}");
        Console.WriteLine($"  Reason: {medResult.Reason}");

        Console.WriteLine("\n--- Input 3: HIGH Risk (Severe chest pain) ---");
        var highResult = await agent.AnalyzeSymptomsAsync(new TriageSubmitRequestDto { Symptoms = "Severe chest pain and difficulty breathing." });
        Console.WriteLine($"  Score: {highResult.RiskScore}, Level: {highResult.RiskLevel}");
        Console.WriteLine($"  Reason: {highResult.Reason}");

        Console.WriteLine("\n=== Test Complete ===");
    }
}
