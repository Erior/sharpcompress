using System;
using System.IO;

namespace SharpCompress.Common.Zip;
internal class PostDataDescriptorStream : Stream
{
    private readonly Stream stream;
    private bool isDisposed;
    private long position;

    public PostDataDescriptorStream(Stream stream)
    {
        if(!stream.CanSeek)
        {
            throw new NotSupportedException();
        }

        this.stream = stream;
        this.position = stream.Position;
    }

    protected override void Dispose(bool disposing)
    {
        if (isDisposed)
        {
            return;
        }
        isDisposed = true;
        base.Dispose(disposing);
        if (disposing)
        {
            stream.Dispose();
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override void Flush() => throw new NotSupportedException();
    public override long Length => stream.Length;
    public override long Position
    {
        get => stream.Position;
        set => throw new NotSupportedException();
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        if(count < 16)
        {
            return 0;
        }

        var length = stream.Read(buffer, 0, count);

        byte[] sdh = { 0x50, 0x4b, 0x07, 0x08 };
        int sdh_pos = 0;

        for(int i = 0; i < length - 4; i++)
        {
            if(buffer[i] == sdh[sdh_pos])
            {
                if(sdh_pos == 3)
                {
                    var current = stream.Position;
                    stream.Position -= (length - (i + 1));
                    var reader = new BinaryReader(stream);
                    var crc = reader.ReadUInt32();
                    var compress_length = reader.ReadUInt32();

                    if(compress_length == stream.Position - position - 12)
                    {
                        stream.Position = position + compress_length;
                        return (length - i - 3);
                    }

                    // Not right
                    stream.Position = current;
                    sdh_pos = 0;
                }
                else
                {
                    sdh_pos++;
                }
            }
            else
            {
                sdh_pos = 0;
            }
        }

        stream.Position -= 4;
        return length - 4;
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
