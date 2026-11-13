using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using FabricaWebRender.Models;

namespace FabricaWebPortal.Controllers
{
    public class EstoqueController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EstoqueController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(string? categoria, string? ordenacao)
        {
            var estoqueList = new List<InsumoEstoqueViewModel>();

            using (var client = _httpClientFactory.CreateClient("Supabase"))
            {
                var response = await client.GetAsync(
                    "tabela_estoque?select=id_insumo,nome,categoria,quantidade,unidade_medida,marca,lote_fabricante,atualizado_em");
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        StatusCodes.Status502BadGateway,
                        "Não foi possível carregar o estoque. Verifique se a categoria já foi sincronizada.");
                }

                var json = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(json))
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        decimal quantidade = 0;
                        if (element.TryGetProperty("quantidade", out var q1) && q1.ValueKind != JsonValueKind.Null) quantidade = q1.GetDecimal();
                        else if (element.TryGetProperty("estoque", out var q2) && q2.ValueKind != JsonValueKind.Null) quantidade = q2.GetDecimal();
                        else if (element.TryGetProperty("peso", out var q3) && q3.ValueKind != JsonValueKind.Null) quantidade = q3.GetDecimal();

                        DateTime atualizadoEm = default;
                        if (element.TryGetProperty("atualizado_em", out var atualizadoEmElement))
                        {
                            atualizadoEmElement.TryGetDateTime(out atualizadoEm);
                        }

                        estoqueList.Add(new InsumoEstoqueViewModel
                        {
                            id_insumo = element.TryGetProperty("id_insumo", out var id) ? id.GetInt64() : 0,
                            nome = element.TryGetProperty("nome", out var ins) ? ins.GetString() ?? "" : "",
                            categoria = element.TryGetProperty("categoria", out var cat) ? cat.GetString() ?? "" : "",
                            quantidade = quantidade,
                            unidade_medida = element.TryGetProperty("unidade_medida", out var um) ? um.GetString() ?? "KG" : "KG",
                            marca = element.TryGetProperty("marca", out var marca) ? marca.GetString() ?? "" : "",
                            lote_fabricante = element.TryGetProperty("lote_fabricante", out var lf) ? lf.GetString() ?? "" : "",
                            atualizado_em = atualizadoEm
                        });
                    }
                }
            }

            var registrosBobinaEFardo = estoqueList
                .Where(x => string.Equals(x.categoria?.Trim(), "Bobina", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(x.categoria?.Trim(), "Fardo", StringComparison.OrdinalIgnoreCase));

            string categoriaSelecionada = categoria?.Trim().ToLowerInvariant() switch
            {
                "bobina" => "bobina",
                "fardo" => "fardo",
                _ => "todas"
            };
            string ordenacaoSelecionada = ordenacao?.Trim().ToLowerInvariant() == "quantidade_asc"
                ? "quantidade_asc"
                : "quantidade_desc";

            if (categoriaSelecionada != "todas")
            {
                string categoriaFiltro = categoriaSelecionada;
                registrosBobinaEFardo = registrosBobinaEFardo.Where(
                    x => string.Equals(x.categoria?.Trim(), categoriaFiltro, StringComparison.OrdinalIgnoreCase));
            }

            var registrosOrdenados = ordenacaoSelecionada == "quantidade_asc"
                ? registrosBobinaEFardo.OrderBy(x => x.quantidade).ThenBy(x => x.nome, StringComparer.CurrentCultureIgnoreCase)
                : registrosBobinaEFardo.OrderByDescending(x => x.quantidade).ThenBy(x => x.nome, StringComparer.CurrentCultureIgnoreCase);

            return View(new EstoquePageViewModel
            {
                Registros = registrosOrdenados.ToList(),
                CategoriaSelecionada = categoriaSelecionada,
                OrdenacaoSelecionada = ordenacaoSelecionada
            });
        }
    }
}