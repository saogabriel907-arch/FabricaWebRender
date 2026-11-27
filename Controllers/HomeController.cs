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
            decimal consumoBobinaGramasPorFardo = 175m;
            string gramatura = "400g (175g/fardo)";
            bool vitaLeite350g = bobina.nome.Contains("Vita Leite", StringComparison.OrdinalIgnoreCase) &&
                                 Regex.IsMatch(bobina.nome, @"(?<!\d)350\s*g(?!\w)", RegexOptions.IgnoreCase);
            bool bobina800g = Regex.IsMatch(bobina.nome, @"(?<!\d)800\s*g(?!\w)", RegexOptions.IgnoreCase);
            if (vitaLeite350g)
            {
                consumoBobinaGramasPorFardo = 174m;
                gramatura = "Vita Leite 350g (174g/fardo)";
            }
            else if (bobina800g)
            {
                consumoBobinaGramasPorFardo = 129.6m;
                gramatura = "800g (129,6g/fardo)";
            }
            else if (Regex.IsMatch(bobina.nome, @"(?<!\d)1\s*kg(?!\w)", RegexOptions.IgnoreCase))
            {
                consumoBobinaGramasPorFardo = 120m;
                gramatura = "1kg (120g/fardo)";
            }
            else if (Regex.IsMatch(bobina.nome, @"(?<!\d)200\s*g(?!\w)", RegexOptions.IgnoreCase))
            {
                consumoBobinaGramasPorFardo = 260m;
                gramatura = "200g (260g/fardo)";
            }

            decimal estoqueKg = bobina.unidade_medida.Trim().Equals("g", StringComparison.OrdinalIgnoreCase)
                ? bobina.quantidade / 1000m
                : bobina.quantidade;
            decimal consumoBobinaKgPorFardo = consumoBobinaGramasPorFardo / 1000m;
            decimal fardosEstimados = Math.Max(estoqueKg, 0m) / consumoBobinaKgPorFardo;
            decimal leiteKgPorFardo = vitaLeite350g ? 10.5m : bobina800g ? 9.6m : 10m;

            return new BobinaProducaoViewModel
            {
                Nome = bobina.nome,
                GramaturaIdentificada = gramatura,
                ConsumoBobinaKgPorFardo = consumoBobinaKgPorFardo,
                EstoqueAtualKg = estoqueKg,
                FardosEstimados = fardosEstimados,
                LeiteKgPorFardo = leiteKgPorFardo,
                LeiteEstimadoKg = fardosEstimados * leiteKgPorFardo
            };
        }
    }
}