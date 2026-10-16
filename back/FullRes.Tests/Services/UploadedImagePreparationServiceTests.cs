using FullRes.Api.Options;
using FullRes.Api.Providers;
using FullRes.Api.Services;
using FullRes.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FullRes.Tests.Services;

public class UploadedImagePreparationServiceTests
{
    private static FullResOptions DefaultOptions() => new()
    {
        MaxUploadBytes = 20 * 1024 * 1024,
        MaxImagePixels = 30_000_000,
        ProviderMaxImageBytes = 500 * 1024
    };

    private static UploadedImagePreparationService CreateService(FullResOptions? options = null) =>
        new(options ?? DefaultOptions(), NullLogger<UploadedImagePreparationService>.Instance);

    private static IFormFile File(byte[] bytes, string name = "test.jpg", string contentType = "image/jpeg")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "image", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task AcceptsSmallJpegWithoutReencoding()
    {
        var bytes = ImageFixtures.RealJpeg(320, 200);
        Assert.True(bytes.Length < 500 * 1024);

        var result = await CreateService().PrepareAsync(File(bytes), CancellationToken.None);

        Assert.False(result.ReencodedForProvider);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal("JPG", result.OriginalFormat);
        Assert.Equal(320, result.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal(bytes.Length, result.OriginalSizeBytes);
        Assert.Equal(bytes, result.Content);
    }

    [Fact]
    public async Task ReencodesOversizedImageWithinProviderLimit()
    {
        var bytes = ImageFixtures.RealJpeg(2000, 1500, quality: 100, noise: 1);
        Assert.True(bytes.Length > 500 * 1024, $"fixture should exceed the provider limit but was {bytes.Length} bytes");

        var result = await CreateService().PrepareAsync(File(bytes), CancellationToken.None);

        Assert.True(result.ReencodedForProvider);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.True(result.Content.Length <= 500 * 1024, $"re-encoded size was {result.Content.Length}");
        Assert.Equal(2000, result.Width);
        Assert.Equal(1500, result.Height);
        Assert.Equal(ImageFormatKind.Jpeg, ImageFormatInspector.Detect(result.Content));
    }

    [Fact]
    public async Task RejectsMissingFile()
    {
        var ex = await Assert.ThrowsAsync<ImageValidationException>(
            () => CreateService().PrepareAsync(null, CancellationToken.None));
        Assert.Equal(ImageValidationError.Missing, ex.Kind);
    }

    [Fact]
    public async Task RejectsEmptyFile()
    {
        var ex = await Assert.ThrowsAsync<ImageValidationException>(
            () => CreateService().PrepareAsync(File([], "empty.jpg"), CancellationToken.None));
        Assert.Equal(ImageValidationError.Missing, ex.Kind);
    }

    [Fact]
    public async Task RejectsFileAboveUploadLimit()
    {
        var options = DefaultOptions();
        options.MaxUploadBytes = 1024;
        var bytes = ImageFixtures.RealJpeg(200, 200);

        var ex = await Assert.ThrowsAsync<ImageValidationException>(
            () => CreateService(options).PrepareAsync(File(bytes), CancellationToken.None));
        Assert.Equal(ImageValidationError.TooLarge, ex.Kind);
    }

    [Fact]
    public async Task RejectsUnsupportedFormat()
    {
        var ex = await Assert.ThrowsAsync<ImageValidationException>(
            () => CreateService().PrepareAsync(File(ImageFixtures.GifHeader(10, 10), "anim.gif", "image/gif"), CancellationToken.None));
        Assert.Equal(ImageValidationError.UnsupportedFormat, ex.Kind);
    }

    [Fact]
    public async Task RejectsNonImageContent()
    {
        var ex = await Assert.ThrowsAsync<ImageValidationException>(
            () => CreateService().PrepareAsync(File("not an image at all"u8.ToArray(), "fake.jpg"), CancellationToken.None));
        Assert.Equal(ImageValidationError.UnsupportedFormat, ex.Kind);
    }

    [Fact]
    public async Task RejectsTooManyPixels()
    {
        var options = DefaultOptions();
        options.MaxImagePixels = 1000;
        var bytes = ImageFixtures.JpegHeader(4000, 3000);

        var ex = await Assert.ThrowsAsync<ImageValidationException>(
            () => CreateService(options).PrepareAsync(File(bytes), CancellationToken.None));
        Assert.Equal(ImageValidationError.DimensionsTooLarge, ex.Kind);
    }
}
