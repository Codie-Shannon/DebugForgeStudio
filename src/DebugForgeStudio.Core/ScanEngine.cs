using System.Text.RegularExpressions;

namespace DebugForgeStudio.Core;

public sealed partial class ScanEngine
{
    public ScanSummary Scan(
        IEnumerable<string> lines,
        int contextRadius = 1)
    {
        if (lines is null)
            throw new ArgumentNullException(nameof(lines));

        var materialised = lines.ToArray();
        var findings = new List<LogFinding>();

        for (var index = 0; index < materialised.Length; index++)
        {
            var line = materialised[index];
            var severity = ClassifySeverity(line);

            if (severity == "Information")
                continue;

            var signature = NormalizeSignature(line);

            var start = Math.Max(
                0,
                index - Math.Max(0, contextRadius));

            var end = Math.Min(
                materialised.Length - 1,
                index + Math.Max(0, contextRadius));

            var context = string.Join(
                Environment.NewLine,
                materialised
                    .Skip(start)
                    .Take(end - start + 1));

            findings.Add(new(
                index + 1,
                severity,
                signature,
                context,
                severity == "Error"
                    ? "LOG_ERROR"
                    : "LOG_WARNING"));
        }

        return BuildSummary(
            materialised.Length,
            findings);
    }

    public async Task<ScanSummary> ScanAsync(
        TextReader reader,
        CancellationToken cancellationToken = default)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        var findings = new List<LogFinding>();
        var lineNumber = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
                break;

            lineNumber++;

            var severity = ClassifySeverity(line);

            if (severity == "Information")
                continue;

            findings.Add(new(
                lineNumber,
                severity,
                NormalizeSignature(line),
                line,
                severity == "Error"
                    ? "LOG_ERROR"
                    : "LOG_WARNING"));
        }

        return BuildSummary(
            lineNumber,
            findings);
    }

    public TriageSummary Triage(ScanSummary scan)
    {
        if (scan is null)
            throw new ArgumentNullException(nameof(scan));

        var errors = scan.Findings.Count(
            x => x.Severity == "Error");

        var warnings = scan.Findings.Count(
            x => x.Severity == "Warning");

        var repeated = scan.SignatureCounts.Count(
            x => x.Value > 1);

        var state =
            errors > 0 && repeated > 0 ? "Investigate" :
            errors > 0 ? "Review" :
            warnings > 0 ? "Monitor" :
            "Clear";

        return new(
            state,
            errors,
            warnings,
            repeated,
            new[]
            {
                "errors:" + errors,
                "warnings:" + warnings,
                "repeatedSignatures:" + repeated,
                "linesRead:" + scan.LinesRead
            });
    }

    private static string ClassifySeverity(string line)
    {
        if (
            line.Contains(
                " ERROR ",
                StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith(
                "ERROR ",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return "Error";
        }

        if (
            line.Contains(
                " WARN ",
                StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith(
                "WARN ",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return "Warning";
        }

        return "Information";
    }

    private static string NormalizeSignature(string line)
    {
        var normalized = TimestampRegex().Replace(
            line,
            string.Empty);

        normalized = GuidRegex().Replace(
            normalized,
            "<guid>");

        normalized = IntegerRegex().Replace(
            normalized,
            "<n>");

        return normalized.Trim();
    }

    private static ScanSummary BuildSummary(
        int linesRead,
        IReadOnlyList<LogFinding> findings)
    {
        return new(
            linesRead,
            findings,
            findings
                .GroupBy(
                    x => x.Signature,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.Count(),
                    StringComparer.OrdinalIgnoreCase));
    }

    [GeneratedRegex(
        @"^\d{4}-\d{2}-\d{2}T\S+\s+",
        RegexOptions.CultureInvariant)]
    private static partial Regex TimestampRegex();

    [GeneratedRegex(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex GuidRegex();

    [GeneratedRegex(
        @"\b\d+\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex IntegerRegex();
}
