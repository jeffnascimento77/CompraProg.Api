using CompraProg.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace CompraProg.Api.Controllers;

[ApiController]
[Route("api/kafka")]

public class KafkaController : ControllerBase
{
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaController> _logger;

    public KafkaController(
        IKafkaProducer kafkaProducer,
        IConfiguration configuration,
        ILogger<KafkaController> logger)

    {
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
        _logger = logger;
    }

    // Endpoint para validar rapidamente se a publicacao no kafka esta funcionando.
    // Ele monta um evento simples e envia para o topico configurado no appsettings

    [HttpPost("teste")]
    public async Task<IActionResult> PublicarTeste([FromBody] KafkaTesteRequest request)
    {
        var topic = _configuration["Kafka:TopicIrEventos"];

        if (string.IsNullOrWhiteSpace(topic))
        {
            return StatusCode(500, new
            {
                codigo = "KAFKA_CONFIG_INVALIDA",
                erro = "Topico Kafka nao configurado no appsettings."
            });
        }

        // Evento de Teste
        var evento = new
        {
            tipo = "TESTE_KAFKA",
            origem = "api_kafka_teste",
            mensagem = request.Mensagem,
            dataEnvio = DateTime.UtcNow
        };

        try
        {
            await _kafkaProducer.PublishAsync(topic, evento);

            _logger.LogInformation(
                   "kafka_teste_ok topic={Topic} mensagem={Mensagem}",
                   topic,
                   request.Mensagem);

            return Ok(new
            {
                sucesso = true,
                topic,
                evento
            });
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "kafka_teste_fail topic={Topic}", topic);

            return StatusCode(500, new
            {
                codigo = "KAFKA_INDISPONIVEL",
                erro = "Erro ao publicar mensagem no Kafka."
            });
        }
    }
}

//DTO do endpoint de teste
public record KafkaTesteRequest(string Mensagem);