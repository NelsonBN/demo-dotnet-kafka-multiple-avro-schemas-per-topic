using System;
using System.Text.Json;
using System.Threading;
using Avro.Specific;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Demo.Consumer;
using DemoModel;
using static System.Console;


WriteLine(": : : CONSUMER : : :");


var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = Guid.NewGuid().ToString(),
    AutoOffsetReset = AutoOffsetReset.Latest,
    EnablePartitionEof = true,
};

var schemaRegistryConfig = new SchemaRegistryConfig
{
    Url = "http://localhost:8081"
};

var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

var consumer = new ConsumerBuilder<string, ISpecificRecord>(config)
    .SetValueDeserializer(new GenericAvroDeserializer(schemaRegistry).AsSyncOverAsync())
    .Build();

var options = new JsonSerializerOptions
{
    WriteIndented = true
};


consumer.Subscribe("demo-topic");

var cancellationTokenSource = new CancellationTokenSource();

WriteLine(">>> Waiting messages...");
while(!cancellationTokenSource.Token.IsCancellationRequested)
{
    try
    {
        var result = consumer.Consume();
        if(result.IsPartitionEOF)
        {
            continue;
        }

        switch(result.Message.Value)
        {
            case ModelA model:
                WriteLine($"Offset: '{result.TopicPartitionOffset}' >>> Key '{result.Message.Key}' >>> Value ModelA '{JsonSerializer.Serialize(model, options)}'");
                break;

            case ModelB model:
                WriteLine($"Offset: '{result.TopicPartitionOffset}' >>> Key '{result.Message.Key}' >>> Value ModelB '{JsonSerializer.Serialize(model, options)}'");
                break;

            default:
                throw new InvalidCastException("Message not supported");
        }
    }
    catch(ConsumeException exception)
    {
        WriteLine($"Error trying consume an message {exception}");
    }
    catch(Exception exception)
    {
        WriteLine($"Error in consume {exception}");
    }
}


ReadLine();

cancellationTokenSource.Cancel();
