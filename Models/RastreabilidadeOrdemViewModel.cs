using System;
using System.Collections.Generic;

namespace fabricawebrender.Models
{
    public class RastreabilidadeOrdemViewModel
    {
        public long IdOrdem { get; set; }
        public string? Lote { get; set; }
        public string? Status { get; set; }
        public DateTime? DataCriacao { get; set; }
        public DateTime? DataInicio { get; set; }
        public IReadOnlyList<RastreabilidadeProdutoOrdemViewModel> Produtos { get; set; } =
            new List<RastreabilidadeProdutoOrdemViewModel>();
        public IReadOnlyList<RastreabilidadeMovimentacaoViewModel> Movimentos { get; set; } =
            new List<RastreabilidadeMovimentacaoViewModel>();
        public int QuantidadeMovimentosBobina { get; set; }
        public int QuantidadeMovimentosFardo { get; set; }
        public int QuantidadeMovimentosMateriaPrima { get; set; }
        public decimal TotalBobinas { get; set; }
        public decimal TotalFardos { get; set; }
        public decimal TotalMateriaPrima { get; set; }
        public int LotesBobinaRegistrados { get; set; }
    }

    public class RastreabilidadeProdutoOrdemViewModel
    {
        public long IdInsumo { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal QuantidadeFardos { get; set; }
    }
}
