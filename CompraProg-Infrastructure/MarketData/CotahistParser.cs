using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using CompraProg.Infrastructure.Persistence.Entities;
using System.Security.Cryptography.X509Certificates;

namespace CompraProg.Infrastructure.MarketData;

// Parser simples de COTAHIST (campos fixos)
// Extrai DATPRE (3-10), CODBDI (11-12), CODNEG (13-24), TPMERC (25-27), PREULT (109-121)
// conforme layout
public static class CotahistParser
{

    public static bool TryParseLinha (string linha, out CotacaoEntity cotacao)
    {
        cotacao = new CotacaoEntity();

        //Linha precisa ter pelo menos ate PREULT (pos 121)
        if (string.IsNullOrWhiteSpace(linha) || linha.Length < 245)
            return false;

        // TIPREG 1-2: detalhe = "01"
        var tipreg = Sub(linha, 1, 2);
        if (tipreg != "01")
            return false;

        var datpreRaw = Sub(linha, 3, 10); //AAAAMMDD
        var codbdi = Sub(linha, 11, 12); //02 ou 96
        var ticker = Sub(linha, 13, 24).Trim(); //CODNEG
        var tpmerc = Sub(linha, 25, 27); // 010/020
        var preultRaw = Sub(linha, 109, 121); //PREULT


        // Filtros do sistema:
        // BDI - 02 (lote) e 96 (fracionario)
        if (codbdi != "02" && codbdi != "96") return false;

        // Mercado: 010 (vista) e 020 (fracionario)
        if (tpmerc != "010" && tpmerc != "020") return false;

        if (!DateTime.TryParseExact(datpreRaw, "yyyyMMdd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dataPregao))
        {  
            return false; 
        }

        // PREULT vem como inteiro com 2 casas implicitas (dividir por 100)
        if (!long.TryParse(preultRaw.Trim(), out var preultInteiro))
            return false;

        var preco = preultInteiro / 100;

        cotacao = new CotacaoEntity
        {
            Ticker = ticker.ToUpperInvariant(),
            DataPregao = dataPregao.Date,
            PrecoFechamento = preco,
            CodBdi = codbdi,
            TpMerc = tpmerc
        };

        return true;

    }


    // Helper para substring baseado em posicao 1 - Based (como no layout)
    private static string Sub (string s, int ini, int fim)
    {
        var start = ini - 1;
        var len = fim - ini + 1;
        return s.Substring(start, len);
    }




}
