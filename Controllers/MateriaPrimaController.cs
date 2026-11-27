using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FabricaWebPortal.Controllers;

public class MateriaPrimaController : Controller
{
    private const int TamanhoPaginaEstoque = 100;
    private static readonly TimeZoneInfo FusoBrasilia =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");

    private readonly IHttpClientFactory _httpClientFactory;

    public MateriaPrimaController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var materiasPrimas = new List<MateriaPrimaViewModel>();

        try
        {
            using var client = _httpClientFactory.CreateClient("Supabase");
            const string consultaBase =
                "tabela_estoque?select=id_insumo,nome,categoria,quantidade,unidade_medida,marca,lote_fabricante,atualizado_em&order=id_insumo.asc";
            int offset = 0;

            while (true)
            {
                using var response = await client.GetAsync(
                    $"{consultaBase}&limit={TamanhoPaginaEstoque}&offset={offset}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(StatusCodes.Status502BadGateway,
                        "Não foi possível carregar o estoque de matérias-primas.");
                }

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                int registrosRecebidos = 0;
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    registrosRecebidos++;
                    string categoria = GetString(element, "categoria");
                    string? categoriaMateriaPrima = NormalizarCategoriaMateriaPrima(categoria);
                    decimal quantidade = GetDecimal(element, "quantidade");

                    if (categoriaMateriaPrima is null || quantidade <= 0)
                    {
                        continue;
                    }

                    DateTimeOffset? atualizadoEm = GetDateTimeOffset(element, "atualizado_em");
                    materiasPrimas.Add(new MateriaPrimaViewModel
                    {
                        Id = GetInt64(element, "id_insumo"),
                        Nome = GetString(element, "nome"),
                        Categoria = categoriaMateriaPrima,
                        Quantidade = quantidade,
                        UnidadeMedida = GetString(element, "unidade_medida", "KG"),
                        Marca = GetString(element, "marca", "-"),
                        LoteFabricante = GetString(element, "lote_fabricante", "-"),
                        AtualizadoEm = atualizadoEm
                    });
                }

                if (registrosRecebidos == 0)
                {
                    break;
                }

                offset += registrosRecebidos;
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                "Não foi possível consultar o estoque: " + ex.Message);
        }

        return View(materiasPrimas
            .OrderBy(item => item.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList());
    }

    private static string? NormalizarCategoriaMateriaPrima(string categoria)
    {
        string normalizada = RemoverAcentos(categoria).Trim();
        return normalizada.Equals("materia prima", StringComparison.OrdinalIgnoreCase) ||
               normalizada.Equals("leite", StringComparison.OrdinalIgnoreCase) ||
               normalizada.Equals("soro", StringComparison.OrdinalIgnoreCase) ||
               normalizada.Equals("leite desnatado", StringComparison.OrdinalIgnoreCase) ||
               normalizada.Equals("permeado", StringComparison.OrdinalIgnoreCase) ||
               normalizada.Equals("descasque", StringComparison.OrdinalIgnoreCase) ||
               string.IsNullOrWhiteSpace(normalizada)
            ? "Matéria Prima"
            : null;
    }

    private static string GetString(JsonElement element, string property, string fallback = "")
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return fallback;
        }

        string result = value.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    private static long GetInt64(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return 0;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number)
            ? number
            : long.TryParse(value.ToString(), out number) ? number : 0;
    }

    private static decimal GetDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal number))
        {
            return number;
        }

        return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number)
            ? number
            : 0;
    }

    private static DateTimeOffset? GetDateTimeOffset(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) ||
            !value.TryGetDateTimeOffset(out DateTimeOffset instante))
        {
            return null;
        }

        return TimeZoneInfo.ConvertTime(instante, FusoBrasilia);
    }

    private static string RemoverAcentos(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed class MateriaPrimaViewModel
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public string UnidadeMedida { get; set; } = "KG";
    public string Marca { get; set; } = "-";
    public string LoteFabricante { get; set; } = "-";
    public DateTimeOffset? AtualizadoEm { get; set; }
}
