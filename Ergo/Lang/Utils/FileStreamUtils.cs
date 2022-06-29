using System.IO;
using System.Text;

namespace Ergo.Lang.Utils;

public static class FileStreamUtils
{
    public static ErgoStream MemoryStream(string contents, string fileName = "") => new(new MemoryStream(Encoding.UTF8.GetBytes(contents)), fileName);
    public static ErgoStream FileStream(IEnumerable<string> searchDirectories, string module)
    {
        var fileName = searchDirectories
            .SelectMany(d => Directory.EnumerateFiles(d, "*.ergo", SearchOption.AllDirectories))
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(module));
        if (fileName is null)
        {
            throw new FileNotFoundException(module);
        }

        var fs = EncodedFileStream(File.OpenRead(fileName), closeStream: true);

        fs.Seek(0, SeekOrigin.Begin);
        return new(fs, fileName);

        static Stream EncodedFileStream(FileStream file, bool closeStream = true)
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
}
