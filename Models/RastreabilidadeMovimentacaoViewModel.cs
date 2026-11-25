namespace fabricawebrender.Models
{
    public class RastreabilidadeMovimentacaoViewModel
    {
        public long IdMovimentacao { get; set; }
        public long? IdOrdem { get; set; }
        public string? LoteOrdem { get; set; }
        public long IdInsumo { get; set; }
        public long? IdProduto { get; set; }
        public string? Insumo { get; set; }
        public string? Categoria { get; set; }
        public string? UnidadeMedida { get; set; }
        public decimal QuantidadeConsumida { get; set; }
        public DateTime DataMovimentacao { get; set; }
        public string? LoteBobina { get; set; }
    }
}