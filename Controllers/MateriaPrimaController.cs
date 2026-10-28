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

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    string[] tabelas = { "HistoricoLeiteSoro", "historicoleitesoro", "historico_leite_soro" };
                    HttpResponseMessage response = null;

                    foreach (var tab in tabelas)
                    {
                        response = await client.GetAsync($"{supabaseUrl}{tab}?select=*");
                        if (response.IsSuccessStatusCode)
                        {
                            break;
                        }
                    }

                    if (response != null && response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);

                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                string nomeInsumo = "";
                                decimal peso = 0;
                                string lote = "-";
                                string sif = "-";
                                string nf = "-";
                                string fab = "-";
                                string val = "-";
                                string fornecedor = "Fábrica";
                                DateTime dataHora = DateTime.Now;

                                foreach (var prop in element.EnumerateObject())
                                {
                                    string pName = prop.Name.ToLower();
                                    string pVal = prop.Value.ValueKind != JsonValueKind.Null ? prop.Value.ToString() : "";

                                    if (pName == "insumo" || pName == "nome" || pName == "material")
                                        nomeInsumo = pVal;
                                    else if (pName == "peso" || pName == "quantidade" || pName == "estoque")
                                        decimal.TryParse(pVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out peso);
                                    else if (pName.Contains("lote") && !string.IsNullOrEmpty(pVal) && pVal != "EMPTY")
                                        lote = pVal;
                                    else if (pName.Contains("sif") && !string.IsNullOrEmpty(pVal) && pVal != "EMPTY")
                                        sif = pVal;
                                    else if ((pName.Contains("nf") || pName.Contains("nota")) && !string.IsNullOrEmpty(pVal) && pVal != "EMPTY")
                                        nf = pVal;
                                    else if ((pName.Contains("fabricacao") || pName.Contains("fab")) && !string.IsNullOrEmpty(pVal) && pVal != "EMPTY")
                                        fab = pVal;
                                    else if ((pName.Contains("validade") || pName.Contains("val")) && !string.IsNullOrEmpty(pVal) && pVal != "EMPTY")
                                        val = pVal;
                                    else if (pName.Contains("fornecedor") && !string.IsNullOrEmpty(pVal) && pVal != "EMPTY")
                                        fornecedor = pVal;
                                    else if ((pName.Contains("data") || pName.Contains("hora")) && prop.Value.TryGetDateTime(out var dt))
                                        dataHora = dt;
                                }

                                // Filtro simplificado: A despesa/entrada deve conter Leite ou Soro, com peso maior que zero e sem termos bloqueados desnecessários
if (!string.IsNullOrWhiteSpace(nomeInsumo) &&
    peso > 0 &&
    !nomeInsumo.Contains("Film", StringComparison.OrdinalIgnoreCase) &&
    !nomeInsumo.Contains("Fardo", StringComparison.OrdinalIgnoreCase) &&
    !nomeInsumo.Contains("Teste", StringComparison.OrdinalIgnoreCase))
{
    historicoList.Add(new HistoricoLeiteSoroViewModel
    {
        Id = element.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0,
        Insumo = nomeInsumo,
        Peso = peso,
        DataHora = dataHora,
        Fornecedor = fornecedor,
        Lote = lote,
        NotaFiscal = nf,
        Sif = sif,
        OutrosDetalhes = $"Fab: {fab} | Val: {val}"
    });
}
                            }
                        }
                    }
                    else
                    {
                        string erroBody = response != null ? await response.Content.ReadAsStringAsync() : "Sem resposta";
                        historicoList.Add(new HistoricoLeiteSoroViewModel
                        {
                            Id = 0,
                            Insumo = $"Erro HTTP: {response?.StatusCode}",
                            Peso = 0,
                            Fornecedor = "Supabase",
                            Lote = "-", NotaFiscal = "-", Sif = "-",
                            OutrosDetalhes = erroBody.Length > 50 ? erroBody.Substring(0, 50) : erroBody
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                historicoList.Add(new HistoricoLeiteSoroViewModel
                {
                    Id = 0,
                    Insumo = $"Exceção: {ex.Message}",
                    Peso = 0,
                    Fornecedor = "Sistema",
                    Lote = "-", NotaFiscal = "-", Sif = "-",
                    OutrosDetalhes = "-"
                });
            }

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
        public string OutrosDetalhes { get; set; } = string.Empty;
    }
}