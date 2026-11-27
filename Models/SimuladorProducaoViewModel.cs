namespace FabricaWebRender.Models;

public sealed class SimuladorProducaoViewModel
{
    public List<BobinaProducaoViewModel> Bobinas { get; set; } = new();
    public decimal EstoqueTotalKg { get; set; }
    public decimal TotalFardosEstimados { get; set; }
    public decimal TotalLeiteEstimadoKg { get; set; }
}

public sealed class BobinaProducaoViewModel
{
    public string Nome { get; set; } = "";
    public string GramaturaIdentificada { get; set; } = "";
    public decimal ConsumoBobinaKgPorFardo { get; set; }
    public decimal EstoqueAtualKg { get; set; }
    public decimal FardosEstimados { get; set; }
    public decimal LeiteKgPorFardo { get; set; }
    public decimal LeiteEstimadoKg { get; set; }
}
