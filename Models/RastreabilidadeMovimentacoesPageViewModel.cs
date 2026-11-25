using System.Collections.Generic;

namespace fabricawebrender.Models
{
    public class RastreabilidadeMovimentacoesPageViewModel
    {
        public IReadOnlyList<RastreabilidadeMovimentacaoViewModel> Registros { get; set; } =
            new List<RastreabilidadeMovimentacaoViewModel>();
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
        public long TotalRegistros { get; set; }
        public string Pesquisa { get; set; } = string.Empty;
        public string DataInicial { get; set; } = string.Empty;
        public string DataFinal { get; set; } = string.Empty;
        public string Vinculo { get; set; } = string.Empty;
        public string? ErroFiltro { get; set; }

        public int MovimentosComOrdemNaPagina { get; set; }
        public int MovimentosSemOrdemNaPagina { get; set; }
    }
}
