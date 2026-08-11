using DebugForgeStudio.Core;

var passed = 0;
void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"Assertion failed: {name}");
    passed++;
    Console.WriteLine($"PASS {passed:00} {name}");
}

var scanner = new ScanEngine();
var scan = scanner.Scan(new[]
{
    "2026-08-11 INFO start",
    "2026-08-11 WARN retry",
    "2026-08-11 ERROR timeout",
    "2026-08-11 ERROR timeout"
});

Assert(scan.LinesRead == 4, "stream line count");
Assert(scan.Findings.Count == 3, "warning and errors found");
Assert(scan.Findings.Count(x => x.Severity == "Error") == 2, "error count");
Assert(scan.SignatureCounts.Values.Max() >= 1, "signature grouping");

var investigation = new InvestigationEngine();
Assert(investigation.BuildSteps(new[] { "Open export", "Run import" }).Count == 2, "reproduction steps");
Assert(investigation.Compare(new[] { "a", "b" }, new[] { "a", "c" }).Count == 1, "file comparison");
Assert(investigation.ProposeHypothesis("H1", "Delimiter mismatch", new[] { "changed header" }).State == "Candidate", "hypothesis state");

var report = new DebugReport(
    "INC-001",
    "Synthetic import failure",
    scan.Findings,
    investigation.BuildSteps(new[] { "Open export", "Run import" }),
    new[] { investigation.ProposeHypothesis("H1", "Delimiter mismatch", new[] { "header differs" }) },
    investigation.Compare(new[] { "id,status" }, new[] { "id;status" }));

var reporter = new ReportEngine();
Assert(reporter.ToMarkdown(report).Contains("DebugForge Investigation"), "markdown report");
Assert(reporter.ToJson(report).Contains("INC-001"), "json report");
Assert(reporter.ToMarkdown(report).Contains("candidate evidence"), "certainty boundary");

Console.WriteLine($"DEBUGFORGE STUDIO TESTS PASSED {passed}/{passed}");
