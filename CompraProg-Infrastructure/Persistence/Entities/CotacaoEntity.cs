using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Persistence.Entities;

//Cotacao de fechamento (PREULT) por ticker e data de pregao
public class CotacaoEntity
{
    public long Id { get; set; }

    //Ex PETR4, VALE3, PETR4F
    public string Ticker { get; set; } = string.Empty;

    //Data do Pregao (AAAAMMDD)
    public DateTime DataPregao { get; set; }

    //Preco de fechamento (PREULT convertido /100)
    public decimal PrecoFechamento { get; set; }

    //Metadados opcionais, ajudam debug/observability
    public string CodBdi { get; set; } = string.Empty; // Ex: 02 ou 96
    public string TpMerc { get; set; } = string.Empty; // Ex: 010 ou 020

}
