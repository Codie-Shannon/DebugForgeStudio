namespace DebugForgeStudio.Core;

public sealed class ScanEngine
{
    public ScanSummary Scan(IEnumerable<string> lines, int contextRadius = 1)
    {
        var materialised = lines.ToArray();
        var findings = new List<LogFinding>();

        for (var index = 0; index < materialised.Length; index++)
        {
            var line = materialised[index];
            var severity =
                line.Contains(" ERROR ", StringComparison.OrdinalIgnoreCase) ? "Error" :
                line.Contains(" WARN ", StringComparison.OrdinalIgnoreCase) ? "Warning" :
                "Information";

            if (severity == "Information")
                continue;

            var signature = line
                .Replace("2026-08-11T08:00:01Z ", "", StringComparison.Ordinal)
                .Replace("2026-08-11T08:00:02Z ", "", StringComparison.Ordinal)
                .Replace("2026-08-11T08:00:03Z ", "", StringComparison.Ordinal);

            var start = Math.Max(0, index - Math.Max(0, contextRadius));
            var end = Math.Min(materialised.Length - 1, index + Math.Max(0, contextRadius));
            var context = string.Join(
                Environment.NewLine,
                materialised.Skip(start).Take(end - start + 1));

            findings.Add(new(
                index + 1,
                severity,
                signature,
                context,
                severity == "Error" ? "LOG_ERROR" : "LOG_WARNING"));
        }

        return new(
            materialised.Length,
            findings,
            findings
                .GroupBy(x => x.Signature, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase));
    }
}
