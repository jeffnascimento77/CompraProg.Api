using CompraProg.Infrastructure.Persistence;
using CompraProg.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompraProg.Api.Controllers;

[ApiController]
[Route("api/admin/cesta")]

public class AdminCestaController : ControllerBase
{
    public readonly AppDbContext _db;
    public readonly ILogger<AdminCestaController> _logger;


    public AdminCestaController(AppDbContext db, ILogger<AdminCestaController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // POST /api/admin/cesta
    // Contrato: cadastra ou altera cesta Top Five :contentReference[oaicite:3]{index:3}
    [HttpPost]
    public async Task<IActionResult> CriarOuAlterar([FromBody] CestaRequest request)
    {
        //RN-014: exatamente 5 acoes :contentReference[oiacite:4]{index=4}
        if (request.Itens.Count != 5)
        {
            return BadRequest(new
            {
                erro = $"A Cesta deve conter exatamente 5 ativos. Quantidade Informada: {request.Itens.Count}.",
                codigo = "QUANTIDADE_ATIVOS_INVALIDA"
            });
        }

        //RN-016 Cada Percentual > 0
        if (request.Itens.Any(i => i.Percentual <= 0))
        {
            return BadRequest(new
            {
                erro = "Cada percentual deve ser maior que 0%.",
                codigo = "PERCENTUAIS_INVALIDOS"
            });
        }

        //RN-015 Soma Exatamente 100
        var soma = request.Itens.Sum(i => i.Percentual);
        if (soma != 100m)
        {
            return BadRequest(new
            {
                erro = $"A Soma dos percentuais deve ser exaamente 100%. Soma Atual: {soma}%.",
                codigo = "PERCENTUAIS_INVALIDOS"                
            });
        }

        // Evita Ticker Duplicado dentro da Cesta (Boa Pratica)
        var tickers = request.Itens.Select(i => i.Ticker.Trim().ToUpperInvariant()).ToList();
        if (tickers.Distinct().Count() != 5) 
        {
            return BadRequest(new
            {
                erro = "Nao pode haver Ticker duplicado na Cesta.",
                codigo = "TICKERS_DUPLICADOS"
            });
        }

        //Busca cesta ativa autal (caso exista)
        var cestaAtiva = await _db.Cestas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa);

        var agora = DateTime.UtcNow;

        //Se ja existe uma ativa: Desativa e cria uma nova
        //RN 017/ RN-018

        CestaEntity? anterior = null;
        var primeiraCesta = cestaAtiva is null;

        if (cestaAtiva is not null)
        {
            cestaAtiva.Ativa = false;
            cestaAtiva.DataDesativacao = agora;
            anterior = cestaAtiva;

            _logger.LogInformation("cesta_desativada cestaId={CestaId} data={Data}",
                cestaAtiva.Id, agora);
        }

        var nova = new CestaEntity
        {
            Nome = request.Nome.Trim(),
            Ativa = true,
            DataCriacao = agora,
            Itens = request.Itens.Select(i => new ItemCestaEntity
            {
                Ticker = i.Ticker.Trim().ToUpperInvariant(),
                Percentual = i.Percentual
            }).ToList()
        };

        _db.Cestas.Add(nova);
        await _db.SaveChangesAsync();

        /* Neste Ponto, RN-019 diz que alterar cesta disara Rebalanceamento
           ainda nao vamos implementar o rebalanceamento aqui, so vamos sinalizar "false/true" */

        var rebalanceamentoDisparado = !primeiraCesta;

        _logger.LogInformation("cesta_criada_ok cestaId={CestaId} ativa={Ativa} rebalanceamentoDisparado={Reb}",
            nova.Id, nova.Ativa, rebalanceamentoDisparado);

        // Para responder parecido com o exemplo do contrato :contentReference[oaicite:7]{index=7} :contentReference[oaicite:8]{index=8}
        return StatusCode(201, new
        {
            cestaId = nova.Id,
            nome = nova.Nome,
            ativa = nova.Ativa,
            dataCriacao = nova.DataCriacao,
            itens = nova.Itens.Select(x => new { ticker = x.Ticker, percentual = x.Percentual }),
            cestaAnteriorDesativada = anterior is null ? null : new
            {
                cestaId = anterior.Id,
                nome = anterior.Nome,
                dataDesativacao = anterior.DataDesativacao
            },
            rebalanceamentoDisparado,
            // Por enquanto deixamos como listas vazias; depois calculamos no rebalanceamento.
            ativosRemovidos = Array.Empty<string>(),
            ativosAdicionados = Array.Empty<string>(),
            mensagem = primeiraCesta
                ? "Primeira cesta cadastrada com sucesso."
                : "Cesta atualizada. Rebalanceamento sera implementado na proxima etapa."
        });
    }

    // GET /api/admin/cesta/atual :contentReference[oaicite:9]{index=9}
    [HttpGet("atual")]
    public async Task<IActionResult> ConsultarAtual()
    {
        var cesta = await _db.Cestas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa);

        if (cesta is null)
        {
            return NotFound(new { erro = "Nenhuma cesta ativa encontrada.", codigo = "CESTA_NAO_ENCONTRADA" });
        }

        // O contrato mostra cotacaoAtual junto, mas isso depende do provedor de cotacoes.
        // Por enquanto retornamos sem cotacaoAtual.
        return Ok(new
        {
            cestaId = cesta.Id,
            nome = cesta.Nome,
            ativa = cesta.Ativa,
            dataCriacao = cesta.DataCriacao,
            itens = cesta.Itens.Select(x => new { ticker = x.Ticker, percentual = x.Percentual })
        });
    }
}

// DTOs (entrada)
public record CestaRequest(string Nome, List<CestaItemRequest> Itens);
public record CestaItemRequest(string Ticker, decimal Percentual);
