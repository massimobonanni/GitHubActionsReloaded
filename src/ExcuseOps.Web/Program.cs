using System.Security.Cryptography;
using System.Text;
using ExcuseOps.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IExcuseCatalog, ExcuseCatalog>();
var app = builder.Build();
_ = app.Services.GetRequiredService<IExcuseCatalog>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", (IConfiguration config) => Results.Ok(new
{
    status = "healthy",
    app = "ExcuseOps",
    stage = config["DeploymentStage"] ?? "local"
}));

app.MapGet("/api/categories", (IExcuseCatalog catalog) => TypedResults.Ok(catalog.Categories));

app.MapGet("/api/excuses", (string? category, int? variant, IExcuseCatalog catalog) =>
{
    if (category is not null && !catalog.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["category"] = [$"Choose one of: {string.Join(", ", catalog.Categories)}."]
        });
    if (variant is < 0 or > 1000)
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["variant"] = ["Use an integer between 0 and 1000."]
        });
    return Results.Ok(catalog.Pick(category, variant));
});

app.MapGet("/api/vip-excuse", (HttpRequest request, IConfiguration config) =>
{
    var expected = config["Demo:ApiKey"];
    if (string.IsNullOrWhiteSpace(expected))
        return Results.Problem(statusCode: 503, title: "VIP access is not configured.");

    var supplied = request.Headers["X-Demo-Key"];
    if (supplied.Count != 1 || string.IsNullOrEmpty(supplied[0]))
        return Results.Unauthorized();

    // Hash to equal-length buffers before constant-time comparison.
    var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied[0]!));
    var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
    return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash)
        ? Results.Ok(new Excuse("vip", "Production is fine. The dashboard is merely expressing itself."))
        : Results.Unauthorized();
});

app.Run();

public partial class Program;
