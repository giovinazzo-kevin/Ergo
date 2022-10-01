using System.IO;

namespace Ergo.Lang.Utils;

public sealed class ErgoStream : Stream
{
    public readonly string FileName;
    private readonly MemoryStream _stream;

    public event Action Disposing;

    internal ErgoStream(MemoryStream from, string fileName)
    {
        _stream = from;
        FileName = fileName;
    }

    public override bool CanRead => _stream.CanRead;
    public override bool CanSeek => _stream.CanSeek;
    public override bool CanWrite => _stream.CanWrite;
    public override long Length => _stream.Length;
    public override long Position { get => _stream.Position; set => _stream.Position = value; }
    public override void Flush() => _stream.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
    public override void SetLength(long value) => _stream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
    public ErgoStream Clone(bool forwardDispose)
    {
        var newStream = new MemoryStream();
        _stream.CopyTo(newStream);
        var ret = new ErgoStream(newStream, FileName);
        if (forwardDispose)
        {
            ret.Disposing += () => Dispose();
        }
        ret.Seek(0, SeekOrigin.Begin);
        return ret;
    }
}
