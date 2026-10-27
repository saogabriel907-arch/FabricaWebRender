using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using FabricaWebRender.Models;

namespace FabricaWebRender.Controllers
{
    public class EstoqueController : Controller
    {
        private readonly string supabaseUrl = "https://pzjyyjkilhhtuuuxtjeo.supabase.co/rest/v1/tabela_estoque?select=*";
        private readonly string supabaseKey = "sb_publishable_L7myx4lWC6orKJyJJ0dJ1w_V_6jEWhk";

        public async Task<IActionResult> Index()
        {
            List<InsumoEstoqueViewModel> listaEstoque = new List<InsumoEstoqueViewModel>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

                HttpResponseMessage response = await client.GetAsync(supabaseUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    listaEstoque = JsonSerializer.Deserialize<List<InsumoEstoqueViewModel>>(jsonResponse, options) ?? new List<InsumoEstoqueViewModel>();

                    // Filtra para remover produtos, fardos, leite em pó e oculta itens com quantidade zerada ou menor/igual a zero
                    listaEstoque = listaEstoque
                        .Where(x => x.quantidade > 0
                                 && !string.IsNullOrEmpty(x.nome)
                                 && !x.nome.Contains("LEITE EM PÓ", StringComparison.OrdinalIgnoreCase)
                                 && !x.nome.Contains("COMPOSTO LÁCTEO", StringComparison.OrdinalIgnoreCase)
                                 && !x.nome.Contains("Fardo", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            return View(listaEstoque);
        }
    }
}