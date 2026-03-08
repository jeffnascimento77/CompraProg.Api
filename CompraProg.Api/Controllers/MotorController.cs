using CompraProg.Infrastructure.Messaging;
using CompraProg.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Timers;


namespace CompraProg.Api.Controllers;

[ApiController]
[Route ("api/motor")]

public class MotorController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<MotorController> _logger;
    private readonly IKafkaProducer _kafka;
    private readonly IConfiguration _cfg;


    public MotorController (AppDbContext db, ILogger <MotorController> logger , IKafkaProducer kafka, IConfiguration cfg)
    {
        _db = db; 
        _logger = logger;
        _kafka = kafka;
        _cfg = cfg;
    }

    //POST /api/motor/executar-compra
    [HttpPost("executar-compra")]
    public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequest request)
    {
        var dataRef = request.DataReferencia.Date;

        //Regra RN-020/ 022 /023 (Datas e Dias Uteis)
        //Aqui vamos apenas ajustar se cair em sab/dom.

        var dataExecucao = AjustarParaProximoDiaUtilSeFds(dataRef);

        // Carrega clientes ativos (RN-024)
        var clientes = await _db.Clientes
            .Where(c => c.Ativo)
            .ToListAsync();

        if (clientes.Count == 0)
        {
            return Ok(new
            {
                dataExecucao = dataExecucao,
                totalClientes = 0,
                totalConsolidado = 0,
                ordensCompra = Array.Empty<object>(),
                distribuicoes = Array.Empty<object>(),
                eventosIRPublicados = 0,
                mensagem = "Nenhum cliente ativo para Processar."
            });
        }

        // Cesta Ativa (RN-018)
        var cesta = await _db.Cestas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa);

        if (cesta is null)
        {
            return BadRequest(new { erro = "Nao Existe cesta ativa cadastrada.", codigo = "CESTA_INEXISTENTE" });
        }

        //Valor por cliente na data = valorMensal/3 (RN-023/Rn-025)
        var aportesPorCliente = clientes.Select(c => new
        {
            c.Id,
            c.Nome,
            valorAporte = decimal.Round(c.ValorMensal / 3m, 2)
        }).ToList();

        var totalConsolidado = aportesPorCliente.Sum(x => x.valorAporte);

        //Para cada item da cesta, calcula valor consolidado e quantidade a comprar (TRUNCAR)
        // RN-027/RN-028 (usar fechamento, truncar) - layout? PREULT
        var ordens = new List<object>();
        var ordensParaKafka = new List<OrdemKafkaDto>();

        foreach (var item in cesta.Itens)
        {
            var Ticker = item.Ticker;
            var cot = await _db.Cotacoes
                .Where(x => x.Ticker == Ticker && x.DataPregao <= dataExecucao.Date)
                .OrderByDescending(x => x.DataPregao)
                .FirstOrDefaultAsync();

            if (cot is null)
            {
                // Contrato sugere 404 para cotacao nao encontrada
                return NotFound(new { erro = $"Cotacao nao encontrada para {Ticker} ate a data {dataExecucao:yyyy-MM-dd}.", codigo = "COTACAO_NAO_ENCONTRADA" });
            }

            var preco = cot.PrecoFechamento;

            var valorParaAtivo = totalConsolidado * (item.Percentual / 100);

            //Quantidade inteira (arredonda para baixo)
            var qtd = (int)decimal.Floor(valorParaAtivo / preco);

            // Separacao lote vs fracionario (RN-031/032/033)
            // Lote = multiplos de 100; resto vai com sufixo F (ex: PETR4F)
            var lotes = qtd / 100;
            var qtdLote = lotes * 100;
            var qtdFrac = qtd - qtdLote;

            var detalhes = new List<object>();
            if (qtdLote > 0) detalhes.Add(new { tipo = "LOTE", ticker = Ticker, quantidade = qtdLote });
            if (qtdFrac > 0) detalhes.Add(new { tipo = "FRACIONARIO", ticker = $"{Ticker}F", quantidade = qtdFrac });

            var valorTotal = decimal.Round(qtd * preco, 2);

            ordens.Add(new
            {
                Ticker ,
                quantidadeTotal = qtd,
                detalhes ,
                precoUnitario = preco,
                valorTotal = decimal.Round(qtd * preco, 2)
            });

            // DTO auxiliar para publicar no Kafka depois
            ordensParaKafka.Add(new OrdemKafkaDto
            {
                Ticker = Ticker,
                Quantidade = qtd,
                PrecoUnitario = preco,
                ValorOperacao = valorTotal
            });
        }

        //publicacao KAFKA
        var topic = _cfg["Kafka:TopicIrEventos"];

        const decimal aliquotaDedoDuro = 0.00005m;

        var eventosIRPublicados = 0;
      //int publicados = 0;

        foreach (var ordem in ordensParaKafka)
        {
            //Evento "macro" (ainda nao por cliente)
            //var valorOperacao = ordem.valorTotal;
            var valorIr = Math.Round(ordem.ValorOperacao * aliquotaDedoDuro, 2);

            var evento = new
            {
                tipo = "IR_DEDO_DURO",
                clienteId = 0,
                cpf = "MASTER",
                ticker = ordem.Ticker,
                tipoOperacao = "COMPRA",
                quantidade = ordem.Quantidade,
                precoUnitario = ordem.PrecoUnitario,
                valorOperacao = ordem.ValorOperacao,
                aliquota = aliquotaDedoDuro,
                valorIR = valorIr,
                dataOperacao = DateTime.UtcNow
            };

            try
            {
                await _kafka.PublishAsync(topic!, evento);
                eventosIRPublicados++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "motor_kafka_publish_fail topic={Topic}", topic);
                //Contrato sugere 500 Kafka_Indisponivel
                return StatusCode(500, new { erro = "Erro ao Publicar no Kafka", codigo = "KAFKA_INDISPONIVEL" 
               });
            }
        }

        _logger.LogInformation(
            "motor_execucao_ok dataRef={DataRef} dataExecucao={DataExecucao} totalClientes={TotalClientes} totalConsolidado={TotalConsolidado} eventosIRPublicados={eventosIRPublicados}",
            dataRef, dataExecucao, clientes.Count, totalConsolidado, eventosIRPublicados);

        /* Por enquanto, vamos retornar a estrutura principal (ordensCompra),
         * e na proxima etapa implementamos distribuição + preco medio + residuos.
         O Contrato completo mostra distribuicoes */
        return Ok(new
        {
            dataExecucao ,
            totalClientes = clientes.Count,
            totalConsolidado = decimal.Round(totalConsolidado, 2),
            ordensCompra = ordens,
            //distribuicoes = Array.Empty<object>(),
            eventosIRPublicados,
            mensagem = "Execucao concluida com publicacao no Kafka."            
        });

    }    

    private static DateTime AjustarParaProximoDiaUtilSeFds(DateTime d)
    {
        //Dias uteis simplificados: seg-sex. Se Cair sab/dom, pula para segunda.
        if (d.DayOfWeek == DayOfWeek.Saturday) return d.AddDays(2);
        if (d.DayOfWeek == DayOfWeek.Sunday) return d.AddDays(1);
        return d;
    }   
}

public record ExecutarCompraRequest(DateTime DataReferencia);

// DTO interno para facilicar a publicacao Kafka
public class OrdemKafkaDto
{
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal ValorOperacao { get; set; }
}











