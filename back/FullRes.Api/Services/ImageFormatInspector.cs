namespace FullRes.Api.Services;

public enum ImageFormatKind
{
    Unknown,
    Jpeg,
    Png,
    Webp,
    Gif,
    Bmp
}

public static class ImageFormatInspector
{
    public static ImageFormatKind Detect(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return ImageFormatKind.Jpeg;
        if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return ImageFormatKind.Png;
        if (data.Length >= 12 && data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F' &&
            data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P')
            return ImageFormatKind.Webp;
        if (data.Length >= 6 && data[0] == (byte)'G' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'8')
            return ImageFormatKind.Gif;
        if (data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M')
            return ImageFormatKind.Bmp;
        return ImageFormatKind.Unknown;
    }

    public static bool TryReadDimensions(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = 0;
        height = 0;
        return Detect(data) switch
        {
            ImageFormatKind.Png => TryReadPng(data, out width, out height),
            ImageFormatKind.Jpeg => TryReadJpeg(data, out width, out height),
            ImageFormatKind.Webp => TryReadWebp(data, out width, out height),
            ImageFormatKind.Gif => TryReadGif(data, out width, out height),
            ImageFormatKind.Bmp => TryReadBmp(data, out width, out height),
            _ => false
        };
    }

    public static string ToFormatName(ImageFormatKind kind) => kind switch
    {
        ImageFormatKind.Jpeg => "JPG",
        ImageFormatKind.Png => "PNG",
        ImageFormatKind.Webp => "WEBP",
        ImageFormatKind.Gif => "GIF",
        ImageFormatKind.Bmp => "BMP",
        _ => "UNKNOWN"
    };

    public static string ToMimeType(ImageFormatKind kind) => kind switch
    {
        ImageFormatKind.Jpeg => "image/jpeg",
        ImageFormatKind.Png => "image/png",
        ImageFormatKind.Webp => "image/webp",
        ImageFormatKind.Gif => "image/gif",
        ImageFormatKind.Bmp => "image/bmp",
        _ => "application/octet-stream"
    };

    private static bool TryReadPng(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        if (data.Length < 24 || data[12] != (byte)'I' || data[13] != (byte)'H' || data[14] != (byte)'D' || data[15] != (byte)'R')
            return false;
        width = ReadInt32BigEndian(data[16..20]);
        height = ReadInt32BigEndian(data[20..24]);
        return width > 0 && height > 0;
    }

    private static bool TryReadGif(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        if (data.Length < 10)
            return false;
        width = data[6] | (data[7] << 8);
        height = data[8] | (data[9] << 8);
        return width > 0 && height > 0;
    }

    private static bool TryReadBmp(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        if (data.Length < 26)
            return false;
        width = ReadInt32LittleEndian(data[18..22]);
        height = Math.Abs(ReadInt32LittleEndian(data[22..26]));
        return width > 0 && height > 0;
    }

    private static bool TryReadJpeg(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        var index = 2;
        while (index + 9 < data.Length)
        {
            if (data[index] != 0xFF)
            {
                index++;
                continue;
            }

            var marker = data[index + 1];
            if (marker == 0xFF || marker == 0x00 || (marker >= 0xD0 && marker <= 0xD9) || marker == 0x01)
            {
                index += 2;
                continue;
            }

            if (index + 3 >= data.Length)
                return false;

            var segmentLength = (data[index + 2] << 8) | data[index + 3];
            if (segmentLength < 2)
                return false;

            var isStartOfFrame = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
            if (isStartOfFrame)
            {
                if (index + 8 >= data.Length)
                    return false;
                height = (data[index + 5] << 8) | data[index + 6];
                width = (data[index + 7] << 8) | data[index + 8];
                return width > 0 && height > 0;
            }

            if (marker == 0xDA)
                return false;

            index += 2 + segmentLength;
        }

        return false;
    }

    private static bool TryReadWebp(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        if (data.Length < 30)
            return false;

        var chunk = System.Text.Encoding.ASCII.GetString(data[12..16].ToArray());

        switch (chunk)
        {
            case "VP8 ":
            {
                if (data.Length < 30 || data[23] != 0x9D || data[24] != 0x01 || data[25] != 0x2A)
                    return false;
                width = ((data[27] << 8) | data[26]) & 0x3FFF;
                height = ((data[29] << 8) | data[28]) & 0x3FFF;
                return width > 0 && height > 0;
            }
            case "VP8L":
            {
                if (data.Length < 25 || data[20] != 0x2F)
                    return false;
                var bits = data[21] | (data[22] << 8) | (data[23] << 16) | (data[24] << 24);
                width = (bits & 0x3FFF) + 1;
                height = ((bits >> 14) & 0x3FFF) + 1;
                return width is >= 1 and <= 16384 && height is >= 1 and <= 16384;
            }
            case "VP8X":
            {
                if (data.Length < 30)
                    return false;
                width = (data[24] | (data[25] << 8) | (data[26] << 16)) + 1;
                height = (data[27] | (data[28] << 8) | (data[29] << 16)) + 1;
                return width > 0 && height > 0;
            }
            default:
                return false;
        }
    }

    private static int ReadInt32BigEndian(ReadOnlySpan<byte> data) =>
        (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];

    private static int ReadInt32LittleEndian(ReadOnlySpan<byte> data) =>
        data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);
}
