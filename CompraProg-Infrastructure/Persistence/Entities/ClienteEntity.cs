using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Persistence.Entities
{
    //Entidade do banco (infra)
    //Mantemos separado do Domain para não depender de EF Core

    public class ClienteEntity
    {
        public long Id { get; set; }

        public String Nome { get; set; } = String.Empty;

        //CPF sem mascará (11 Digitos). Regra RN-002: Unico
        public string CPF { get; set; } = String.Empty;

        public string Email { get; set; } = String.Empty;

        //Valor Mensal do aporte (RN-003 Minimo 100, RN-011 pode alterar)
        public decimal ValorMensal { get; set; }

        //RN 005 Inicia Ativo
        public bool Ativo { get; set; } = true;

        //RN-006 data de adesao registrada
        public DateTime DataAdesao { get; set; } = DateTime.UtcNow;

    }
}
