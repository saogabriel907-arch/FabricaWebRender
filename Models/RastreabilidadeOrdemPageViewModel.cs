using System.Collections.Generic;

namespace fabricawebrender.Models
{
    public class RastreabilidadeOrdemPageViewModel
    {
        public IReadOnlyList<RastreabilidadeOrdemViewModel> Ordens { get; set; } =
            new List<RastreabilidadeOrdemViewModel>();
        public IReadOnlyList<RastreabilidadeMovimentacaoViewModel> MovimentosSemOrdem { get; set; } =
            new List<RastreabilidadeMovimentacaoViewModel>();
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
        public long TotalOrdensFiltradas { get; set; }
        public long TotalOrdensAbertas { get; set; }
        public long TotalOrdensFinalizadas { get; set; }
        public long TotalMovimentosSemOrdem { get; set; }
        public long TotalMovimentosSemOrdemFiltrados { get; set; }
        public string Pesquisa { get; set; } = string.Empty;
        public string DataInicial { get; set; } = string.Empty;
        public string DataFinal { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool MostrarSemOrdem { get; set; }
        public string? ErroFiltro { get; set; }
    }
}
