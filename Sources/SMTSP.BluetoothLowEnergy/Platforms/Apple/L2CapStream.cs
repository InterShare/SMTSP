namespace SMTSP.BluetoothLowEnergy;

public class L2CapStream(NSOutputStream outputStream, NSInputStream inputStream) : Stream
{
    private readonly List<byte> _buffer = [];

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        Write(_buffer.ToArray(), 0, _buffer.Count);
        _buffer.Clear();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return inputStream.Read(buffer, offset, new UIntPtr((ulong)count)).ToInt32();
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
        var stillToSend = new byte[buffer.Length - offset];
        buffer.CopyTo(stillToSend, offset);

        while (count > 0)
        {
            if (!outputStream.HasSpaceAvailable())
            {
                continue;
            }

            var bytesWritten = outputStream.Write(buffer, offset, (uint)count);

            if (bytesWritten == -1)
            {
                throw new InvalidOperationException("Write error: -1 returned");
            }

            if (bytesWritten > 0)
            {
                count -= (int)bytesWritten;

                if (0 == count)
                {
                    break;
                }

                var temp = new List<byte>();

                for (var i = bytesWritten; i < stillToSend.Length; i++)
                {
                    temp.Add(stillToSend[i]);
                }

                stillToSend = temp.ToArray();
            }
        }
    }

    public override void WriteByte(byte value)
    {
        _buffer.Add(value);
    }
}
