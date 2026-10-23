using System;

namespace fabricawebrender.Models
{
    public class RastreabilidadeProducaoViewModel
    {
        public long Id { get; set; }
        public string LeiteUtilizado { get; set; }
        public decimal Quantidade { get; set; }
        public DateTime DataProducao { get; set; }
        public string BobinaUtilizada { get; set; }
        public string FardoUtilizado { get; set; }
        public string Destino { get; set; }
    }
}