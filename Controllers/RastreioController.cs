using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private const int PageSize = 20;
        private const int UnlinkedMovementPageSize = 50;
        private const int RelatedMovementPageSize = 1000;
        private const string MensagemFalha = "Não foi possível carregar a rastreabilidade de produção.";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

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
            string? status = null,
            bool mostrarSemOrdem = false)
        {
            page = Math.Max(page, 1);
            pesquisa = LimitarTexto(pesquisa, 100);
            status = status is "aberta" or "finalizada" ? status : string.Empty;

            DateOnly? inicio = ParseDate(dataInicial);
            DateOnly? fim = ParseDate(dataFinal);
            string? erroFiltro = ValidarDatas(dataInicial, dataFinal, inicio, fim);

            var model = new RastreabilidadeOrdemPageViewModel
            {
                Pesquisa = pesquisa ?? string.Empty,
                DataInicial = dataInicial ?? string.Empty,
                DataFinal = dataFinal ?? string.Empty,
                Status = status ?? string.Empty,
                MostrarSemOrdem = mostrarSemOrdem,
                ErroFiltro = erroFiltro
            };

            if (erroFiltro is not null)
            {
                return View(model);
            }

            try
            {
                using HttpClient client = _httpClientFactory.CreateClient("Supabase");
                Task<long> abertasTask = ObterContagemAsync(
                    client,
                    "rastreabilidade_ordens?select=id_ordem&status=eq.Em%20Aberto");
                Task<long> finalizadasTask = ObterContagemAsync(
                    client,
                    "rastreabilidade_ordens?select=id_ordem&status=eq.Finalizada");
                Task<long> semOrdemTask = ObterContagemAsync(
                    client,
                    "rastreabilidade_movimentacoes?select=id_movimentacao&id_ordem=is.null");

                await Task.WhenAll(abertasTask, finalizadasTask, semOrdemTask);
                model.TotalOrdensAbertas = await abertasTask;
                model.TotalOrdensFinalizadas = await finalizadasTask;
                model.TotalMovimentosSemOrdem = await semOrdemTask;

                if (mostrarSemOrdem)
                {
                    IActionResult? resultado = await CarregarMovimentosSemOrdemAsync(
                        client,
                        model,
                        page,
                        pesquisa,
                        inicio,
                        fim);
                    if (resultado is not null)
                    {
                        return resultado;
                    }
                }
                else
                {
                    IActionResult? resultado = await CarregarOrdensAsync(
                        client,
                        model,
                        page,
                        pesquisa,
                        inicio,
                        fim,
                        status);
                    if (resultado is not null)
                    {
                        return resultado;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha na consulta de rastreabilidade no Supabase.");
                return StatusCode(StatusCodes.Status502BadGateway, MensagemFalha);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Supabase retornou JSON inválido para a rastreabilidade.");
                return StatusCode(StatusCodes.Status502BadGateway, MensagemFalha);
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Supabase retornou cabeçalhos ou dados inválidos para a rastreabilidade.");
                return StatusCode(StatusCodes.Status502BadGateway, MensagemFalha);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "A consulta de rastreabilidade excedeu o tempo limite.");
                return StatusCode(StatusCodes.Status504GatewayTimeout, MensagemFalha);
            }

            return View(model);
        }

        private async Task<IActionResult?> CarregarOrdensAsync(
            HttpClient client,
            RastreabilidadeOrdemPageViewModel model,
            int page,
            string? pesquisa,
            DateOnly? inicio,
            DateOnly? fim,
            string? status)
        {
            var filtros = new List<string>
            {
                "select=id_ordem,lote,status,data_criacao,data_inicio,produtos,pesquisa_texto",
                "order=data_criacao.desc,id_ordem.desc",
                $"limit={PageSize}",
                $"offset={(long)(page - 1) * PageSize}"
            };

            AdicionarFiltrosData(filtros, "data_criacao", inicio, fim);

            if (status == "aberta")
            {
                filtros.Add("status=eq.Em%20Aberto");
            }
            else if (status == "finalizada")
            {
                filtros.Add("status=eq.Finalizada");
            }

            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                filtros.Add($"or={Uri.EscapeDataString(CriarFiltroPesquisaOrdem(pesquisa))}");
            }

            (RastreabilidadeOrdemViewModel[] ordens, long totalOrdens) =
                await ObterPaginaAsync<RastreabilidadeOrdemViewModel>(
                    client,
                    $"rastreabilidade_ordens?{string.Join("&", filtros)}");

            model.TotalOrdensFiltradas = totalOrdens;
            model.TotalPaginas = (int)Math.Ceiling(totalOrdens / (double)PageSize);

            if (page > Math.Max(model.TotalPaginas, 1))
            {
                return RedirecionarParaPaginaValida(model.TotalPaginas, model, false);
            }

            if (ordens.Length == 0)
            {
                model.Ordens = ordens;
                model.PaginaAtual = page;
                return null;
            }

            string ids = string.Join(",", ordens.Select(ordem => ordem.IdOrdem));
            IReadOnlyList<RastreabilidadeMovimentacaoViewModel> movimentos =
                await ObterMovimentosDasOrdensAsync(client, ids);
            var movimentosPorOrdem = movimentos
                .Where(movimento => movimento.IdOrdem.HasValue)
                .GroupBy(movimento => movimento.IdOrdem!.Value)
                .ToDictionary(grupo => grupo.Key, grupo => (IReadOnlyList<RastreabilidadeMovimentacaoViewModel>)grupo.ToList());

            foreach (RastreabilidadeOrdemViewModel ordem in ordens)
            {
                movimentosPorOrdem.TryGetValue(ordem.IdOrdem, out IReadOnlyList<RastreabilidadeMovimentacaoViewModel>? itens);
                AplicarResumoMovimentos(ordem, itens ?? Array.Empty<RastreabilidadeMovimentacaoViewModel>());
            }

            model.Ordens = ordens;
            model.PaginaAtual = page;
            return null;
        }

        private async Task<IActionResult?> CarregarMovimentosSemOrdemAsync(
            HttpClient client,
            RastreabilidadeOrdemPageViewModel model,
            int page,
            string? pesquisa,
            DateOnly? inicio,
            DateOnly? fim)
        {
            var filtros = new List<string>
            {
                "select=id_movimentacao,id_ordem,lote_ordem,id_insumo,id_produto,insumo,categoria,unidade_medida,quantidade_consumida,data_movimentacao,lote_bobina",
                "id_ordem=is.null",
                "order=data_movimentacao.desc,id_movimentacao.desc",
                $"limit={UnlinkedMovementPageSize}",
                $"offset={(long)(page - 1) * UnlinkedMovementPageSize}"
            };

            AdicionarFiltrosData(filtros, "data_movimentacao", inicio, fim);

            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                filtros.Add($"or={Uri.EscapeDataString(CriarFiltroPesquisaMovimento(pesquisa))}");
            }

            (RastreabilidadeMovimentacaoViewModel[] movimentos, long total) =
                await ObterPaginaAsync<RastreabilidadeMovimentacaoViewModel>(
                    client,
                    $"rastreabilidade_movimentacoes?{string.Join("&", filtros)}");

            model.TotalMovimentosSemOrdemFiltrados = total;
            model.TotalPaginas = (int)Math.Ceiling(total / (double)UnlinkedMovementPageSize);
            if (page > Math.Max(model.TotalPaginas, 1))
            {
                return RedirecionarParaPaginaValida(model.TotalPaginas, model, true);
            }

            model.MovimentosSemOrdem = movimentos;
            model.PaginaAtual = page;
            return null;
        }

        private async Task<long> ObterContagemAsync(HttpClient client, string path)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{path}&limit=1");
            request.Headers.TryAddWithoutValidation("Prefer", "count=exact");
            using HttpResponseMessage response = await client.SendAsync(request);
            ValidarResposta(response);
            return LerContagem(response);
        }

        private async Task<(T[] Registros, long Total)> ObterPaginaAsync<T>(HttpClient client, string path)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.TryAddWithoutValidation("Prefer", "count=exact");
            using HttpResponseMessage response = await client.SendAsync(request);
            ValidarResposta(response);

            string json = await response.Content.ReadAsStringAsync();
            T[]? registros = JsonSerializer.Deserialize<T[]>(json, JsonOptions);
            if (registros is null)
            {
                throw new JsonException("A resposta do Supabase não contém uma lista de registros.");
            }

            return (registros, LerContagem(response));
        }

        private async Task<IReadOnlyList<RastreabilidadeMovimentacaoViewModel>> ObterMovimentosDasOrdensAsync(
            HttpClient client,
            string idsOrdens)
        {
            var movimentos = new List<RastreabilidadeMovimentacaoViewModel>();
            int offset = 0;

            while (true)
            {
                string path =
                    "rastreabilidade_movimentacoes?select=id_movimentacao,id_ordem,lote_ordem,id_insumo,id_produto,insumo,categoria,unidade_medida,quantidade_consumida,data_movimentacao,lote_bobina" +
                    $"&id_ordem={Uri.EscapeDataString($"in.({idsOrdens})")}" +
                    "&order=data_movimentacao.desc,id_movimentacao.desc" +
                    $"&limit={RelatedMovementPageSize}&offset={offset}";

                using var response = await client.GetAsync(path);
                ValidarResposta(response);
                string json = await response.Content.ReadAsStringAsync();
                RastreabilidadeMovimentacaoViewModel[]? pagina =
                    JsonSerializer.Deserialize<RastreabilidadeMovimentacaoViewModel[]>(json, JsonOptions);

                if (pagina is null)
                {
                    throw new JsonException("A resposta do Supabase não contém uma lista de movimentos.");
                }

                movimentos.AddRange(pagina);
                if (pagina.Length < RelatedMovementPageSize)
                {
                    break;
                }

                offset += pagina.Length;
            }

            return movimentos;
        }

        private void ValidarResposta(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Supabase retornou HTTP {StatusCode} ao carregar a rastreabilidade.", (int)response.StatusCode);
                throw new HttpRequestException(
                    "A consulta do Supabase falhou.",
                    null,
                    response.StatusCode);
            }
        }

        private static long LerContagem(HttpResponseMessage response)
        {
            if (!response.Content.Headers.TryGetValues("Content-Range", out IEnumerable<string>? valores))
            {
                throw new InvalidDataException("O Supabase não retornou Content-Range.");
            }

            string? contentRange = valores.FirstOrDefault();
            int separador = contentRange?.LastIndexOf('/') ?? -1;
            string? total = separador < 0 ? null : contentRange!.Substring(separador + 1);
            if (!long.TryParse(total, NumberStyles.None, CultureInfo.InvariantCulture, out long contagem))
            {
                throw new InvalidDataException("O Supabase retornou uma contagem inválida.");
            }

            return contagem;
        }

        private static void AplicarResumoMovimentos(
            RastreabilidadeOrdemViewModel ordem,
            IReadOnlyList<RastreabilidadeMovimentacaoViewModel> movimentos)
        {
            ordem.Movimentos = movimentos;
            var bobinas = movimentos.Where(EhBobina).ToList();
            var fardos = movimentos.Where(EhFardo).ToList();
            var materiasPrimas = movimentos.Where(movimento => !EhBobina(movimento) && !EhFardo(movimento)).ToList();

            ordem.QuantidadeMovimentosBobina = bobinas.Count;
            ordem.QuantidadeMovimentosFardo = fardos.Count;
            ordem.QuantidadeMovimentosMateriaPrima = materiasPrimas.Count;
            ordem.TotalBobinas = bobinas.Sum(movimento => movimento.QuantidadeConsumida);
            ordem.TotalFardos = fardos.Sum(movimento => movimento.QuantidadeConsumida);
            ordem.TotalMateriaPrima = materiasPrimas.Sum(movimento => movimento.QuantidadeConsumida);
            var fardosPorProduto = fardos
                .Where(movimento => movimento.IdProduto.HasValue)
                .GroupBy(movimento => movimento.IdProduto!.Value)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());
            var idsProdutosPlanejados = ordem.Produtos
                .Select(produto => produto.IdInsumo)
                .ToHashSet();

            foreach (RastreabilidadeProdutoOrdemViewModel produto in ordem.Produtos)
            {
                if (!fardosPorProduto.TryGetValue(produto.IdInsumo, out List<RastreabilidadeMovimentacaoViewModel>? baixas))
                {
                    continue;
                }

                produto.QuantidadeFardosBaixados = baixas.Sum(movimento => movimento.QuantidadeConsumida);
                produto.DiferencaFardos = produto.QuantidadeFardosBaixados.Value - produto.QuantidadeFardos;
                produto.QuantidadeMovimentosFardo = baixas.Count;
            }

            var fardosSemProduto = fardos.Where(movimento => !movimento.IdProduto.HasValue).ToList();
            var fardosForaDoPlano = fardos
                .Where(movimento => movimento.IdProduto.HasValue &&
                    !idsProdutosPlanejados.Contains(movimento.IdProduto.Value))
                .ToList();
            ordem.TotalFardosSemProduto = fardosSemProduto.Sum(movimento => movimento.QuantidadeConsumida);
            ordem.QuantidadeMovimentosFardoSemProduto = fardosSemProduto.Count;
            ordem.TotalFardosForaDoPlano = fardosForaDoPlano.Sum(movimento => movimento.QuantidadeConsumida);
            ordem.QuantidadeMovimentosFardoForaDoPlano = fardosForaDoPlano.Count;
            ordem.LotesBobinaRegistrados = bobinas
                .Select(movimento => movimento.LoteBobina?.Trim())
                .Where(lote => !string.IsNullOrWhiteSpace(lote))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        private static bool EhBobina(RastreabilidadeMovimentacaoViewModel movimento)
        {
            return movimento.Categoria?.Contains("bobina", StringComparison.OrdinalIgnoreCase) == true ||
                movimento.Insumo?.StartsWith("Film", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool EhFardo(RastreabilidadeMovimentacaoViewModel movimento)
        {
            return movimento.Categoria?.Contains("fardo", StringComparison.OrdinalIgnoreCase) == true ||
                movimento.Insumo?.StartsWith("Fardo", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static void AdicionarFiltrosData(
            List<string> filtros,
            string coluna,
            DateOnly? inicio,
            DateOnly? fim)
        {
            if (inicio.HasValue)
            {
                filtros.Add($"{coluna}=gte.{inicio.Value:yyyy-MM-dd}T00%3A00%3A00");
            }

            if (fim.HasValue)
            {
                if (fim.Value == DateOnly.MaxValue)
                {
                    filtros.Add($"{coluna}=lte.9999-12-31T23%3A59%3A59.999999");
                }
                else
                {
                    DateOnly diaSeguinte = fim.Value.AddDays(1);
                    filtros.Add($"{coluna}=lt.{diaSeguinte:yyyy-MM-dd}T00%3A00%3A00");
                }
            }
        }

        private static string CriarFiltroPesquisaOrdem(string pesquisa)
        {
            var campos = new List<string>
            {
                $"lote.ilike.\"*{EscaparTermo(pesquisa)}*\"",
                $"pesquisa_texto.ilike.\"*{EscaparTermo(pesquisa)}*\""
            };

            if (long.TryParse(pesquisa, NumberStyles.None, CultureInfo.InvariantCulture, out long numero))
            {
                campos.Add($"id_ordem.eq.{numero}");
            }

            return $"({string.Join(",", campos)})";
        }

        private static string CriarFiltroPesquisaMovimento(string pesquisa)
        {
            var campos = new List<string>
            {
                $"insumo.ilike.\"*{EscaparTermo(pesquisa)}*\"",
                $"lote_bobina.ilike.\"*{EscaparTermo(pesquisa)}*\""
            };

            if (long.TryParse(pesquisa, NumberStyles.None, CultureInfo.InvariantCulture, out long numero))
            {
                campos.Add($"id_movimentacao.eq.{numero}");
                campos.Add($"id_insumo.eq.{numero}");
            }

            return $"({string.Join(",", campos)})";
        }

        private static string EscaparTermo(string pesquisa)
        {
            return pesquisa
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
        }

        private IActionResult RedirecionarParaPaginaValida(
            int totalPaginas,
            RastreabilidadeOrdemPageViewModel model,
            bool mostrarSemOrdem)
        {
            return RedirectToAction(nameof(Index), new
            {
                page = Math.Max(totalPaginas, 1),
                pesquisa = model.Pesquisa,
                dataInicial = model.DataInicial,
                dataFinal = model.DataFinal,
                status = mostrarSemOrdem ? null : model.Status,
                mostrarSemOrdem
            });
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

        private static string? ValidarDatas(
            string? dataInicial,
            string? dataFinal,
            DateOnly? inicio,
            DateOnly? fim)
        {
            if (!string.IsNullOrWhiteSpace(dataInicial) && inicio is null)
            {
                return "A data inicial é inválida.";
            }

            if (!string.IsNullOrWhiteSpace(dataFinal) && fim is null)
            {
                return "A data final é inválida.";
            }

            return inicio.HasValue && fim.HasValue && inicio > fim
                ? "A data inicial não pode ser posterior à data final."
                : null;
        }

        private static string? LimitarTexto(string? value, int maxLength)
        {
            string texto = value?.Trim() ?? string.Empty;
            return texto.Length <= maxLength ? texto : texto.Substring(0, maxLength);
        }
    }
}
