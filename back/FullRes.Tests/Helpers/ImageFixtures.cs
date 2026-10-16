using FullRes.Api.Services;
using SkiaSharp;

namespace FullRes.Tests.Helpers;

public static class ImageFixtures
{
    public static byte[] PngHeader(int width, int height)
    {
        var data = new byte[33];
        byte[] signature = [ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A ];
        signature.CopyTo(data, 0);
        data[8] = 0x00; data[9] = 0x00; data[10] = 0x00; data[11] = 0x0D;
        data[12] = (byte)'I'; data[13] = (byte)'H'; data[14] = (byte)'D'; data[15] = (byte)'R';
        WriteInt32BigEndian(data, 16, width);
        WriteInt32BigEndian(data, 20, height);
        data[24] = 8;
        data[25] = 6;
        return data;
    }

    public static byte[] JpegHeader(int width, int height)
    {
        var data = new List<byte> { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        data.AddRange("JFIF\0"u8.ToArray());
        data.AddRange(new byte[] { 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00 });
        data.AddRange(new byte[] { 0xFF, 0xDB, 0x00, 0x43, 0x00 });
        data.AddRange(new byte[64]);
        data.AddRange(new byte[] { 0xFF, 0xC0, 0x00, 0x11, 0x08 });
        data.Add((byte)(height >> 8));
        data.Add((byte)(height & 0xFF));
        data.Add((byte)(width >> 8));
        data.Add((byte)(width & 0xFF));
        data.AddRange(new byte[] { 0x03, 0x01, 0x22, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01 });
        return data.ToArray();
    }

    public static byte[] GifHeader(int width, int height)
    {
        var data = new byte[13];
        data[0] = (byte)'G'; data[1] = (byte)'I'; data[2] = (byte)'F'; data[3] = (byte)'8';
        data[4] = (byte)'9'; data[5] = (byte)'a';
        data[6] = (byte)(width & 0xFF); data[7] = (byte)(width >> 8);
        data[8] = (byte)(height & 0xFF); data[9] = (byte)(height >> 8);
        return data;
    }

    public static byte[] BmpHeader(int width, int height)
    {
        var data = new byte[54];
        data[0] = (byte)'B'; data[1] = (byte)'M';
        WriteInt32LittleEndian(data, 18, width);
        WriteInt32LittleEndian(data, 22, height);
        return data;
    }

    public static byte[] WebpHeader(int width, int height)
    {
        var data = new byte[40];
        data[0] = (byte)'R'; data[1] = (byte)'I'; data[2] = (byte)'F'; data[3] = (byte)'F';
        data[8] = (byte)'W'; data[9] = (byte)'E'; data[10] = (byte)'B'; data[11] = (byte)'P';
        data[12] = (byte)'V'; data[13] = (byte)'P'; data[14] = (byte)'8'; data[15] = (byte)' ';
        data[23] = 0x9D; data[24] = 0x01; data[25] = 0x2A;
        data[26] = (byte)(width & 0xFF); data[27] = (byte)((width >> 8) & 0x3F);
        data[28] = (byte)(height & 0xFF); data[29] = (byte)((height >> 8) & 0x3F);
        return data;
    }

    public static byte[] RealJpeg(int width, int height, int quality = 95, int noise = 0)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgb888x, SKAlphaType.Opaque));
        var random = new Random(42);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                byte r = (byte)((x * 255 / Math.Max(width - 1, 1)));
                byte g = (byte)((y * 255 / Math.Max(height - 1, 1)));
                byte b = noise > 0 ? (byte)random.Next(256) : (byte)((x + y) % 256);
                bitmap.SetPixel(x, y, new SKColor(r, g, b));
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }

    private static void WriteInt32BigEndian(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)((value >> 16) & 0xFF);
        data[offset + 2] = (byte)((value >> 8) & 0xFF);
        data[offset + 3] = (byte)(value & 0xFF);
    }

    private static void WriteInt32LittleEndian(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }
}
