using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace CarePulse.Api.Tests.Fakes;

public class FakeWebHostEnvironment : IWebHostEnvironment
{
    public FakeWebHostEnvironment()
    {
        ContentRootPath = Path.Combine(Path.GetTempPath(), "carepulse-tests", Guid.NewGuid().ToString());
        WebRootPath = Path.Combine(ContentRootPath, "wwwroot");
        Directory.CreateDirectory(WebRootPath);
    }

    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "CarePulse.Api.Tests";
    public string WebRootPath { get; set; }
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
