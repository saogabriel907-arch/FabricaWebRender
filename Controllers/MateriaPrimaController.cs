using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FabricaWebPortal.Controllers
{
    public class MateriaPrimaController : Controller
    {
        private readonly string supabaseUrl = "https://pzjyyjkilhhtuuuxtjeo.supabase.co/rest/v1/";
        private readonly string supabaseKey = "sb_publishable_L7myx4lWC6orKJyJJ0dJ1w_V_6jEWhk";

        public async Task<IActionResult> Index()
        {
            var historicoList = new List<HistoricoLeiteSoroViewModel>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

                var responseHistorico = await client.GetAsync($"{supabaseUrl}HistoricoLeiteSoro?select=*&order=DataHora.desc");
                if (responseHistorico.IsSuccessStatusCode)
                {
                    var jsonHistorico = await responseHistorico.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(jsonHistorico))
                    {
                        foreach (var element in doc.RootElement.EnumerateArray())
                        {
                            historicoList.Add(new HistoricoLeiteSoroViewModel
                            {
                                Id = element.GetProperty("id").GetInt64(),
                                Insumo = element.TryGetProperty("Insumo", out var ins) ? ins.GetString() ?? "" : "",
                                Peso = element.TryGetProperty("Peso", out var p) && p.ValueKind != JsonValueKind.Null ? p.GetDecimal() : 0,
                                DataHora = element.TryGetProperty("DataHora", out var dt) && dt.ValueKind != JsonValueKind.Null ? dt.GetDateTime() : DateTime.MinValue,
                                Fornecedor = element.TryGetProperty("Fornecedor", out var f) ? f.GetString() ?? "" : "",
                                Lote = element.TryGetProperty("Lote", out var l) ? l.GetString() ?? "" : "",
                                NotaFiscal = element.TryGetProperty("NotaFiscal", out var nf) ? nf.GetString() ?? "" : "",
                                Sif = element.TryGetProperty("Sif", out var sif) ? sif.GetString() ?? "" : "",
                                DataFabricacao = element.TryGetProperty("DataFabricacao", out var df) ? df.GetString() ?? "" : "",
                                DataValidade = element.TryGetProperty("DataValidade", out var dv) ? dv.GetString() ?? "" : "",
                                OutrosDetalhes = element.TryGetProperty("OutrosDetalhes", out var od) ? od.GetString() ?? "" : ""
                            });
                        }
                    }
                }
            }

            // Filtra para garantir que apenas matérias-primas válidas (com nome preenchido e peso positivo) apareçam na listagem
            historicoList = historicoList
                .Where(x => !string.IsNullOrWhiteSpace(x.Insumo) 
                         && x.Peso > 0
                         && (x.Insumo.Contains("Leite", StringComparison.OrdinalIgnoreCase) 
                             || x.Insumo.Contains("Soro", StringComparison.OrdinalIgnoreCase) 
                             || x.Insumo.Contains("Desnatado", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return View(historicoList);
        }
    }

    public class HistoricoLeiteSoroViewModel
    {
        public long Id { get; set; }
        public string Insumo { get; set; } = string.Empty;
        public decimal Peso { get; set; }
        public DateTime DataHora { get; set; }
        public string Fornecedor { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
        public string NotaFiscal { get; set; } = string.Empty;
        public string Sif { get; set; } = string.Empty;
        public string DataFabricacao { get; set; } = string.Empty;
        public string DataValidade { get; set; } = string.Empty;
        public string OutrosDetalhes { get; set; } = string.Empty;
    }
}