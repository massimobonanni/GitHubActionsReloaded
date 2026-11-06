using System.Net;
using System.Net.Http.Json;
using ExcuseOps.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace ExcuseOps.Tests;

public sealed class FileCatalogTests(DemoFactory baseFactory) : IClassFixture<DemoFactory>
{
    [Fact]
    public void ReadsItemsAndDerivesCategoriesInFirstSeenOrder()
    {
        using var files = new TemporaryCatalog("""
            [
              {"category":"Support","text":"First"},
              {"category":"hardware","text":"Second"},
              {"category":"SUPPORT","text":"Third"}
            ]
            """);
        IExcuseCatalog catalog = new ExcuseCatalog(files.Environment);

        Assert.Equal(new[] { "Support", "hardware" }, catalog.Categories);
        Assert.Equal(new[] { "First", "Second", "Third" }, catalog.Items.Select(x => x.Text));
        Assert.Equal("Third", catalog.Pick("support", 1).Text);
        Assert.Equal("First", catalog.Pick("SUPPORT", 2).Text);
        Assert.Equal("Second", catalog.Pick(variant: 1000).Text);
        Assert.Throws<NotSupportedException>(() => ((IList<Excuse>)catalog.Items).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)catalog.Categories).Clear());
    }

    [Fact]
    public void FileChangesAreReadOnlyByANewInstance()
    {
        using var files = new TemporaryCatalog("""[{"category":"old","text":"Before"}]""");
        var original = new ExcuseCatalog(files.Environment);

        files.Write("""[{"category":"new","text":"After"}]""");

        Assert.Equal("Before", original.Pick().Text);
        Assert.Equal(new[] { "old" }, original.Categories);
        var reloaded = new ExcuseCatalog(files.Environment);
        Assert.Equal("After", reloaded.Pick().Text);
        Assert.Equal(new[] { "new" }, reloaded.Categories);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    [InlineData("""[{"text":"Missing category"}]""")]
    [InlineData("""[{"category":"support"}]""")]
    [InlineData("""[{"category":null,"text":"Null category"}]""")]
    [InlineData("""[{"category":"support","text":null}]""")]
    [InlineData("""[{"category":"","text":"Empty category"}]""")]
    [InlineData("""[{"category":" ","text":"Blank category"}]""")]
    [InlineData("""[{"category":"support","text":""}]""")]
    [InlineData("""[{"category":"support","text":" "}]""")]
    [InlineData("""[{"category":"valid","text":"Valid"},null]""")]
    public void InvalidDataIsRejectedWithFilePath(string json)
    {
        using var files = new TemporaryCatalog(json);
        var error = Assert.Throws<InvalidDataException>(() => new ExcuseCatalog(files.Environment));
        Assert.Contains(files.FilePath, error.Message);
    }

    [Fact]
    public void MissingFileIsRejected()
    {
        using var files = new TemporaryCatalog("[]");
        File.Delete(files.FilePath);
        Assert.Throws<FileNotFoundException>(() => new ExcuseCatalog(files.Environment));
    }

    [Fact]
    public void UnreadableFileIsRejected()
    {
        using var files = new TemporaryCatalog("[]");
        using var locked = File.Open(files.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.Throws<IOException>(() => new ExcuseCatalog(files.Environment));
    }

    [Fact]
    public async Task ApiUsesCustomFileForCategoriesSelectionAndValidation()
    {
        using var files = new TemporaryCatalog("""
            [
              {"category":"support","text":"New category"},
              {"category":"SUPPORT","text":"Another excuse"},
              {"category":"hardware","text":"New hardware"}
            ]
            """);
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.UseWebRoot(files.Root));
        using var client = factory.CreateClient();

        Assert.Equal(new[] { "support", "hardware" },
            await client.GetFromJsonAsync<string[]>("/api/categories"));
        Assert.Equal(new Excuse("SUPPORT", "Another excuse"),
            await client.GetFromJsonAsync<Excuse>("/api/excuses?category=SUPPORT&variant=1"));
        var response = await client.GetAsync("/api/excuses?category=code");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Equal(new[] { "Choose one of: support, hardware." }, problem!.Errors["category"]);
        Assert.Equal(new[] { "support", "hardware" },
            factory.Services.GetRequiredService<IExcuseCatalog>().Categories);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{")]
    public void InvalidCatalogPreventsApplicationStartup(string json)
    {
        using var files = new TemporaryCatalog(json);
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.UseWebRoot(files.Root));
        Assert.Throws<InvalidDataException>(() => factory.CreateClient());
    }

    [Fact]
    public void MissingCatalogPreventsApplicationStartup()
    {
        using var files = new TemporaryCatalog("[]");
        File.Delete(files.FilePath);
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.UseWebRoot(files.Root));
        Assert.Throws<FileNotFoundException>(() => factory.CreateClient());
    }
}

internal sealed class TemporaryCatalog : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), $"excuseops-catalog-{Guid.NewGuid():N}");
    public string FilePath => Path.Combine(Root, "data", "excuses.json");
    public IWebHostEnvironment Environment { get; }

    public TemporaryCatalog(string json)
    {
        Directory.CreateDirectory(Path.Combine(Root, "data"));
        Write(json);
        Environment = new CatalogEnvironment { WebRootPath = Root };
    }

    public void Write(string json) => File.WriteAllText(FilePath, json);

    public void Dispose() => Directory.Delete(Root, recursive: true);

    private sealed class CatalogEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ExcuseOps.Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
