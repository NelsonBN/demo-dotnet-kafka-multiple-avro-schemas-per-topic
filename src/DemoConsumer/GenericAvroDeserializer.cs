using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avro.Generic;
using Avro.IO;
using Avro.Specific;
using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace Demo.Consumer;

public class GenericAvroDeserializer : IAsyncDeserializer<ISpecificRecord>
{
    /// <summary>
    /// Magic byte that identifies an Avro serialized message with Confluent Platform framing.
    /// </summary>
    public const byte MAGIC_BYTE = 0;

    /// <remarks>
    ///     A datum reader cache (one corresponding to each write schema that's been seen) 
    ///     is maintained so that they only need to be constructed once.
    /// </remarks>
    private readonly Dictionary<int, DatumReader<ISpecificRecord>> _datumReaderCache = new();

    private readonly SemaphoreSlim _deserializeMutex = new(1);
    private readonly ISchemaRegistryClient _schemaRegistryClient;

    public GenericAvroDeserializer(ISchemaRegistryClient schemaRegistryClient)
        => _schemaRegistryClient = schemaRegistryClient;


    public async Task<ISpecificRecord> DeserializeAsync(ReadOnlyMemory<byte> data, bool isNull, SerializationContext context)
    {
        if(isNull)
        {
            return null;
        }

        if(data.Length < 5)
        {
            throw new InvalidDataException($"Expecting data framing of length 5 bytes or more but total data size is {data.Length} bytes");
        }

        try
        {
            // Note: topic is not necessary for deserialization (or knowing if it's a key or value) only the schema id is needed.
            using(var stream = new MemoryStream(data.ToArray()))
            using(var reader = new BinaryReader(stream))
            {
                var schemaId = _getSchemaId(reader);

                DatumReader<ISpecificRecord> datumReader;
                await _deserializeMutex.WaitAsync().ConfigureAwait(continueOnCapturedContext: false);

                try
                {
                    _datumReaderCache.TryGetValue(schemaId, out datumReader);
                    if(datumReader is null)
                    {
                        // TODO: If any of this cache fills up, this is probably an
                        // indication of misuse of the deserializer. Ideally we would do 
                        // something more sophisticated than the below + not allow 
                        // the misuse to keep happening without warning.
                        if(_datumReaderCache.Count > _schemaRegistryClient.MaxCachedSchemas)
                        {
                            _datumReaderCache.Clear();
                        }

                        var schema = await _schemaRegistryClient.GetSchemaAsync(schemaId).ConfigureAwait(continueOnCapturedContext: false);
                        if(schema.SchemaType != SchemaType.Avro)
                        {
                            throw new InvalidOperationException("Expecting writer schema to have type Avro, not {schema.SchemaType}");
                        }

                        var avroSchema = Avro.Schema.Parse(schema.SchemaString);

                        datumReader = new SpecificReader<ISpecificRecord>(avroSchema, avroSchema);

                        // Add in cache the SpecificReader based in schema from "SchemaRegistry"
                        _datumReaderCache[schemaId] = datumReader;
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    _deserializeMutex.Release();
                }

                try
                {
                    return datumReader.Read(default, new BinaryDecoder(stream));
                }
                catch(Exception exception)
                {
                    throw new SerializationException(
                        "Could not deserialize message",
                        exception
                    );
                }
            }
        }
        catch(AggregateException exception)
        {
            if(exception.InnerException is null)
            {
                throw;
            }

            throw exception.InnerException;
        }
    }

    private static int _getSchemaId(BinaryReader reader)
    {
        var magicByte = reader.ReadByte();
        if(magicByte != MAGIC_BYTE)
        {
            throw new InvalidDataException($"An '{magicByte}' magic byte was received, only '{MAGIC_BYTE}' is supported");
        }

        var value = reader.ReadInt32();
        var schemaId = BitConverter.IsLittleEndian ?
                       BinaryPrimitives.ReverseEndianness(value) :
                       value;

        return schemaId;
    }
}
