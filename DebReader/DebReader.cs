using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using ArxOne.Debian.Formats.Ar;
using ArxOne.Debian.Utility;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Xz;

namespace ArxOne.Debian;

// https://tldp.org/HOWTO/html_single/Debian-Binary-Package-Building-HOWTO/

public class DebReader
{
    private readonly ArReader _arReader;

    public DebReader(Stream inputStream)
    {
        _arReader = new ArReader(inputStream);
    }

    public (IReadOnlyDictionary<string, string> Control, IReadOnlyCollection<string> Files) Read()
    {
        IReadOnlyDictionary<string, string>? control = null;
        IReadOnlyCollection<string>? files = null;
        foreach (var entry in _arReader.GetEntries())
        {
            _ = IfMatch(entry, "debian-binary", ReadDebianBinary)
                || IfMatch(entry, "control.tar", stream => ReadControlTar(stream, out control))
                || IfMatch(entry, "data.tar", stream => ReadDataTar(stream, out files));
        }

        if (control is null)
            throw new FormatException("No control found");
        if (files is null)
            throw new FormatException("No data.tar found");
        return (control, files);
    }

    private static void ReadDebianBinary(Stream inputStream)
    {
        using var reader = new StreamReader(inputStream, Encoding.ASCII);
        var version = reader.ReadLine();
        if (version != "2.0")
            throw new NotSupportedException($"Unsupported package version: {version}");
    }

    private static void ReadControlTar(Stream inputStream, out IReadOnlyDictionary<string, string>? control)
    {
        var fields = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        using var tarReader = new TarReader(inputStream);
        var controlEntry = tarReader.GetEntries().FirstOrDefault(e => e.GetCleanName() == "control");
        if (controlEntry?.DataStream is null)
            throw new FormatException("No control file found");
        using var controlStreamReader = new StreamReader(controlEntry.DataStream);
        var controlText = controlStreamReader.ReadToEnd();
        fields[""] = controlText;
        using var controlTextReader = new StringReader(controlText);
        foreach (var (key, values) in ReadKeyValues(controlTextReader))
            fields[key] = string.Join(Environment.NewLine, values);
        control = fields.AsReadOnly();
    }

    private static IEnumerable<(string Key, IReadOnlyList<string> Values)> ReadKeyValues(TextReader controlReader)
    {
        string? currentKey = null;
        var values = new List<string>();
        foreach (var (key, value) in ReadKeyValue(controlReader))
        {
            if (key is not null && key != currentKey)
            {
                if (currentKey is not null)
                    yield return (currentKey, values);
                currentKey = key;
                values = new() { value };
            }
            else
            {
                values.Add(value);
            }
        }
        if (values.Count > 0 && currentKey is not null)
            yield return (currentKey, values);
    }

    private static IEnumerable<(string? Key, string Value)> ReadKeyValue(TextReader controlReader)
    {
        for (; ; )
        {
            var line = controlReader.ReadLine();
            if (line is null)
                break;
            if (line == "")
                continue;
            if (line.StartsWith(' '))
                yield return (null, line.TrimStart(' '));
            else
            {
                var index = line.IndexOf(':');
                if (index != -1)
                    yield return (line[..index], line[(index + 1)..].Trim());
            }
        }
    }

    private static void ReadDataTar(Stream inputStream, out IReadOnlyCollection<string>? files)
    {
        var filesList = new List<string>();
        using var tarReader = new TarReader(inputStream);
        foreach (var entry in tarReader.GetEntries())
            filesList.Add(entry.GetCleanName());
        files = filesList.ToArray();
    }

    private static bool IfMatch(ArEntry entry, string expectedName, Action<Stream> action)
    {
        if (entry.DataStream is null)
            return false;

        if (entry.Name == expectedName)
        {
            action(entry.DataStream);
            return true;
        }

        if (entry.Name.StartsWith(expectedName + "."))
        {
            var compressionType = entry.Name[(expectedName.Length + 1)..];
            using var stream = GetCompressionStream(entry.DataStream, compressionType);
            action(stream);
            return true;
        }

        return false;
    }

    private static Stream GetCompressionStream(Stream inputStream, string compressionType)
    {
        return compressionType switch
        {
            "gzip" or "gz" => new GZipStream(inputStream, CompressionMode.Decompress),
            "xz" => new XZStream(inputStream),
            "bzip2" => new BZip2Stream(inputStream, SharpCompress.Compressors.CompressionMode.Decompress, true),
            _ => throw new NotSupportedException($"Unsupported extension {compressionType}")
        };
    }
}
