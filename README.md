# Kafka with multiple Avro schemas per topic in .NET

## Install Avro .NET tools
```bash
dotnet tool install -g Apache.Avro.Tools
```

## Generate Avro classes
```bash
avrogen -s {*.avsc} {output directory}
```

**Example**
```bash
avrogen -s .\src\Common\ModelA.avsc .\src\DemoProducerA\
```


## Run the demo

### Start the Kafka
```bash
docker-compose up
```

### Run the consumer
```bash
cd src/DemoConsumer/
dotnet run
```

### Run the individual producers
```bash
cd src/DemoProducerA/
dotnet run


cd src/DemoProducerB/
dotnet run
```

### Run the Multi producer
```bash
cd src/DemoProducerMulti/
dotnet run
```