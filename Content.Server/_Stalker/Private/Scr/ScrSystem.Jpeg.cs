using System.Text;

namespace Content.Server._Stalker.Private.Scr;

public sealed partial class ScrSystem
{
    /// <summary>
    /// Reads the EXIF APP1 tag from a JPEG byte array and extracts the UserComment and Software tags if present.
    /// Had to do it manually because of engine's sandboxing and missing SixLabor's library on server content.
    /// </summary>
    /// <param name="jpeg">JPEG image byte array</param>
    /// <returns>A tuple of 2 EXIF tags, UserComment and Software</returns>
    private static (string? userComment, string? software) _ReadExifTags(byte[] jpeg)
    {
        var index = 0;

        while (index < jpeg.Length - 1)
        {
            if (jpeg[index] != 0xFF) { index++; continue; }

            var marker = jpeg[index + 1];

            if (marker == 0xE1 && index + 4 < jpeg.Length)
            {
                var dataStart = index + 4;

                if (dataStart + 6 <= jpeg.Length &&
                    jpeg[dataStart]     == 'E' && jpeg[dataStart + 1] == 'x' &&
                    jpeg[dataStart + 2] == 'i' && jpeg[dataStart + 3] == 'f' &&
                    jpeg[dataStart + 4] == 0   && jpeg[dataStart + 5] == 0)
                {
                    var tiffBase = dataStart + 6;
                    return _ParseExifTags(jpeg, tiffBase);
                }
            }

            if (marker == 0xD8 || marker == 0xD9 || marker == 0x01)
                index += 2;
            else if (index + 4 <= jpeg.Length)
                index += 2 + ((jpeg[index + 2] << 8) | jpeg[index + 3]);
            else
                break;
        }

        return (null, null);
    }

    private static (string? userComment, string? software) _ParseExifTags(byte[] data, int tiffBase)
    {
        if (tiffBase + 8 > data.Length) return (null, null);

        var littleEndian = data[tiffBase] == 'I' && data[tiffBase + 1] == 'I';

        var ifd0Offset = (int)ReadUInt32(tiffBase + 4);

        var software = _ReadIfdTag(data, tiffBase, ifd0Offset, 0x0131, ReadUInt16, ReadUInt32);

        string? userComment = null;
        var exifIfdOffsetStr = _ReadIfdTag(data, tiffBase, ifd0Offset, 0x8769, ReadUInt16, ReadUInt32);

        if (exifIfdOffsetStr != null && uint.TryParse(exifIfdOffsetStr, out var exifIfdOffset))
            userComment = _ReadIfdTag(data, tiffBase, (int)exifIfdOffset, 0x9286, ReadUInt16, ReadUInt32);

        return (userComment, software);

        uint ReadUInt16(int offset) => littleEndian
            ? (uint)(data[offset] | (data[offset + 1] << 8))
            : (uint)((data[offset] << 8) | data[offset + 1]);

        uint ReadUInt32(int offset) => littleEndian
            ? (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24))
            : (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }

    private static string? _ReadIfdTag(
        byte[] data, int tiffBase, int ifdOffset, ushort targetTag,
        Func<int, uint> readUInt16,
        Func<int, uint> readUInt32)
    {
        var absOffset = tiffBase + ifdOffset;
        if (absOffset + 2 > data.Length) return null;

        var entryCount = (int)readUInt16(absOffset);
        absOffset += 2;

        for (var i = 0; i < entryCount; i++)
        {
            var entryBase = absOffset + i * 12;
            if (entryBase + 12 > data.Length) break;

            var tag    = (ushort)readUInt16(entryBase);
            var type   = (ushort)readUInt16(entryBase + 2);
            var count  = (int)readUInt32(entryBase + 4);

            if (tag != targetTag)
                continue;

            if (type == 4 && count == 1)
            {
                var val = readUInt32(entryBase + 8);
                return val.ToString();
            }

            var typeSize = type switch { 1 => 1, 2 => 1, 3 => 2, 4 => 4, 5 => 8, _ => 1 };
            var totalSize = count * typeSize;
            var dataOffset = totalSize <= 4
                ? entryBase + 8
                : tiffBase + (int)readUInt32(entryBase + 8);

            if (dataOffset + totalSize > data.Length) return null;

            if (tag == 0x9286 && count > 8)
                return Encoding.UTF8.GetString(data, dataOffset + 8, count - 8).TrimEnd('\0');

            if (type == 2)
                return Encoding.ASCII.GetString(data, dataOffset, count).TrimEnd('\0');
        }

        return null;
    }
}
