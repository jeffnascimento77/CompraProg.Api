using CompraProg.Infrastructure.MarketData;
using CompraProg.Infrastructure.Persistence;
using CompraProg.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompraProg.Api.Controllers;

[ApiController]
[Route("api/cotacoes")]
public class CotacoesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CotacoesController> _logger;

    public CotacoesController(AppDbContext db, ILogger<CotacoesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // POST /api/cotacoes/importar-cotahist
    // upload manual do arquivo COTAHIST (txt) para popular o MySQL

    [HttpPost("importar-cotahist")]
    [Consumes("multipart/form-data")]

    public async Task<IActionResult> ImportarCotahist([FromForm] ImportarCotahistRequest request)
    {
        if (request.Arquivo is null || request.Arquivo.Length == 0)
            return BadRequest(new { codigo = "ARQUIVO_INVALIDO", erro = "Arquivo nao informado" });

        int linhasLidas = 0;
        int linhasParseadas = 0;
        int inseridas = 0;
        int ignoradas = 0;
        int linhasNaoDetalhe = 0;
        // int linhasFiltradas = 0;

        DateTime? minData = null;
        DateTime? maxData = null;

        using var stream = request.Arquivo.OpenReadStream();
        using var reader = new StreamReader(stream);

        // Import em lote: vamos acumular e salvar no final
        var cotacoesParaSalvar = new List<CotacaoEntity>(capacity: 10000);

        while (!reader.EndOfStream)
        {
            var linha = await reader.ReadLineAsync();
            linhasLidas++;

            if (string.IsNullOrWhiteSpace(linha))
            {
                ignoradas++;
                continue;
            }

            // Header/trailer ou linha invalida

            if (linha.Length < 2  || linha.Substring(0,2) != "01")
            {
                linhasNaoDetalhe++;
                ignoradas++;
                continue;
            }

            //if (linha is null) continue;

            if (!CotahistParser.TryParseLinha(linha, out var cot))
            {
                ignoradas++;
                continue;
            }

            linhasParseadas++;

            //Reajuste para corrigir erro de maxData = null
            //minData = minData is null ? cot.DataPregao : (cot.DataPregao < minData ? cot.DataPregao : minData);
            //maxData = minData is null ? cot.DataPregao : (cot.DataPregao < maxData ? cot.DataPregao : maxData);

            if (minData is null || cot.DataPregao < minData)
                minData = cot.DataPregao;

            if (maxData is null || cot.DataPregao > maxData)
                maxData = cot.DataPregao;

            cotacoesParaSalvar.Add(cot);
        }

        // Upsert simples: como temos indice unico (Ticker, DataPregao),
        // vamos inserir apenas o que nao existe para evitar estourar duplicidade.
        // (Em massa seria melhor bulk upsert, mas aqui mantemos simples e explicavel.)

        foreach (var c in cotacoesParaSalvar)
        {
            var jaExiste = await _db.Cotacoes.AnyAsync(x => x.Ticker == c.Ticker && x.DataPregao == c.DataPregao);
            if (jaExiste)
            {
                ignoradas++;
                continue;
            }

            _db.Cotacoes.Add(c);
            inseridas++;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "cotahist_import_ok arquivo={Arquivo} linnhasLidas={LinhasLidas} parseadas={Parseadas} inseridas={Inseridas} ignoradas={Ignoradas} dataMin{DataMin} dataMax={DataMax}",
            request.Arquivo.FileName, linhasLidas, linhasParseadas, inseridas, ignoradas, minData, maxData);

        return Ok(new
        {
            arquivos = request.Arquivo.FileName,
            linhasLidas,
            linhasParseadas,
            linhasNaoDetalhe,
            inseridas,
            ignoradas,
            dataMin = minData,
            dataMax = maxData
        });
        
        
    }
}


public record ImportarCotahistRequest(IFormFile Arquivo);