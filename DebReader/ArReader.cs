using System.Globalization;
using System.Text;
using ArxOne.Debian.IO;
using ArxOne.Debian.Utility;

namespace ArxOne.Debian;

// https://en.wikipedia.org/wiki/Ar_(Unix)

public interface IArEntry
{
    string FileName { get; }
}

public class ArReader
{
    private readonly Stream _inputStream;

    private static readonly byte[] Header =
    {
        (byte) '!', (byte) '<', (byte) 'a', (byte) 'r', (byte) 'c', (byte) 'h', (byte) '>', (byte) '\n'
    };

    private static readonly byte[] FileHeaderEnding = new byte[] { 0x60, 0x0A };

    private sealed record FileHeader : IArEntry
    {
        public string FileIdentifier { get; init; }
        public long FileModificationTimestamp { get; init; }
        public long OwnerID { get; init; }
        public long GroupID { get; init; }
        public string FileMode { get; init; }
        public long FileSize { get; init; }
        public byte[] Ending { get; init; }
        public string FileName => FileIdentifier;
    }

    public IEnumerable<(IArEntry Entry, Stream ContentStream)> ReadContent()
    {
        for (; ; )
        {
            var fileHeader = ReadHeader();
            if (fileHeader is null)
                yield break;
            using var stream = new PartialReadStream(_inputStream, fileHeader.FileSize, (int?)(fileHeader.FileSize % 2));
            yield return (fileHeader, stream);
        }
    }

    public ArReader(Stream inputStream)
    {
        _inputStream = inputStream;
        var header = new byte[Header.Length];
        inputStream.ReadAll(header, 0, header.Length);
        if (!header.SequenceEqual(Header))
            throw new FormatException("Not an ar stream");
    }

    private FileHeader? ReadHeader()
    {
        var fileIdentifier = ReadString(16);
        if (fileIdentifier is null)
            return null;
        var fileHeader = new FileHeader
        {
            FileIdentifier = fileIdentifier.TrimEnd('/'),
            FileModificationTimestamp = ReadLong(12),
            OwnerID = ReadLong(6),
            GroupID = ReadLong(6),
            FileMode = ReadString(8),
            FileSize = ReadLong(10),
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
