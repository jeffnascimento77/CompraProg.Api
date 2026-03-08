using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CompraProg.Infrastructure.Messaging;

// producer generico: public objetos como JSON.
public interface IKafkaProducer
{
    Task PublishAsync(String topic, object message, CancellationToken ct = default);
}

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private bool _disposed;

    public KafkaProducer(string bootstrapServers, ILogger<KafkaProducer> logger)
    {
        _logger = logger;

        // Configuracao do Producer
        // BootstrapServers = endereco do Kafka
        // Acks.All = espera confirmacao do broker
        // EnableIdempotence = reduz risco de duplicidade em retries

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            //Boa Pratica: garante entrega (trade-off: pode ficar mais lento)
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishAsync(string topic, object message, CancellationToken ct = default)
    {
        //Serializa em Json (simples e aderente ao exemplo anunciado)
        var payload = JsonSerializer.Serialize(message);

        try
        {
            var result = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = payload }, ct);

            _logger.LogInformation("kafka_publish_ok topic={Topic} partition={Partition} offset={Offset}",
                topic, result.Partition, result.Offset);
        }
        catch (ProduceException<Null, string> ex)
        {
            //Erro especifico do Kafka
            _logger.LogError(
                ex,
                "kafka_publish_fail topic={Topic} reason={reason}",
                topic,
                ex.Error.Reason);

            throw;
        }

        catch (Exception ex)
        {
            // Log sem acento, observavel
            _logger.LogError(ex, "kafka_publish_fail topic={Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        //Flush garante envio das mensagens pendentes antes de encerrar
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        _disposed = true;
   }

}

