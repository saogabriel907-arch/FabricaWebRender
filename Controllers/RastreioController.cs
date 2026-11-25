using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using fabricawebrender.Models;

namespace fabricawebrender.Controllers
{
    public class RastreioController : Controller
    {
        private const int PageSize = 50;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RastreioController> _logger;

        public RastreioController(
            IHttpClientFactory httpClientFactory,
            ILogger<RastreioController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            int page = 1,
            string? pesquisa = null,
            string? dataInicial = null,
            string? dataFinal = null,
            string? vinculo = null)
        {
            page = Math.Max(page, 1);
            pesquisa = LimitarTexto(pesquisa, 100);
            vinculo = vinculo is "com-op" or "sem-op" ? vinculo : string.Empty;

            DateOnly? inicio = ParseDate(dataInicial);
            DateOnly? fim = ParseDate(dataFinal);
            string? erroFiltro = null;
            if (!string.IsNullOrWhiteSpace(dataInicial) && inicio is null)
            {
                erroFiltro = "A data inicial é inválida.";
            }
            else if (!string.IsNullOrWhiteSpace(dataFinal) && fim is null)
            {
                erroFiltro = "A data final é inválida.";
            }
            else if (inicio.HasValue && fim.HasValue && inicio > fim)
            {
                erroFiltro = "A data inicial não pode ser posterior à data final.";
            }

            long offset = (long)(page - 1) * PageSize;
            const string select = "id_movimentacao,id_ordem,lote_ordem,id_insumo,insumo,categoria,unidade_medida,quantidade_consumida,data_movimentacao,lote_bobina";
            var filtros = new List<string>
            {
                $"select={Uri.EscapeDataString(select)}",
                $"order={Uri.EscapeDataString("data_movimentacao.desc,id_movimentacao.desc")}",
                $"limit={PageSize}",
                $"offset={offset}"
            };

            if (erroFiltro is null)
            {
                if (inicio.HasValue)
                {
                    filtros.Add($"data_movimentacao=gte.{inicio.Value:yyyy-MM-dd}T00%3A00%3A00");
                }

                if (fim.HasValue)
                {
                    if (fim.Value == DateOnly.MaxValue)
                    {
                        filtros.Add("data_movimentacao=lte.9999-12-31T23%3A59%3A59.999999");
                    }
                    else
                    {
                        DateOnly diaSeguinte = fim.Value.AddDays(1);
                        filtros.Add($"data_movimentacao=lt.{diaSeguinte:yyyy-MM-dd}T00%3A00%3A00");
                    }
                }

                if (vinculo == "com-op")
                {
                    filtros.Add("id_ordem=not.is.null");
                }
                else if (vinculo == "sem-op")
                {
                    filtros.Add("id_ordem=is.null");
                }

                if (!string.IsNullOrWhiteSpace(pesquisa))
                {
                    filtros.Add($"or={Uri.EscapeDataString(CriarFiltroPesquisa(pesquisa))}");
                }
            }

            string path = $"rastreabilidade_movimentacoes?{string.Join("&", filtros)}";

            var model = new RastreabilidadeMovimentacoesPageViewModel
            {
                Pesquisa = pesquisa ?? string.Empty,
                DataInicial = dataInicial ?? string.Empty,
                DataFinal = dataFinal ?? string.Empty,
                Vinculo = vinculo ?? string.Empty,
                ErroFiltro = erroFiltro
            };

            if (erroFiltro is null)
            {
                using (HttpClient client = _httpClientFactory.CreateClient("Supabase"))
                using (var request = new HttpRequestMessage(HttpMethod.Get, path))
                {
                    request.Headers.TryAddWithoutValidation("Prefer", "count=exact");

                    using HttpResponseMessage response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Supabase retornou {StatusCode} ao carregar a rastreabilidade.", (int)response.StatusCode);
                        return StatusCode(StatusCodes.Status502BadGateway, "Não foi possível carregar a rastreabilidade de produção.");
                    }

                    if (!response.Content.Headers.TryGetValues("Content-Range", out var contentRangeValues))
                    {
                        _logger.LogError("Supabase não retornou a contagem total da rastreabilidade.");
                        return StatusCode(StatusCodes.Status502BadGateway, "Não foi possível carregar a rastreabilidade de produção.");
                    }

                    string? contentRange = System.Linq.Enumerable.FirstOrDefault(contentRangeValues);
                    int separatorIndex = contentRange?.LastIndexOf('/') ?? -1;
                    string? totalText = separatorIndex < 0 ? null : contentRange!.Substring(separatorIndex + 1);
                    if (!long.TryParse(
                            totalText,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out long totalRegistros))
                    {
                        _logger.LogError("Supabase retornou um cabeçalho Content-Range inválido para a rastreabilidade.");
                        return StatusCode(StatusCodes.Status502BadGateway, "Não foi possível carregar a rastreabilidade de produção.");
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    RastreabilidadeMovimentacaoViewModel[]? registros;
                    try
                    {
                        registros = JsonSerializer.Deserialize<RastreabilidadeMovimentacaoViewModel[]>(
                            jsonResponse,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                            });
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Supabase retornou JSON inválido para a rastreabilidade.");
                        return StatusCode(StatusCodes.Status502BadGateway, "Não foi possível carregar a rastreabilidade de produção.");
                    }

                    if (registros is null)
                    {
                        _logger.LogError("Supabase retornou uma resposta vazia ou inválida para a rastreabilidade.");
                        return StatusCode(StatusCodes.Status502BadGateway, "Não foi possível carregar a rastreabilidade de produção.");
                    }

                    int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)PageSize);
                    if (page > Math.Max(totalPaginas, 1))
                    {
                        return RedirectToAction(nameof(Index), new
                        {
                            page = Math.Max(totalPaginas, 1),
                            pesquisa,
                            dataInicial,
                            dataFinal,
                            vinculo
                        });
                    }

                    model.Registros = registros;
                    model.PaginaAtual = page;
                    model.TotalPaginas = totalPaginas;
                    model.TotalRegistros = totalRegistros;
                    model.MovimentosComOrdemNaPagina = registros.Count(registro => registro.IdOrdem.HasValue);
                    model.MovimentosSemOrdemNaPagina = registros.Length - model.MovimentosComOrdemNaPagina;
                }
            }

            return View(model);
        }

        private static DateOnly? ParseDate(string? value)
        {
            return DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date)
                ? date
                : null;
        }

        private static string? LimitarTexto(string? value, int maxLength)
        {
            string texto = value?.Trim() ?? string.Empty;
            return texto.Length <= maxLength ? texto : texto.Substring(0, maxLength);
        }

        private static string CriarFiltroPesquisa(string pesquisa)
        {
            string texto = pesquisa
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
            string filtro = $"(insumo.ilike.\"*{texto}*\",lote_ordem.ilike.\"*{texto}*\",lote_bobina.ilike.\"*{texto}*\"";

            if (long.TryParse(pesquisa, NumberStyles.None, CultureInfo.InvariantCulture, out long numero))
            {
                filtro += $",id_movimentacao.eq.{numero},id_ordem.eq.{numero},id_insumo.eq.{numero}";
            }

            return filtro + ")";
        }
    }
}