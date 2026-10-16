using FullRes.Api.Models;
using FullRes.Api.Options;
using FullRes.Api.Providers;
using SkiaSharp;

namespace FullRes.Api.Services;

public sealed class UploadedImagePreparationService
{
    private static readonly ImageFormatKind[] AllowedFormats =
        [ ImageFormatKind.Jpeg, ImageFormatKind.Png, ImageFormatKind.Webp ];

    private const int MinEncodeDimension = 320;
    private const int InitialQuality = 85;
    private const int MinQuality = 45;

    private readonly FullResOptions _options;
    private readonly ILogger<UploadedImagePreparationService> _logger;

    public UploadedImagePreparationService(FullResOptions options, ILogger<UploadedImagePreparationService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<PreparedImage> PrepareAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new ImageValidationException(ImageValidationError.Missing, "No image file was uploaded.");

        if (file.Length > _options.MaxUploadBytes)
            throw new ImageValidationException(
                ImageValidationError.TooLarge,
                $"The uploaded file is {file.Length} bytes; the limit is {_options.MaxUploadBytes} bytes.",
                file.Length);

        byte[] original;
        await using (var buffer = new MemoryStream((int)Math.Min(file.Length, int.MaxValue)))
        {
            await file.CopyToAsync(buffer, cancellationToken);
            original = buffer.ToArray();
        }

        if (original.Length == 0)
            throw new ImageValidationException(ImageValidationError.Empty, "The uploaded file is empty.", 0);

        var kind = ImageFormatInspector.Detect(original);
        if (!AllowedFormats.Contains(kind))
        {
            throw new ImageValidationException(
                ImageValidationError.UnsupportedFormat,
                kind == ImageFormatKind.Unknown
                    ? "The uploaded file is not a recognized image (JPEG, PNG or WebP)."
                    : $"Unsupported image format '{ImageFormatInspector.ToFormatName(kind)}'. Supported formats: JPEG, PNG, WebP.",
                original.Length);
        }

        if (!ImageFormatInspector.TryReadDimensions(original, out var width, out var height))
            throw new ImageValidationException(
                ImageValidationError.InvalidContent,
                "The uploaded file is not a decodable image.",
                original.Length);

        long pixels = (long)width * height;
        if (pixels > _options.MaxImagePixels)
            throw new ImageValidationException(
                ImageValidationError.DimensionsTooLarge,
                $"The image is {width}x{height} ({pixels} pixels); the limit is {_options.MaxImagePixels} pixels.",
                original.Length);

        var originalFormat = ImageFormatInspector.ToFormatName(kind);

        if (original.Length <= _options.ProviderMaxImageBytes)
        {
            return new PreparedImage
            {
                Content = original,
                ContentType = ImageFormatInspector.ToMimeType(kind),
                FileName = file.FileName is { Length: > 0 } ? file.FileName : $"upload.{ImageFormatInspector.ToFormatName(kind).ToLowerInvariant()}",
                Width = width,
                Height = height,
                OriginalSizeBytes = original.Length,
                OriginalFormat = originalFormat,
                ReencodedForProvider = false
            };
        }

        var reencoded = ReencodeWithinLimit(original, cancellationToken);
        return new PreparedImage
        {
            Content = reencoded,
            ContentType = "image/jpeg",
            FileName = "upload.jpg",
            Width = width,
            Height = height,
            OriginalSizeBytes = original.Length,
            OriginalFormat = originalFormat,
            ReencodedForProvider = true
        };
    }

    private byte[] ReencodeWithinLimit(byte[] original, CancellationToken cancellationToken)
    {
        using var source = SKBitmap.Decode(original);
        if (source is null)
            throw new ImageValidationException(
                ImageValidationError.InvalidContent,
                "The uploaded image could not be decoded.",
                original.Length);

        var width = source.Width;
        var height = source.Height;
        var quality = InitialQuality;
        long limit = _options.ProviderMaxImageBytes;

        using var flattened = Flatten(source);
        SKBitmap current = flattened;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytes = EncodeJpeg(current, quality);
            if (bytes.Length <= limit)
            {
                _logger.LogInformation(
                    "Re-encoded upload to {Size} bytes (JPEG q{Quality}, {Width}x{Height}) to fit the provider limit of {Limit} bytes.",
                    bytes.Length, quality, width, height, limit);
                if (!ReferenceEquals(current, flattened))
                    current.Dispose();
                return bytes;
            }

            if (quality > MinQuality)
            {
                quality -= 15;
                continue;
            }

            if (width <= MinEncodeDimension && height <= MinEncodeDimension)
                break;

            var nextWidth = Math.Max(MinEncodeDimension, (int)(width * 0.7));
            var nextHeight = Math.Max(MinEncodeDimension, (int)(height * 0.7));
            var resized = current.Resize(new SKImageInfo(nextWidth, nextHeight), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            if (resized is null)
                break;

            if (!ReferenceEquals(current, flattened))
                current.Dispose();
            current = resized;
            width = nextWidth;
            height = nextHeight;
            quality = InitialQuality;
        }

        if (!ReferenceEquals(current, flattened))
            current.Dispose();
        throw new ImageValidationException(
            ImageValidationError.CouldNotFitProviderLimit,
            $"The image could not be reduced below {_options.ProviderMaxImageBytes} bytes. Try a smaller or less detailed version.");
    }

    private static SKBitmap Flatten(SKBitmap source)
    {
        var info = new SKImageInfo(source.Width, source.Height, SKColorType.Rgb888x, SKAlphaType.Opaque);
        var target = new SKBitmap(info);
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        canvas.Flush();
        return target;
    }

    private static byte[] EncodeJpeg(SKBitmap bitmap, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }
}
