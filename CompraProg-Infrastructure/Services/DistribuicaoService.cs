using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Services
{
    public class DistribuicaoService
    {
        public Dictionary<long, int> Distribuir(
            int quantidadeTotal,
            Dictionary<long, decimal> proporcoes)
        {
            var resultado = new Dictionary<long, int>();

            foreach (var p in proporcoes)
            {
                var qtd = (int)Math.Floor(quantidadeTotal * p.Value);
                resultado[p.Key] = qtd;
            }

            return resultado;

        }

    }
}
