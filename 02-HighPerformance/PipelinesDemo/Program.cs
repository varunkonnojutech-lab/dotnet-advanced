using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

const int Port = 5005;

using var cts = new CancellationTokenSource();

// ---- Server: accepts one client and echoes everything back through a Pipe ----
var serverTask = Task.Run(async () =>
{
    var listener = new TcpListener(IPAddress.Loopback, Port);
    listener.Start();
    try
    {
        using var client = await listener.AcceptTcpClientAsync(cts.Token);
        using var stream = client.GetStream();
        var pipe = new Pipe();

        // Producer: fill the pipe from the socket.
        var filling = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    Memory<byte> memory = pipe.Writer.GetMemory(512);
                    int bytesRead = await stream.ReadAsync(memory, cts.Token);
                    if (bytesRead == 0)
                        break; // client closed the connection
                    pipe.Writer.Advance(bytesRead);
                    FlushResult flush = await pipe.Writer.FlushAsync(cts.Token);
                    if (flush.IsCompleted)
                        break;
                }
            }
            finally
            {
                await pipe.Writer.CompleteAsync();
            }
        }, cts.Token);

        // Consumer: read segments from the pipe and echo them to the socket.
        // The reader never worries about partial reads — the pipe buffers
        // until the producer completes or the consumer has had enough.
        while (true)
        {
            ReadResult result = await pipe.Reader.ReadAsync(cts.Token);
            ReadOnlySequence<byte> buffer = result.Buffer;
            foreach (ReadOnlyMemory<byte> segment in buffer)
                await stream.WriteAsync(segment, cts.Token);
            pipe.Reader.AdvanceTo(buffer.End);
            if (result.IsCompleted)
                break;
        }
        await pipe.Reader.CompleteAsync();
        await filling;
    }
    finally
    {
        listener.Stop();
    }
}, cts.Token);

await Task.Delay(500, cts.Token); // give the listener a moment to start

// ---- Client: plain StreamReader/Writer. Only the server uses Pipelines. ----
using var tcp = new TcpClient();
await tcp.ConnectAsync(IPAddress.Loopback, Port, cts.Token);
using var netStream = tcp.GetStream();
// No BOM on the wire: the echo must be byte-identical to what we sent.
using var writer = new StreamWriter(netStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };
using var reader = new StreamReader(netStream, Encoding.UTF8);

foreach (string message in new[] { "hello", "pipelines", "echo" })
{
    await writer.WriteLineAsync(message);
    string? echo = await reader.ReadLineAsync();
    Console.WriteLine($"Sent: {message,-10} Received: {echo}");
}

tcp.Close(); // EOF -> the server's fill loop ends -> the server task completes
await serverTask;
Console.WriteLine("Echo server shut down cleanly.");
