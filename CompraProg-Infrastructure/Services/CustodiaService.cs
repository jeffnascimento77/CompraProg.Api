using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProg.Infrastructure.Persistence;
using CompraProg.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompraProg.Infrastructure.Services;

public class CustodiaService
{

    private readonly AppDbContext _db;

    public CustodiaService(AppDbContext db)
    {
        _db = db; 
    }

    public async Task<CustodiaEntity> ObterOuCriarCustodiaMasterAsync()
    {
        var master = await _db.Custodias
            .Include(x => x.Posicoes)
            .FirstOrDefaultAsync(x => x.IsMaster);

        if (master != null)
            return master;

        master = new CustodiaEntity
        {
            IsMaster = true,
            ClienteId = null,
            DataCriacao = DateTime.UtcNow
        };

        _db.Custodias.Add(master);
        await _db.SaveChangesAsync();

        return master;
    }

    public async Task<CustodiaEntity> ObterOuCriarCustodiaClienteAsync(long clienteId)
    {
        var custodia = await _db.Custodias
            .Include(x => x.Posicoes)
            .FirstOrDefaultAsync(x => !x.IsMaster && x.ClienteId == clienteId);

        if (custodia != null)
            return custodia;

        custodia = new CustodiaEntity
        {
            IsMaster = false,
            ClienteId = clienteId,
            DataCriacao = DateTime.UtcNow
        };

        _db.Custodias.Add(custodia);
        await _db.SaveChangesAsync();

        return custodia;
    }

    public PosicaoEntity ObterOuCriarPosicao (CustodiaEntity custodia, String ticker)
    {
        var posicao = custodia.Posicoes.FirstOrDefault(x => x.Ticker == ticker);

        if (posicao != null)
            return posicao;

        posicao = new PosicaoEntity
        {
            CustodiaId = custodia.Id,
            Ticker = ticker,
            Quantidade = 0,
            PrecoMedio = 0m
        };

        custodia.Posicoes.Add(posicao);
        return posicao;
    }
}
