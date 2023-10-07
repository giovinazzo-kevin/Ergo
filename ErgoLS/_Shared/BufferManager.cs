// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;
class BufferManager
{
    private ConcurrentDictionary<string, StringBuffer> _buffers = new ConcurrentDictionary<string, StringBuffer>();

    public void UpdateBuffer(string documentPath, StringBuffer buffer)
    {
        _buffers.AddOrUpdate(documentPath, buffer, (k, v) => buffer);
    }

    public StringBuffer? GetBuffer(string documentPath)
    {
        return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : null;
    }
}