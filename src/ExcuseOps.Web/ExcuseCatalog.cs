using System.Text.Json;

namespace ExcuseOps.Web;

public sealed record Excuse(string Category, string Text);

public sealed class ExcuseCatalog : IExcuseCatalog
{
    public IReadOnlyList<Excuse> Items { get; }
    public IReadOnlyList<string> Categories { get; }

    public ExcuseCatalog(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.WebRootPath, "data", "excuses.json");
        using var stream = File.OpenRead(path);
        Excuse[]? items;
        try
        {
            items = JsonSerializer.Deserialize<Excuse[]>(stream, JsonSerializerOptions.Web);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The excuse catalog '{path}' contains invalid JSON.", exception);
        }

        if (items is null || items.Length == 0)
            throw new InvalidDataException($"The excuse catalog '{path}' must contain at least one excuse.");

        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            if (item is null || string.IsNullOrWhiteSpace(item.Category) || string.IsNullOrWhiteSpace(item.Text))
                throw new InvalidDataException(
                    $"The excuse catalog '{path}' has an invalid item at index {index}: category and text are required.");
        }

        Items = Array.AsReadOnly(items);
        Categories = Array.AsReadOnly(items.Select(x => x.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public Excuse Pick(string? category = null, int? variant = null)
    {
        if (category is not null && !Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Unknown category.", nameof(category));
        if (variant is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(variant));

        IReadOnlyList<Excuse> candidates = category is null
            ? Items
            : Items.Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();
        return candidates[variant.HasValue ? variant.Value % candidates.Count : Random.Shared.Next(candidates.Count)];
    }
}
