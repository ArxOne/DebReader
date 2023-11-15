using System.Formats.Tar;

namespace ArxOne.Debian.Utility;

internal static class TarExtensions
{
    public static IEnumerable<TarEntry> GetEntries(this TarReader reader)
    {
        for (; ; )
        {
            var entry = reader.GetNextEntry();
            if (entry is null)
                break;
            yield return entry;
        }
    }

    public static string GetCleanName(this TarEntry entry)
    {
        if(entry.Name.StartsWith("./"))
            return entry.Name[2..];
        return entry.Name;
    }
}