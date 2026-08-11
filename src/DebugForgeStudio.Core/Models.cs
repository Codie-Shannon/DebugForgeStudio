namespace DebugForgeStudio.Core;

public sealed record LogFinding(
    int LineNumber,
    string Severity,
    string Signature,
    string Context,
    string RuleId);

public sealed record ScanSummary(
    int LinesRead,
    IReadOnlyList<LogFinding> Findings,
    IReadOnlyDictionary<string, int> SignatureCounts);

public sealed record TriageSummary(
    string State,
    int ErrorCount,
    int WarningCount,
    int RepeatedSignatureCount,
    IReadOnlyList<string> Evidence);

public sealed record ReproductionStep(
    int Order,
    string Action,
    string Expected);

public sealed record Hypothesis(
    string Id,
    string Description,
    string State,
    IReadOnlyList<string> Evidence);

public sealed record FileDifference(
    string Kind,
    int? LineNumber,
    string Working,
    string Broken);

public sealed record DebugReport(
    string IncidentId,
    string Summary,
    IReadOnlyList<LogFinding> Findings,
    IReadOnlyList<ReproductionStep> Steps,
    IReadOnlyList<Hypothesis> Hypotheses,
    IReadOnlyList<FileDifference> Differences);

public sealed record InvestigationExport(
    DebugReport Report,
    TriageSummary Triage,
    DateTimeOffset GeneratedAt,
    string Boundary);
