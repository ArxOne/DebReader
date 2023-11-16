using System.IO;

namespace ArxOne.Debian.Utility;

internal static class StreamExtensions
{
    public static int ReadAll(this Stream stream, byte[] buffer, int offset, int count)
    {
        var totalRead = 0;
        while (count > 0)
        {
            var r = stream.Read(buffer, offset, count);
            if (r == 0)
                break;
            count -= r;
            totalRead += r;
            if (count == 0)
                break;
            offset += r;
        }
        return totalRead;
    }
}