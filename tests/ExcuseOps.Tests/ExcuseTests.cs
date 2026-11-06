using System.Net;
using System.Net.Http.Json;
using ExcuseOps.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExcuseOps.Tests;

public class CatalogTests(DemoFactory factory) : IClassFixture<DemoFactory>
{
    private readonly IExcuseCatalog catalog = factory.Services.GetRequiredService<IExcuseCatalog>();

    [Theory]
    [InlineData("code")]
    [InlineData("CLOUD")]
    [InlineData("meetings")]
    public void PickHonorsCategory(string category)
    {
        var excuse = catalog.Pick(category, 0);
        Assert.Equal(category.ToLowerInvariant(), excuse.Category);
        Assert.False(string.IsNullOrWhiteSpace(excuse.Text));
    }

    [Fact]
    public void VariantIsDeterministicAndWraps()
    {
        Assert.Equal(catalog.Pick("code", 0), catalog.Pick("code", 2));
        Assert.NotEqual(catalog.Pick("code", 0), catalog.Pick("code", 1));
    }

    [Fact]
    public void UnknownCategoryIsRejected() =>
        Assert.Throws<ArgumentException>(() => catalog.Pick("blame"));

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void OutOfRangeVariantIsRejected(int variant) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Pick(variant: variant));

    [Fact]
    public void RandomPickBelongsToCatalog()
    {
        Assert.Contains(catalog.Pick(), catalog.Items);
        Assert.Contains(catalog.Pick("CLOUD"), catalog.Items.Where(x => x.Category == "cloud"));
    }

    [Fact]
    public void CatalogIsRegisteredAsSingleton()
    {
        using var scope = factory.Services.CreateScope();
        Assert.IsType<ExcuseCatalog>(catalog);
        Assert.Same(catalog, scope.ServiceProvider.GetRequiredService<IExcuseCatalog>());
    }
}

public sealed class DemoFactory : WebApplicationFactory<Program>
{
    // A test fixture, never a deployment credential.
    public const string TestKey = "integration-test-only-not-a-real-secret";
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Demo:ApiKey"] = TestKey }));
}

public class ApiTests(DemoFactory factory) : IClassFixture<DemoFactory>
{
    [Fact]
    public async Task HealthAndStaticPageAreAvailable()
    {
        using var client = factory.CreateClient();
        var health = await client.GetFromJsonAsync<Dictionary<string, string>>("/health");
        Assert.Equal("healthy", health!["status"]);
        Assert.Contains("ExcuseOps", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task ValidRequestReturnsDeterministicExcuse()
    {
        using var client = factory.CreateClient();
        var actual = await client.GetFromJsonAsync<Excuse>("/api/excuses?category=cloud&variant=0");
        Assert.Equal(factory.Services.GetRequiredService<IExcuseCatalog>().Pick("cloud", 0), actual);
    }

    [Fact]
    public async Task CategoriesMatchCatalog()
    {
        using var client = factory.CreateClient();
        var categories = await client.GetFromJsonAsync<string[]>("/api/categories");
        Assert.Equal(factory.Services.GetRequiredService<IExcuseCatalog>().Categories, categories);
    }

    [Theory]
    [InlineData("/api/excuses?category=blame")]
    [InlineData("/api/excuses?variant=-1")]
    [InlineData("/api/excuses?variant=1001")]
    [InlineData("/api/excuses?variant=not-a-number")]
    public async Task InvalidInputReturnsBadRequest(string path)
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong")]
    public async Task VipRejectsMissingOrWrongKey(string? key)
    {
        using var client = factory.CreateClient();
        if (key is not null) client.DefaultRequestHeaders.Add("X-Demo-Key", key);
        var response = await client.GetAsync("/api/vip-excuse");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(DemoFactory.TestKey, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task VipAcceptsCorrectKeyWithoutReturningIt()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Demo-Key", DemoFactory.TestKey);
        var response = await client.GetAsync("/api/vip-excuse");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(DemoFactory.TestKey, await response.Content.ReadAsStringAsync());
        Assert.Equal("vip", (await response.Content.ReadFromJsonAsync<Excuse>())!.Category);
    }

    [Fact]
    public async Task UnconfiguredVipFailsClosed()
    {
        using var unconfigured = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:ApiKey"] = "" })));
        using var client = unconfigured.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/vip-excuse")).StatusCode);
    }
}
