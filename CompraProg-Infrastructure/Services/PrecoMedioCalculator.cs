using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProg.Infrastructure.Services
{
    public static class PrecoMedioCalculator
    {
        public static decimal Calcular (
            int qtdAnterior,
            decimal pmAnterior,
            int qtdNova,
            decimal precoCompra)

        {
            var totalAnterior = qtdAnterior * pmAnterior;

            var totalNovo = qtdNova * precoCompra;

            var qtdTotal = qtdAnterior + qtdNova;

            if (qtdTotal == 0)
                return 0;

            return (totalAnterior + totalNovo) / qtdTotal;
        }
    }
}
