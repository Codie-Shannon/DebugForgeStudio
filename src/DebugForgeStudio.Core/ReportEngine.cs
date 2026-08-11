using System.Text.Json;

namespace DebugForgeStudio.Core;

public sealed class ReportEngine
{
    public string ToMarkdown(DebugReport report)
    {
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

        lines.AddRange(report.Findings.Select(x =>
            $"- Line {x.LineNumber}: [{x.Severity}] {x.Signature} ({x.RuleId})"));

        lines.Add("");
        lines.Add("## Reproduction");
        lines.AddRange(report.Steps.Select(x => $"{x.Order}. {x.Action} — {x.Expected}"));

        lines.Add("");
        lines.Add("## Boundaries");
        lines.Add("- Findings are candidate evidence, not certainty.");
        lines.Add("- Mock suggestions never write to external systems.");

        return string.Join(Environment.NewLine, lines);
    }

    public string ToJson(DebugReport report) =>
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
}
