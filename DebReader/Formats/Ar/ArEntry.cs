using System;
using System.IO;

namespace ArxOne.Debian.Formats.Ar;

public sealed record ArEntry(string Name)
{
    public long ModificationTimestamp { get; init; }
    public DateTimeOffset ModificationTime => DateTimeOffset.FromUnixTimeSeconds(ModificationTimestamp);
    public long Uid { get; init; }
    public long Gid { get; init; }
    // TODO --> UnixFileMode
    public string? Mode { get; init; }
    public long Length { get; init; }
    internal byte[]? Ending { get; init; }
    public Stream? DataStream { get; internal set; }
}
