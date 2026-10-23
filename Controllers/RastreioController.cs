using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using fabricawebrender.Models;

namespace fabricawebrender.Controllers
{
    public class RastreioController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var listaRastreio = new List<RastreabilidadeProducaoViewModel>();
            // Certifica-te de que o nome da tabela aqui coincide exatamente com o usado no robô
            string supabaseUrl = "https://pzjyyjkilhhtuuuxtjeo.supabase.co/rest/v1/rastreabilidadeproducao?select=*";
            string supabaseKey = "sb_publishable_L7myx4lWC6orKJyJJ0dJ1w_V_6jEWhk";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

                HttpResponseMessage response = await client.GetAsync(supabaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    listaRastreio = JsonSerializer.Deserialize<List<RastreabilidadeProducaoViewModel>>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }

            return View(listaRastreio);
        }
    }
}