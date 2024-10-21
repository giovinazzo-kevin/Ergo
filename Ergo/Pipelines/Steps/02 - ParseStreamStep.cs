using Ergo.Lang.Parser;
using Ergo.Lang.Utils;
using System.IO;

namespace Ergo;

public interface IParseStreamStep : IErgoPipeline<ErgoStream, LegacyErgoParser, IParseStreamStep.Env>
{

    public interface Env
    {
        ISet<Operator> Operators { get; }
    }

}

public class ParseStreamStep(IEnumerable<IAbstractTermParser> abstractTermParsers) : IParseStreamStep
{
    public Either<LegacyErgoParser, PipelineError> Run(ErgoStream stream, IParseStreamStep.Env env)
    {
        var lexer = new ErgoLexer(stream, env.Operators);
        var parser = new LegacyErgoParser(lexer);
        foreach (var abs in abstractTermParsers)
            parser.AddAbstractParser(abs);
        return parser;
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

