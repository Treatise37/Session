using System.IO;
using Robust.Shared.Network;

namespace Content.Client._Stalker.Private.Scr;

public sealed partial class ScrManager
{
    /// <summary>
    /// Injects minimal EXIF APP1 to an image.<br/>
    /// Required to do it manually because of Engine's sandboxing/fucking around with SixLabor's ExifProfile class. <br /><br />
    ///
    /// Usage of byte arrays instead of "STRING"u8.ToArray() is intentional to avoid engine's sandbox too(((. Engine's sandbox don't like ReadOnlySpan,
    /// cry about it
    /// </summary>
    private byte[] _FillExif(byte[] jpegData, NetUserId? playerId)
    {
        using var ms = new MemoryStream();

        var exifHeader = new byte[] { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00 };
        ms.Write(exifHeader, 0, exifHeader.Length);

        var mmHeader = new byte[] { 0x4D, 0x4D };
        ms.Write(mmHeader, 0, mmHeader.Length);
        WriteBe16(ms, 42);
        WriteBe32(ms, 8);

        var softwareBytes = System.Text.Encoding.ASCII.GetBytes("StalkerSS14\0");
        var ifd0DataOffset = 8 + 2 + 2 * 12 + 4;

        WriteBe16(ms, 2);

        WriteBe16(ms, 0x0131);
        WriteBe16(ms, 2);
        WriteBe32(ms, softwareBytes.Length);
        WriteBe32(ms, ifd0DataOffset);

        var exifIfdOffset = ifd0DataOffset + softwareBytes.Length;
        WriteBe16(ms, 0x8769);
        WriteBe16(ms, 4);
        WriteBe32(ms, 1);
        WriteBe32(ms, exifIfdOffset);

        WriteBe32(ms, 0);
        ms.Write(softwareBytes, 0, softwareBytes.Length);

        var commentText = System.Text.Encoding.ASCII.GetBytes($"{playerId}");
        var userCommentLen = 8 + commentText.Length;
        var exifIfdDataOffset = exifIfdOffset + 2 + 12 + 4;

        WriteBe16(ms, 1);

        WriteBe16(ms, 0x9286);
        WriteBe16(ms, 7);
        WriteBe32(ms, userCommentLen);
        WriteBe32(ms, exifIfdDataOffset);

        WriteBe32(ms, 0);

        var asciiPrefix = new byte[] { 0x41, 0x53, 0x43, 0x49, 0x49, 0x00, 0x00, 0x00 }; // "ASCII\0\0\0"
        ms.Write(asciiPrefix, 0, asciiPrefix.Length);
        ms.Write(commentText, 0, commentText.Length);

        var exifPayload = ms.ToArray();

        var app1ContentLen = (ushort) (2 + exifPayload.Length);
        var result = new byte[2 + 2 + 2 + exifPayload.Length + (jpegData.Length - 2)];
        result[0] = 0xFF; result[1] = 0xD8;                     // SOI
        result[2] = 0xFF; result[3] = 0xE1;                     // APP1 marker
        result[4] = (byte) (app1ContentLen >> 8);                // length hi
        result[5] = (byte) (app1ContentLen & 0xFF);              // length lo
        Array.Copy(exifPayload, 0, result, 6, exifPayload.Length);
        Array.Copy(jpegData, 2, result, 6 + exifPayload.Length, jpegData.Length - 2);

        return result;
    }

    private static void WriteBe16(MemoryStream s, int v)
    {
        s.WriteByte((byte) ((v >> 8) & 0xFF));
        s.WriteByte((byte) (v & 0xFF));
    }

    private static void WriteBe32(MemoryStream s, int v)
    {
        s.WriteByte((byte) ((v >> 24) & 0xFF));
        s.WriteByte((byte) ((v >> 16) & 0xFF));
        s.WriteByte((byte) ((v >> 8) & 0xFF));
        s.WriteByte((byte) (v & 0xFF));
    }
}
