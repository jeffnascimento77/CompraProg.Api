using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Persistence.Entities;

public class MovimentoEntity
{
    public long Id { get; set; }
    public long CustodiaId { get; set; }
    public String Ticker { get; set; } = string.Empty;

    public int Quantidade { get; set; }
    
    public decimal Preco {  get; set; }

    public string Tipo { get; set; } = string.Empty; // Compra / Venda

    public DateTime DataMovimento { get; set; } = DateTime.UtcNow;
}
