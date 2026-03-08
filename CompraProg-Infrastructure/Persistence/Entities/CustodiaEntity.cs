using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Persistence.Entities;

public class CustodiaEntity
{
    public long Id { get; set; }

    // null = custodia master
    public long? ClienteId { get; set; }

    public bool IsMaster { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public List<PosicaoEntity> Posicoes { get; set; } = new();
}
