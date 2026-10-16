using FullRes.Api.Services;
using FullRes.Tests.Helpers;

namespace FullRes.Tests.Services;

public class ImageFormatInspectorTests
{
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1, 1)]
    [InlineData(8000, 6000)]
    public void ReadsJpegDimensions(int width, int height)
    {
        var data = ImageFixtures.JpegHeader(width, height);
        Assert.Equal(ImageFormatKind.Jpeg, ImageFormatInspector.Detect(data));
        Assert.True(ImageFormatInspector.TryReadDimensions(data, out var w, out var h));
        Assert.Equal(width, w);
        Assert.Equal(height, h);
    }

    [Theory]
    [InlineData(1024, 768)]
    public void ReadsPngDimensions(int width, int height)
    {
        var data = ImageFixtures.PngHeader(width, height);
        Assert.Equal(ImageFormatKind.Png, ImageFormatInspector.Detect(data));
        Assert.True(ImageFormatInspector.TryReadDimensions(data, out var w, out var h));
        Assert.Equal(width, w);
        Assert.Equal(height, h);
    }

    [Theory]
    [InlineData(640, 480)]
    public void ReadsWebpDimensions(int width, int height)
    {
        var data = ImageFixtures.WebpHeader(width, height);
        Assert.Equal(ImageFormatKind.Webp, ImageFormatInspector.Detect(data));
        Assert.True(ImageFormatInspector.TryReadDimensions(data, out var w, out var h));
        Assert.Equal(width, w);
        Assert.Equal(height, h);
    }

    [Theory]
    [InlineData(320, 240)]
    public void ReadsGifDimensions(int width, int height)
    {
        var data = ImageFixtures.GifHeader(width, height);
        Assert.Equal(ImageFormatKind.Gif, ImageFormatInspector.Detect(data));
        Assert.True(ImageFormatInspector.TryReadDimensions(data, out var w, out var h));
        Assert.Equal(width, w);
        Assert.Equal(height, h);
    }

    [Fact]
    public void DetectsRealEncodedJpeg()
    {
        var data = ImageFixtures.RealJpeg(200, 100);
        Assert.Equal(ImageFormatKind.Jpeg, ImageFormatInspector.Detect(data));
        Assert.True(ImageFormatInspector.TryReadDimensions(data, out var w, out var h));
        Assert.Equal(200, w);
        Assert.Equal(100, h);
    }

    [Fact]
    public void UnknownForRandomBytes()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        Assert.Equal(ImageFormatKind.Unknown, ImageFormatInspector.Detect(data));
        Assert.False(ImageFormatInspector.TryReadDimensions(data, out _, out _));
    }

    [Fact]
    public void TruncatedPngDoesNotThrow()
    {
        var data = ImageFixtures.PngHeader(100, 100)[..10];
        Assert.False(ImageFormatInspector.TryReadDimensions(data, out _, out _));
    }

    [Theory]
    [InlineData(ImageFormatKind.Jpeg, "JPG", "image/jpeg")]
    [InlineData(ImageFormatKind.Png, "PNG", "image/png")]
    [InlineData(ImageFormatKind.Webp, "WEBP", "image/webp")]
    [InlineData(ImageFormatKind.Gif, "GIF", "image/gif")]
    [InlineData(ImageFormatKind.Bmp, "BMP", "image/bmp")]
    public void FormatsHaveStableNames(ImageFormatKind kind, string name, string mime)
    {
        Assert.Equal(name, ImageFormatInspector.ToFormatName(kind));
        Assert.Equal(mime, ImageFormatInspector.ToMimeType(kind));
    }
}
