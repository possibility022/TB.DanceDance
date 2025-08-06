namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class MemoryStreamWrapper : Stream
{
    private readonly MemoryStream innerStream;
    private readonly long waitAt;
    private readonly CancellationTokenSource cancellationTokenSource;

    public MemoryStreamWrapper(MemoryStream innerStream, long waitAt, CancellationTokenSource cancellationTokenSource)
    {
        this.innerStream = innerStream;
        this.waitAt = waitAt;
        this.cancellationTokenSource = cancellationTokenSource;
    }

    public override void Flush()
    {
        innerStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (this.innerStream.Position + count >= waitAt)
        {
            cancellationTokenSource.Cancel();
        }

        return innerStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        innerStream.Write(buffer, offset, count);
    }

    public override bool CanRead => innerStream.CanRead;
    public override bool CanSeek => innerStream.CanSeek;
    public override bool CanWrite => innerStream.CanWrite;
    public override long Length => innerStream.Length;
    public override long Position { get => innerStream.Position; set => innerStream.Position = value; }
}