using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using FabricaWebRender.Models;

namespace FabricaWebPortal.Controllers
{
    public class EstoqueController : Controller
    {
        private readonly string supabaseUrl = "https://pzjyyjkilhhtuuuxtjeo.supabase.co/rest/v1/";
        private readonly string supabaseKey = "sb_publishable_L7myx4lWC6orKJyJJ0dJ1w_V_6jEWhk";

        public async Task<IActionResult> Index()
        {
            var estoqueList = new List<InsumoEstoqueViewModel>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

                var response = await client.GetAsync($"{supabaseUrl}tabela_estoque?select=*");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(json))
                    {
                        foreach (var element in doc.RootElement.EnumerateArray())
                        {
                            decimal quantidade = 0;
                            if (element.TryGetProperty("quantidade", out var q1) && q1.ValueKind != JsonValueKind.Null) quantidade = q1.GetDecimal();
                            else if (element.TryGetProperty("estoque", out var q2) && q2.ValueKind != JsonValueKind.Null) quantidade = q2.GetDecimal();
                            else if (element.TryGetProperty("peso", out var q3) && q3.ValueKind != JsonValueKind.Null) quantidade = q3.GetDecimal();

                            estoqueList.Add(new InsumoEstoqueViewModel
                            {
                                nome = element.TryGetProperty("nome", out var ins) ? ins.GetString() ?? "" : "",
                                quantidade = quantidade,
                                unidade_medida = element.TryGetProperty("unidade_medida", out var um) ? um.GetString() ?? "KG" : "KG",
                                lote_fabricante = element.TryGetProperty("lote_fabricante", out var lf) ? lf.GetString() ?? "" : ""
                            });
                        }
                    }
                }
            }

            // Exclui apenas a matéria-prima pura da home, mantendo o leite em pó e o restante estoque geral
            estoqueList = estoqueList
                .Where(x => !string.IsNullOrWhiteSpace(x.nome)
                         && !((x.nome.Contains("Leite", StringComparison.OrdinalIgnoreCase) || x.nome.Contains("Soro", StringComparison.OrdinalIgnoreCase))
                              && !x.nome.Contains("Pó", StringComparison.OrdinalIgnoreCase)
                              && !x.nome.Contains("Film", StringComparison.OrdinalIgnoreCase)
                              && !x.nome.Contains("Fardo", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return View(estoqueList);
        }
    }
}