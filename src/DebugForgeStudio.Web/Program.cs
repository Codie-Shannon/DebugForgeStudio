using DebugForgeStudio.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ScanEngine>();
builder.Services.AddSingleton<InvestigationEngine>();
builder.Services.AddSingleton<ReportEngine>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    project = "DebugForge Studio",
    version = "0.1.0",
    mode = "Synthetic generated baseline"
}));

app.MapPost("/api/scan", (ScanRequest request, ScanEngine engine) =>
    Results.Ok(engine.Scan(request.Lines, request.ContextRadius)));

app.MapPost("/api/reproduction", (ReproductionRequest request, InvestigationEngine engine) =>
    Results.Ok(engine.BuildSteps(request.Actions)));

app.MapPost("/api/compare", (ComparisonRequest request, InvestigationEngine engine) =>
    Results.Ok(engine.Compare(request.Working, request.Broken)));

app.MapPost("/api/report/markdown", (DebugReport report, ReportEngine engine) =>
    Results.Text(engine.ToMarkdown(report), "text/markdown"));

app.MapPost("/api/report/json", (DebugReport report, ReportEngine engine) =>
    Results.Text(engine.ToJson(report), "application/json"));

app.MapFallbackToFile("overview.html");
app.Run();

public sealed record ScanRequest(IReadOnlyList<string> Lines, int ContextRadius = 1);
public sealed record ReproductionRequest(IReadOnlyList<string> Actions);
public sealed record ComparisonRequest(IReadOnlyList<string> Working, IReadOnlyList<string> Broken);
public partial class Program { }
