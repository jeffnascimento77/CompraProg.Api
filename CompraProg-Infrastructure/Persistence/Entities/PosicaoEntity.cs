using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Persistence.Entities; 

public class PosicaoEntity
{

    public long Id { get; set; }

    public long CustodiaId { get; set; }
    public CustodiaEntity Custodia { get; set; }

    public string Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    public decimal PrecoMedio { get; set; }
}
