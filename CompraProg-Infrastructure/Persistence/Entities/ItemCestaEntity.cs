using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Persistence.Entities;

// Item da Cesta: ticker + percentual
// Ex: PETR4 30.00

public class ItemCestaEntity
{
    public long Id { get; set; }
    
    public long CestaId { get; set; }

    public CestaEntity? Cesta { get; set; }

    public string Ticker { get; set; } = string.Empty;

    public decimal Percentual { get; set; } // 0-100
}
