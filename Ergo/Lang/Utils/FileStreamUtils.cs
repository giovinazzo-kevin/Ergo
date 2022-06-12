using System.IO;
using System.Text;

namespace Ergo.Lang.Utils;

public static class FileStreamUtils
{
    public static Stream MemoryStream(string contents) => new MemoryStream(Encoding.UTF8.GetBytes(contents));

    public static Stream EncodedFileStream(FileStream file, bool closeStream = true)
    {
        var stream = (Stream)file;
        using (var reader = new StreamReader(file))
        {
            var contents = reader.ReadToEnd();
            var ms = new MemoryStream();
            using var sw = new StreamWriter(ms, leaveOpen: true);
            sw.Write(contents);
            if (closeStream)
            {
                file.Dispose();
            }

            stream = ms;
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
