using System;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using DemoModel;
using static System.Console;


WriteLine(": : : PRODUCER A : : :");


var config = new ProducerConfig
{
    BootstrapServers = "localhost:9092",
    Acks = Acks.All,
};
var schemaRegistryConfig = new SchemaRegistryConfig
{
    Url = "http://localhost:8081"
};

var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);


using var producer = new ProducerBuilder<string, ModelA>(config)
    .SetValueSerializer(new AvroSerializer<ModelA>(
        schemaRegistry,
        new AvroSerializerConfig
        {
            BufferBytes = 100,
            SubjectNameStrategy = SubjectNameStrategy.Record
        })
    ).Build();

var message = new Message<string, ModelA>
{
    Key = Guid.NewGuid().ToString(),
    Value = new ModelA
    {
        Id = Guid.NewGuid().ToString(),
        Username = "A " + DateTime.UtcNow.ToString()
    }
};

var result = await producer.ProduceAsync("demo-topic", message);


WriteLine($">>> Message sent {result.Partition}/{result.Offset}");
