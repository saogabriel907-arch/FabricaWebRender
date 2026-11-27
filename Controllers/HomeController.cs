using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using FabricaWebRender.Models;
using FabricaWebRender.Services;

namespace FabricaWebRender.Controllers
{
    public class HomeController : Controller
    {
        private readonly SupabaseEstoqueService _estoqueService;

        public HomeController(SupabaseEstoqueService estoqueService)
        {
            _estoqueService = estoqueService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Estoque");
        }

        public async Task<IActionResult> Privacy(CancellationToken cancellationToken)
        {
            var estoque = await _estoqueService.ObterEstoqueAsync(cancellationToken);
            var bobinas = estoque
                .Where(item =>
                    item.nome.Contains("film", StringComparison.OrdinalIgnoreCase) ||
                    item.nome.Contains("bobina", StringComparison.OrdinalIgnoreCase) ||
                    item.categoria.Contains("film", StringComparison.OrdinalIgnoreCase) ||
                    item.categoria.Contains("bobina", StringComparison.OrdinalIgnoreCase))
                .Select(CalcularProducao)
                .OrderByDescending(item => item.EstoqueAtualKg)
                .ToList();

            var model = new SimuladorProducaoViewModel
            {
                Bobinas = bobinas,
                EstoqueTotalKg = bobinas.Sum(item => item.EstoqueAtualKg),
                TotalFardosEstimados = bobinas.Sum(item => item.FardosEstimados),
                TotalLeiteEstimadoKg = bobinas.Sum(item => item.LeiteEstimadoKg)
            };

            return View(model);
        }

        private static BobinaProducaoViewModel CalcularProducao(InsumoEstoqueViewModel bobina)
        {
            decimal pesoPacoteVazioGramas = 0.175m;
            int pacotesPorFardo = 25;
            string gramatura = "400g";
            bool vitaLeite350g = bobina.nome.Contains("Vita Leite", StringComparison.OrdinalIgnoreCase) &&
                                 Regex.IsMatch(bobina.nome, @"(?<!\d)350\s*g(?!\w)", RegexOptions.IgnoreCase);
            bool bobina800g = Regex.IsMatch(bobina.nome, @"(?<!\d)800\s*g(?!\w)", RegexOptions.IgnoreCase);
            if (vitaLeite350g)
            {
                pesoPacoteVazioGramas = 0.174m;
                pacotesPorFardo = 30;
                gramatura = "Vita Leite 350g";
            }
            else if (bobina800g)
            {
                pesoPacoteVazioGramas = 0.130m;
                pacotesPorFardo = 12;
                gramatura = "800g";
            }
            else if (Regex.IsMatch(bobina.nome, @"(?<!\d)1\s*kg(?!\w)", RegexOptions.IgnoreCase))
            {
                pesoPacoteVazioGramas = 0.120m;
                pacotesPorFardo = 10;
                gramatura = "1kg";
            }
            else if (Regex.IsMatch(bobina.nome, @"(?<!\d)200\s*g(?!\w)", RegexOptions.IgnoreCase))
            {
                pesoPacoteVazioGramas = 0.260m;
                pacotesPorFardo = 50;
                gramatura = "200g";
            }

            decimal estoqueKg = bobina.unidade_medida.Trim().Equals("g", StringComparison.OrdinalIgnoreCase)
                ? bobina.quantidade / 1000m
                : bobina.quantidade;
            decimal consumoBobinaGramas = pesoPacoteVazioGramas * pacotesPorFardo;
            decimal consumoBobinaKgPorFardo = consumoBobinaGramas / 1000m;
            decimal fardosEstimados = Math.Max(estoqueKg, 0m) / consumoBobinaKgPorFardo;
            decimal leiteKgPorFardo = vitaLeite350g ? 10.5m : bobina800g ? 9.6m : 10m;

            return new BobinaProducaoViewModel
            {
                Nome = bobina.nome,
                GramaturaIdentificada = gramatura,
                PacotesPorFardo = pacotesPorFardo,
                PesoPacoteVazioGramas = pesoPacoteVazioGramas,
                ConsumoBobinaGramasPorFardo = consumoBobinaGramas,
                ConsumoBobinaKgPorFardo = consumoBobinaKgPorFardo,
                EstoqueAtualKg = estoqueKg,
                FardosEstimados = fardosEstimados,
                LeiteKgPorFardo = leiteKgPorFardo,
                LeiteEstimadoKg = fardosEstimados * leiteKgPorFardo
            };
        }
    }
}