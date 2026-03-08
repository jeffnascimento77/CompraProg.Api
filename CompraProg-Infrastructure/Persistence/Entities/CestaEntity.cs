using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Persistence.Entities;

// Representa uma cesta (Top Five) no banco
// Regras: so 1 ativa por vez, e ao trocar, a anterior e desativada

public class CestaEntity
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    // "Ativa" para consulta rapida da cesta vigente
    public bool Ativa { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Quando uma nova cesta entra, desativamos a anterior preenchendo esta data
    public DateTime? DataDesativacao { get; set; }

    // Relacao 1:N com os itens (5 ativos)
    public List<ItemCestaEntity> Itens { get; set; } = new();
}
