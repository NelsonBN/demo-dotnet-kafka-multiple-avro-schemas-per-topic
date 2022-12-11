using System;
using Avro.Specific;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using DemoModel;
using static System.Console;

WriteLine(": : : PRODUCER Multi : : :");


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


using var producer = new ProducerBuilder<string, ISpecificRecord>(config)
    .SetValueSerializer(new AvroSerializer<ISpecificRecord>(
        schemaRegistry,
        new AvroSerializerConfig
        {
            BufferBytes = 100,
            SubjectNameStrategy = SubjectNameStrategy.Record
        })
    ).Build();

var messageA = new Message<string, ISpecificRecord>
{
    Key = Guid.NewGuid().ToString(),
    Value = new ModelA
    {
        Id = Guid.NewGuid().ToString(),
        Username = "A " + DateTime.UtcNow.ToString()
    }
};

var resultA = await producer.ProduceAsync("demo-topic", messageA);

WriteLine($">>> Message A sent {resultA.Partition}/{resultA.Offset}");

var messageB = new Message<string, ISpecificRecord>
{
    Key = Guid.NewGuid().ToString(),
    Value = new ModelB
    {
        Id = Guid.NewGuid().ToString(),
        Name = "B " + DateTime.UtcNow.ToString(),
        Age = 10,
    }
};

var resultB = await producer.ProduceAsync("demo-topic", messageB);

WriteLine($">>> Message B sent {resultB.Partition}/{resultB.Offset}");
