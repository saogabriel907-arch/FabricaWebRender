using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FabricaWebPortal.Models;

namespace FabricaWebPortal.Controllers
{
    public class HomeController : Controller
    {
        // (Mantém os teus métodos anteriores de Leite e Estoque aqui...)

        public async Task<IActionResult> Rastreabilidade()
        {
            var listaRastreio = new List<RastreabilidadeProducaoViewModel>();
            string supabaseUrl = "https://pzjyyjkilhhtuuuxtjeo.supabase.co/rest/v1/RastreabilidadeProducao?select=*";
            string supabaseKey = "sb_publishable_L7myx4lWC6orKJyJJ0dJ1w_V_6jEWhk"; // Substitui pela tua chave pública atual

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