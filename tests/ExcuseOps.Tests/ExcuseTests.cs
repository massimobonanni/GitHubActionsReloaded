using System.Net;
using System.Net.Http.Json;
using ExcuseOps.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ExcuseOps.Tests;

public class CatalogTests
{
    [Theory]
    [InlineData("code")]
    [InlineData("CLOUD")]
    [InlineData("meetings")]
    public void PickHonorsCategory(string category)
    {
        var excuse = ExcuseCatalog.Pick(category, 0);
        Assert.Equal(category.ToLowerInvariant(), excuse.Category);
        Assert.False(string.IsNullOrWhiteSpace(excuse.Text));
    }

    [Fact]
    public void VariantIsDeterministicAndWraps()
    {
        Assert.Equal(ExcuseCatalog.Pick("code", 0), ExcuseCatalog.Pick("code", 2));
        Assert.NotEqual(ExcuseCatalog.Pick("code", 0), ExcuseCatalog.Pick("code", 1));
    }

    [Fact]
    public void UnknownCategoryIsRejected() =>
        Assert.Throws<ArgumentException>(() => ExcuseCatalog.Pick("blame"));

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void OutOfRangeVariantIsRejected(int variant) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcuseCatalog.Pick(variant: variant));
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
        Assert.Equal(ExcuseCatalog.Pick("cloud", 0), actual);
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
