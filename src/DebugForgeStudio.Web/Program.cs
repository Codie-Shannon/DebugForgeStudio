using DebugForgeStudio.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ScanEngine>();
builder.Services.AddSingleton<InvestigationEngine>();

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

app.MapPost(
    "/api/reproduction",
    (ReproductionRequest request, InvestigationEngine engine) =>
        Results.Ok(engine.BuildSteps(request.Actions)));

app.MapPost(
    "/api/compare",
    (ComparisonRequest request, InvestigationEngine engine) =>
        Results.Ok(engine.Compare(request.Working, request.Broken)));

app.MapFallbackToFile("overview.html");
app.Run();

public sealed record ScanRequest(IReadOnlyList<string> Lines, int ContextRadius = 1);
public sealed record ReproductionRequest(IReadOnlyList<string> Actions);
public sealed record ComparisonRequest(IReadOnlyList<string> Working, IReadOnlyList<string> Broken);
public partial class Program { }
