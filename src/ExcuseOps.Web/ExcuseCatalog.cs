namespace ExcuseOps.Web;

public sealed record Excuse(string Category, string Text);

public static class ExcuseCatalog
{
    public static IReadOnlyList<string> Categories { get; } =
        Array.AsReadOnly(new[] { "code", "cloud", "meetings" });

    private static readonly Excuse[] Items =
    [
        new("code", "It works on my machine. My machine has declined to comment."),
        new("code", "The tests passed. The users were not in the test matrix."),
        new("cloud", "The cloud is just someone else's computer having a Monday."),
        new("cloud", "We achieved zero downtime by never deploying."),
        new("meetings", "The fix is ready. It is waiting for a meeting about the meeting."),
        new("meetings", "We need alignment. The YAML already has excellent indentation.")
    ];

    public static Excuse Pick(string? category = null, int? variant = null)
    {
        if (category is not null && !Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Unknown category.", nameof(category));
        if (variant is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(variant));

        var candidates = category is null
            ? Items
            : Items.Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();
        return candidates[variant.HasValue ? variant.Value % candidates.Length : Random.Shared.Next(candidates.Length)];
    }
}
