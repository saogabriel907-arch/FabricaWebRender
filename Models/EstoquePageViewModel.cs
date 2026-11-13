namespace FabricaWebRender.Models
{
    public class EstoquePageViewModel
    {
        public IReadOnlyList<InsumoEstoqueViewModel> Registros { get; set; } = Array.Empty<InsumoEstoqueViewModel>();
        public string CategoriaSelecionada { get; set; } = "todas";
        public string OrdenacaoSelecionada { get; set; } = "quantidade_desc";
    }
}
