using System;
using System.IO;
using ArxOne.Debian.Utility;

namespace ArxOne.Debian.IO;

internal class PartialReadStream : Stream
{
    private readonly long _start;
    private long _position = 0;
    private readonly Stream _stream;
    private readonly int? _disposePadding;

    public override bool CanRead => true;
    public override bool CanSeek => _stream.CanSeek;
    public override bool CanWrite => false;
    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public PartialReadStream(Stream stream, long length, int? disposePadding)
    {
        _stream = stream;
        _disposePadding = disposePadding;
        _start = stream.CanSeek ? stream.Position : 0;
        Length = length;
    }

    protected override void Dispose(bool disposing)
    {
        if(_disposePadding.HasValue)
            SeekToEnd(_disposePadding.Value);
        base.Dispose(disposing);
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

    public void SeekToEnd(int padding)
    {
        if (CanSeek)
        {
            Seek(0, SeekOrigin.End);
            _stream.Seek(padding, SeekOrigin.Current);
            return;
        }

        var buffer = new byte[1024];
        while (this.ReadAll(buffer, 0, buffer.Length) > 0)
        {
            // it’s all in the condition
        }
        _stream.ReadAll(buffer, 0, padding);
    }
}