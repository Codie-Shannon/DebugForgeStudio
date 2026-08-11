using DebugForgeStudio.Core;

var passed = 0;

void Assert(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"Assertion failed: {name}");

    passed++;
    Console.WriteLine($"PASS {passed:00} {name}");
}

var scanner = new ScanEngine();

var scan = scanner.Scan(new[]
{
    "2026-08-11T08:00:00Z INFO start",
    "2026-08-11T08:00:01Z WARN retry 1",
    "2026-08-11T08:00:02Z ERROR timeout request 123",
    "2026-08-11T08:00:03Z ERROR timeout request 456"
});

Assert(scan.LinesRead == 4, "line count");
Assert(scan.Findings.Count == 3, "warning and errors found");
Assert(scan.Findings.Count(x => x.Severity == "Error") == 2, "error count");
Assert(scan.SignatureCounts.Values.Max() == 2, "normalized repeated signature");

var triage = scanner.Triage(scan);
Assert(triage.State == "Investigate", "repeated error triage");
Assert(triage.RepeatedSignatureCount == 1, "repeated signature count");

using var reader = new StringReader(
    "INFO start\nWARN slow\nERROR stopped\n");

var streamed = await scanner.ScanAsync(reader);

Assert(streamed.LinesRead == 3, "stream reader line count");
Assert(streamed.Findings.Count == 2, "stream reader findings");

var investigation = new InvestigationEngine();

Assert(
    investigation.BuildSteps(
        new[] { " Open export ", "", "Run import" }).Count == 2,
    "reproduction steps");

Assert(
    investigation.ProposeHypothesis(
        "H1",
        "Delimiter mismatch",
        Array.Empty<string>()).State == "NeedsEvidence",
    "hypothesis needs evidence");

Assert(
    investigation.ProposeHypothesis(
        "H1",
        "Delimiter mismatch",
        new[] { "changed header", "changed header" }).Evidence.Count == 1,
    "hypothesis evidence de-duplicated");

var differences = investigation.Compare(
    new[] { "id,status", "1,Ready" },
    new[] { "id;status", "1,Ready", "2,Failed" });

Assert(differences.Count == 2, "file comparison count");
Assert(differences.Any(x => x.Kind == "MissingLine"), "missing-line comparison");

var report = new DebugReport(
    "INC-001",
    "Synthetic import failure",
    scan.Findings,
    investigation.BuildSteps(
        new[] { "Open export", "Run import" }),
    new[]
    {
        investigation.ProposeHypothesis(
            "H1",
            "Delimiter mismatch",
            new[] { "header differs" })
    },
    differences);

var reporter = new ReportEngine();
var markdown = reporter.ToMarkdown(report);

Assert(markdown.Contains("DebugForge Investigation"), "markdown report");
Assert(markdown.Contains("Hypotheses"), "markdown hypotheses");
Assert(markdown.Contains("candidate evidence"), "certainty boundary");
Assert(reporter.ToJson(report).Contains("INC-001"), "json report");
Assert(reporter.BuildExport(report, triage).Triage.State == "Investigate", "export carries triage");

if (passed != 18)
    throw new InvalidOperationException($"Expected 18 tests, got {passed}.");

Console.WriteLine("DEBUGFORGE STUDIO TESTS PASSED 18/18");
