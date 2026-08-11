using DebugForgeStudio.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ScanEngine>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    project = "DebugForge Studio",
    mode = "Synthetic generated baseline"
}));

app.MapPost(
    "/api/scan",
    (ScanRequest request, ScanEngine engine) =>
        Results.Ok(engine.Scan(request.Lines, request.ContextRadius)));

app.MapFallbackToFile("overview.html");
app.Run();

public sealed record ScanRequest(IReadOnlyList<string> Lines, int ContextRadius = 1);
public partial class Program { }
