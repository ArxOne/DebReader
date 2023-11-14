namespace ArxOne.Debian.IO;

internal class PartialReadStream : Stream
{
    private readonly Stream _stream;
    private readonly long _start;
    private long _position;

    public override bool CanRead => true;
    public override bool CanSeek => _stream.CanSeek;
    public override bool CanWrite => false;
    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public PartialReadStream(Stream stream, long length)
    {
        _stream = stream;
        Length = length;
        _start = stream.CanSeek ? stream.Position : 0;
        _position = 0;
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = (int)Math.Min(count, Length - _position);
        if (remaining == 0)
            return 0;
        var bytesRead = _stream.Read(buffer, offset, remaining);
        _position += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (!CanSeek)
            throw new NotSupportedException();
        _position = Math.Min(Length, Math.Max(0, origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        }));
        _stream.Position = _position + _start;
        return _position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}