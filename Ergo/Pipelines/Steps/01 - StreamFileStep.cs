using Ergo.Lang.Utils;
using System.IO;

namespace Ergo;

public interface IStreamFileStep : IErgoPipeline<string, ErgoStream, IStreamFileStep.Env>
{

    public interface Env
    {
    }

}

public class StreamFileStep : IStreamFileStep
{
    public Either<ErgoStream, PipelineError> Run(string fileName, IStreamFileStep.Env env)
    {
        if (!File.Exists(fileName))
            return new PipelineError(this, new FileNotFoundException(null, fileName));
        var fs = EncodedFileStream(File.OpenRead(fileName), closeStream: true);
        fs.Seek(0, SeekOrigin.Begin);
        var stream = new ErgoStream(fs, fileName);
        return stream;
    }

    protected static MemoryStream EncodedFileStream(FileStream file, bool closeStream = true)
    {
        var stream = (Stream)file;
        using (var reader = new StreamReader(file))
        {
            var contents = reader.ReadToEnd();
            var ms = new MemoryStream();
            using var sw = new StreamWriter(ms, leaveOpen: true);
            sw.Write(contents);
            if (closeStream)
                file.Dispose();
            stream = ms;
        }
        stream.Seek(0, SeekOrigin.Begin);
        return (MemoryStream)stream;
    }

}

