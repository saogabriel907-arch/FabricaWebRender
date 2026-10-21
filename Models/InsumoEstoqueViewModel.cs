namespace FabricaWebRender.Models
{
    public class InsumoEstoqueViewModel
    {
        public long id_insumo { get; set; }
        public string nome { get; set; }
        public decimal quantidade { get; set; }
        public string unidade_medida { get; set; }
        public string marca { get; set; }
        public string lote_fabricante { get; set; }
        public DateTime atualizado_em { get; set; }
    }
}