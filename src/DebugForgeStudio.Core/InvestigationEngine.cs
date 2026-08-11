namespace DebugForgeStudio.Core;

public sealed class InvestigationEngine
{
    public IReadOnlyList<ReproductionStep> BuildSteps(
        IEnumerable<string> actions)
    {
        if (actions is null)
            throw new ArgumentNullException(nameof(actions));

        return actions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(
                (action, index) =>
                    new ReproductionStep(
                        index + 1,
                        action.Trim(),
                        "Record observed result"))
            .ToArray();
    }

    public Hypothesis ProposeHypothesis(
        string id,
        string description,
        IEnumerable<string> evidence)
    {
        var items = evidence?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(id))
        {
            return new(
                string.Empty,
                description,
                "Invalid",
                items);
        }

        return new(
            id.Trim(),
            description?.Trim() ?? string.Empty,
            items.Length == 0
                ? "NeedsEvidence"
                : "Candidate",
            items);
    }

    public IReadOnlyList<FileDifference> Compare(
        IEnumerable<string> working,
        IEnumerable<string> broken)
    {
        if (working is null)
            throw new ArgumentNullException(nameof(working));

        if (broken is null)
            throw new ArgumentNullException(nameof(broken));

        var left = working.ToArray();
        var right = broken.ToArray();
        var count = Math.Max(left.Length, right.Length);
        var results = new List<FileDifference>();

        for (var index = 0; index < count; index++)
        {
            var a = index < left.Length
                ? left[index]
                : "<missing>";

            var b = index < right.Length
                ? right[index]
                : "<missing>";

            if (string.Equals(a, b, StringComparison.Ordinal))
                continue;

            results.Add(new(
                a == "<missing>" || b == "<missing>"
                    ? "MissingLine"
                    : "ChangedLine",
                index + 1,
                a,
                b));
        }

        return results;
    }
}
