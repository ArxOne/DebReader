using System.Globalization;
using System.Text;
using ArxOne.Debian.IO;
using ArxOne.Debian.Utility;

namespace ArxOne.Debian.Formats.Ar;

// https://en.wikipedia.org/wiki/Ar_(Unix)

public class ArReader
{
    private readonly Stream _inputStream;

    private static readonly byte[] Header = "!<arch>\n"u8.ToArray();

    private static readonly byte[] FileHeaderEnding = "`\n"u8.ToArray();

    public ArEntry? GetNextEntry(bool copyData = false)
    {
        var arEntry = ReadHeader();
        if (arEntry is null)
            return null;
        using var stream = new PartialReadStream(_inputStream, arEntry.Length, (int?)(arEntry.Length % 2));
        if (copyData)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            arEntry.DataStream = memoryStream;
        }
        else
            arEntry.DataStream = stream;
        return arEntry;
    }

    public ArReader(Stream inputStream)
    {
        _inputStream = inputStream;
        var header = new byte[Header.Length];
        inputStream.ReadAll(header, 0, header.Length);
        if (!header.SequenceEqual(Header))
            throw new FormatException("Not an ar stream");
    }

    private ArEntry? ReadHeader()
    {
        var fileIdentifier = ReadString(16);
        if (fileIdentifier is null)
            return null;
        var fileHeader = new ArEntry
        {
            Name = fileIdentifier.TrimEnd('/'),
            ModificationTimestamp = ReadLong(12),
            Uid = ReadLong(6),
            Gid = ReadLong(6),
            Mode = ReadString(8),
            Length = ReadLong(10),
            Ending = ReadBytes(2)
        };
        if (fileHeader.Ending is null || !fileHeader.Ending.SequenceEqual(FileHeaderEnding))
            return null;
        return fileHeader;
    }

    private long ReadLong(int length)
    {
        var s = ReadString(length);
        if (s is null)
            throw new FormatException("Unexpected end of stream on int read");
        if (!long.TryParse(s, 0, CultureInfo.InvariantCulture, out var v))
            throw new FormatException("Invalid integer value");
        return v;
    }

    private byte[]? ReadBytes(int length)
    {
        var bytes = new byte[length];
        var r = _inputStream.ReadAll(bytes, 0, bytes.Length);
        if (r == 0)
            return null;
        if (r != length)
            throw new FormatException("Unexpected end of stream");
        return bytes;
    }

    private string? ReadString(int length)
    {
        var bytes = ReadBytes(length);
        if (bytes is null)
            return null;
        return Encoding.ASCII.GetString(bytes).TrimEnd(' ');
    }
}
