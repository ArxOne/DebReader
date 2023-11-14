namespace ArxOne.Debian.IO;

internal class PartialReadStream : Stream
{
    private readonly Stream _stream;
    private readonly long _start;
    private long _position;

    public override bool CanRead { get; }
    public override bool CanSeek { get; }
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
        Position = 0;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = (int)Math.Min(count, Length - Position);
        if (remaining == 0)
            return 0;
        var bytesRead = _stream.Read(buffer, offset, remaining);
        Position += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if(!CanSeek)
            throw new NotSupportedException();
        throw new NotImplementedException();
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