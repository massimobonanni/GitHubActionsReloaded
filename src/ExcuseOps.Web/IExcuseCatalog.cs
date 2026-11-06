namespace ExcuseOps.Web;

public interface IExcuseCatalog
{
    IReadOnlyList<Excuse> Items { get; }
    IReadOnlyList<string> Categories { get; }
    Excuse Pick(string? category = null, int? variant = null);
}
