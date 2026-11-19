using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FabricaWebPortal.Controllers;

public class MateriaPrimaController : Controller
{
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
            using var response = await client.GetAsync(
                "tabela_estoque?select=id_insumo,nome,categoria,quantidade,unidade_medida,marca,lote_fabricante,atualizado_em");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    "Não foi possível carregar o estoque de matérias-primas.");
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            foreach (var element in document.RootElement.EnumerateArray())
            {
                string categoria = GetString(element, "categoria");
                decimal quantidade = GetDecimal(element, "quantidade");

                if (!EhMateriaPrima(categoria) || quantidade <= 0)
                {
                    continue;
                }

                DateTime atualizadoEm = GetDateTime(element, "atualizado_em");
                materiasPrimas.Add(new MateriaPrimaViewModel
                {
                    Id = GetInt64(element, "id_insumo"),
                    Nome = GetString(element, "nome"),
                    Categoria = categoria,
                    Quantidade = quantidade,
                    UnidadeMedida = GetString(element, "unidade_medida", "KG"),
                    Marca = GetString(element, "marca", "-"),
                    LoteFabricante = GetString(element, "lote_fabricante", "-"),
                    AtualizadoEm = atualizadoEm
                });
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

    private static bool EhMateriaPrima(string categoria)
    {
        string normalizada = RemoverAcentos(categoria).Trim();
        return normalizada.Equals("materia prima", StringComparison.OrdinalIgnoreCase);
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

    private static DateTime GetDateTime(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) ||
            !value.TryGetDateTime(out DateTime dateTime))
        {
            return DateTime.MinValue;
        }

        return dateTime;
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
    public DateTime AtualizadoEm { get; set; }
}
