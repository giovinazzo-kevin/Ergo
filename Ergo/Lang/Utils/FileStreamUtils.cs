using System.IO;
using System.Text;

namespace Ergo.Lang.Utils;

public static class FileStreamUtils
{
    public static ErgoStream MemoryStream(string contents, string fileName = "") => new(new MemoryStream(Encoding.UTF8.GetBytes(contents)), fileName);
    public static ErgoStream FileStream(IEnumerable<string> searchDirectories, string module)
    {
        module = module.Replace("/", @"\");
        var i = module.LastIndexOf(@"\");
        var (prefix, name) = i > -1
            ? (module[..(i + 1)], module[(i + 1)..])
            : (string.Empty, module);
        var nameNoExt = Path.GetFileNameWithoutExtension(name);
        var fileName = searchDirectories
            .Select(d => Path.Combine(d, prefix))
            .Where(Directory.Exists)
            .SelectMany(d =>
            {
                try
                {
                    return Directory.EnumerateFiles(d, "*.ergo", SearchOption.AllDirectories);
                }
                catch
                {
                    return Enumerable.Empty<string>();
                }
            })
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(nameNoExt));
        if (fileName is null)
        {
            throw new FileNotFoundException(module);
        }

        var fs = EncodedFileStream(File.OpenRead(fileName), closeStream: true);

        fs.Seek(0, SeekOrigin.Begin);
        return new(fs, fileName);

        static MemoryStream EncodedFileStream(FileStream file, bool closeStream = true)
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
            return (MemoryStream)stream;
        }
    }
}
