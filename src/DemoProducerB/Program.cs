using System;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using DemoModel;
using static System.Console;


WriteLine(": : : PRODUCER B : : :");


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


using var producer = new ProducerBuilder<string, ModelB>(config)
    .SetValueSerializer(new AvroSerializer<ModelB>(
        schemaRegistry,
        new AvroSerializerConfig
        {
            BufferBytes = 100,
            SubjectNameStrategy = SubjectNameStrategy.Record
        })
    ).Build();

var message = new Message<string, ModelB>
{
    Key = Guid.NewGuid().ToString(),
    Value = new ModelB
    {
        Id = Guid.NewGuid().ToString(),
        Name = "B " + DateTime.UtcNow.ToString(),
        Age = 10,
    }
};


var result = await producer.ProduceAsync("demo-topic", message);


WriteLine($">>> Message sent {result.Partition}/{result.Offset}");
