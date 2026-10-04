namespace CarePulse.Api.Services.Common;

public sealed class ApiProblem(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
