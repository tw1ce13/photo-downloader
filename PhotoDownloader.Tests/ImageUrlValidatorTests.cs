using PhotoDownloader.Infrastructure;
using Xunit;

namespace PhotoDownloader.Tests;

public sealed class ImageUrlValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryValidate_Empty_Fails(string? url)
    {
        var ok = ImageUrlValidator.TryValidate(url, out var uri, out var error);

        Assert.False(ok);
        Assert.Null(uri);
        Assert.Equal("Введен пустой URL", error);
    }

    [Fact]
    public void TryValidate_RelativeOrInvalid_Fails()
    {
        var ok = ImageUrlValidator.TryValidate("not-a-uri", out _, out var error);

        Assert.False(ok);
        Assert.Equal("Введен некорректный URL", error);
    }

    [Theory]
    [InlineData("ftp://example.com/a.png")]
    [InlineData("file:///C:/a.png")]
    public void TryValidate_NonHttpScheme_Fails(string url)
    {
        var ok = ImageUrlValidator.TryValidate(url, out var uri, out var error);

        Assert.False(ok);
        Assert.Null(uri);
        Assert.Equal("Строка url должна содержать http или https", error);
    }

    [Theory]
    [InlineData("https://example.com/image.png")]
    [InlineData("http://localhost/pic.jpg")]
    public void TryValidate_HttpOrHttps_Succeeds(string url)
    {
        var ok = ImageUrlValidator.TryValidate(url, out var uri, out var error);

        Assert.True(ok);
        Assert.NotNull(uri);
        Assert.Null(error);
        Assert.Equal(url.Trim(), uri!.ToString());
    }

    [Fact]
    public void TryValidate_TrimsWhitespace()
    {
        const string inner = "https://example.com/x.png";
        var ok = ImageUrlValidator.TryValidate($"  {inner}  ", out var uri, out _);

        Assert.True(ok);
        Assert.Equal(inner, uri!.ToString());
    }
}
