// See https://aka.ms/new-console-template for more information

using System.Buffers;
using MemoryPack;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

Console.WriteLine("Hello, World!");

var opts = NatsOpts.Default with
{
    Url = "nats://localhost:4222",
    // AuthOpts = auths, 
    SerializerRegistry = new NatsMessagePackContextSerializerRegistry()
};

await using var natsConnectionPool = new NatsConnectionPool(10, opts);

// var natsConnection = (NatsConnection)natsConnectionPool.GetConnection();
var natsConnection = new NatsConnection(opts);


var js = new NatsJSContext(natsConnection);

var c = await js.CreateOrUpdateConsumerAsync("any-test", new ConsumerConfig("c1"));

await foreach (var msg in c.ConsumeAsync<GatewayMessage>())
{
    Console.WriteLine($"RCV: {msg.Data?.MessageId}");
    await msg.AckAsync();
}





[MemoryPackable(SerializeLayout.Explicit)]
public partial class GatewayMessage(int messageId)
{
    [MemoryPackOrder(0)]
    public int MessageId { get; set; } = messageId;
}

public sealed class NatsMessagePackContextSerializer<T> :
    INatsSerializer<T>
{
    public static readonly INatsSerializer<T> Default = new NatsMessagePackContextSerializer<T>();
    
    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        MemoryPackSerializer.Serialize(bufferWriter, value);
    }

    public T Deserialize(in ReadOnlySequence<byte> buffer) => MemoryPackSerializer.Deserialize<T>(buffer);
}

public sealed class NatsMessagePackContextSerializerRegistry : INatsSerializerRegistry
{
    public INatsSerialize<T> GetSerializer<T>() => NatsMessagePackContextSerializer<T>.Default;

    public INatsDeserialize<T> GetDeserializer<T>() => NatsMessagePackContextSerializer<T>.Default;
}
