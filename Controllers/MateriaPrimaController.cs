using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FabricaWebRender.Controllers
{
    public class MateriaPrimaController : Controller
    {
        // URL da tabela de histórico de entradas ou matéria-prima no Supabase
        private readonly string supabaseUrl = "https://pzjyyjkilhhtuuuxtjeo.supabase.co/rest/v1/HistoricoEntrada?select=*";
        private readonly string supabaseKey = "sb_publishable_L7myx4lWC6orKJyJJ0dJ1w_V_6jEWhk";

        public async Task<IActionResult> Index()
        {
            List<MateriaPrimaViewModel> listaMateriaPrima = new List<MateriaPrimaViewModel>();

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

                    listaMateriaPrima = JsonSerializer.Deserialize<List<MateriaPrimaViewModel>>(jsonResponse, options) ?? new List<MateriaPrimaViewModel>();
                }
            }

            return View(listaMateriaPrima);
        }
    }

    public class MateriaPrimaViewModel
    {
        public int Id { get; set; }
        public int InsumoId { get; set; }
        public decimal QuantidadeAdicionada { get; set; }
        public string DataEntrada { get; set; }
        public string Observacao { get; set; }
    }
}