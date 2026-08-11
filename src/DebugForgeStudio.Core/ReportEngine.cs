using System.Text.Json;

namespace DebugForgeStudio.Core;

public sealed class ReportEngine
{
    public string ToMarkdown(DebugReport report)
    {
        if (report is null)
            throw new ArgumentNullException(nameof(report));

        var lines = new List<string>
        {
            "# DebugForge Investigation",
            "",
            $"Incident: {report.IncidentId}",
            "",
            report.Summary,
            "",
            "## Findings"
        };

        lines.AddRange(
            report.Findings.Select(
                x =>
                    $"- Line {x.LineNumber}: [{x.Severity}] " +
                    $"{x.Signature} ({x.RuleId})"));

        lines.Add("");
        lines.Add("## Reproduction");

        lines.AddRange(
            report.Steps.Select(
                x =>
                    $"{x.Order}. {x.Action} - {x.Expected}"));

        lines.Add("");
        lines.Add("## Hypotheses");

        lines.AddRange(
            report.Hypotheses.Select(
                x =>
                    $"- {x.Id}: {x.Description} [{x.State}]"));

        lines.Add("");
        lines.Add("## Differences");

        lines.AddRange(
            report.Differences.Select(
                x =>
                    $"- Line {x.LineNumber}: {x.Kind}"));

        lines.Add("");
        lines.Add("## Boundaries");
        lines.Add(
            "- Findings are candidate evidence, not certainty.");
        lines.Add(
            "- Hypotheses require operator review.");
        lines.Add(
            "- DebugForge does not execute fixes or write to external systems.");

        return string.Join(
            Environment.NewLine,
            lines);
    }

    public string ToJson(DebugReport report) =>
        JsonSerializer.Serialize(
            report,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

    public InvestigationExport BuildExport(
        DebugReport report,
        TriageSummary triage)
    {
        return new(
            report,
            triage,
            DateTimeOffset.UtcNow,
            "Synthetic portfolio evidence only; no automated fix execution.");
    }
}
