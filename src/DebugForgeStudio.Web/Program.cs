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
    version = "1.0.0",
    mode = "Synthetic native portfolio environment"
}));

app.MapGet("/api/status", () => Results.Ok(new
{
    project = "DebugForge Studio",
    version = "1.0.0",
    screenshotGroups = 3,
    screenshots = 12,
    dataBoundary = "All logs, files, incidents, error signatures, reproduction steps, and reports are synthetic."
}));

app.MapPost(
    "/api/scan",
    (
        ScanRequest request,
        ScanEngine engine) =>
            Results.Ok(
                engine.Scan(
                    request.Lines,
                    request.ContextRadius)));

app.MapPost(
    "/api/scan/stream",
    async (
        HttpRequest request,
        ScanEngine engine,
        CancellationToken cancellationToken) =>
    {
        using var reader = new StreamReader(
            request.Body,
            leaveOpen: true);

        var result = await engine.ScanAsync(
            reader,
            cancellationToken);

        return Results.Ok(result);
    });

app.MapPost(
    "/api/triage",
    (
        ScanRequest request,
        ScanEngine engine) =>
    {
        var scan = engine.Scan(
            request.Lines,
            request.ContextRadius);

        return Results.Ok(engine.Triage(scan));
    });

app.MapPost(
    "/api/reproduction",
    (
        ReproductionRequest request,
        InvestigationEngine engine) =>
            Results.Ok(
                engine.BuildSteps(
                    request.Actions)));

app.MapPost(
    "/api/hypothesis",
    (
        HypothesisRequest request,
        InvestigationEngine engine) =>
            Results.Ok(
                engine.ProposeHypothesis(
                    request.Id,
                    request.Description,
                    request.Evidence)));

app.MapPost(
    "/api/compare",
    (
        ComparisonRequest request,
        InvestigationEngine engine) =>
            Results.Ok(
                engine.Compare(
                    request.Working,
                    request.Broken)));

app.MapPost(
    "/api/report/markdown",
    (
        DebugReport report,
        ReportEngine engine) =>
            Results.Text(
                engine.ToMarkdown(report),
                "text/markdown"));

app.MapPost(
    "/api/report/json",
    (
        DebugReport report,
        ReportEngine engine) =>
            Results.Text(
                engine.ToJson(report),
                "application/json"));

app.MapFallbackToFile("overview.html");
app.Run();

public sealed record ScanRequest(
    IReadOnlyList<string> Lines,
    int ContextRadius = 1);

public sealed record ReproductionRequest(
    IReadOnlyList<string> Actions);

public sealed record HypothesisRequest(
    string Id,
    string Description,
    IReadOnlyList<string> Evidence);

public sealed record ComparisonRequest(
    IReadOnlyList<string> Working,
    IReadOnlyList<string> Broken);

public partial class Program { }
