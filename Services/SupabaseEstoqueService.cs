using System.Globalization;
using System.Text.Json;
using FabricaWebRender.Models;

namespace FabricaWebRender.Services;

public sealed class SupabaseEstoqueService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SupabaseEstoqueService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<InsumoEstoqueViewModel>> ObterEstoqueAsync(CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient("Supabase");
        using var response = await client.GetAsync(
            "tabela_estoque?select=id_insumo,nome,categoria,quantidade,unidade_medida,marca,lote_fabricante,atualizado_em",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("A resposta do estoque não contém uma lista de insumos.");
        }

        var estoque = new List<InsumoEstoqueViewModel>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            estoque.Add(new InsumoEstoqueViewModel
            {
                id_insumo = ReadLong(element, "id_insumo", "id"),
                nome = ReadString(element, "nome"),
                quantidade = ReadDecimal(element, "quantidade", "estoque", "peso"),
                unidade_medida = ReadString(element, "unidade_medida") is { Length: > 0 } unidade ? unidade : "KG",
                marca = ReadString(element, "marca"),
                lote_fabricante = ReadString(element, "lote_fabricante"),
                categoria = ReadString(element, "categoria", "setor", "tipo"),
                atualizado_em = ReadDateTimeOffset(element, "atualizado_em", "updated_at")
            });
        }

        return estoque;
    }

    private static string ReadString(JsonElement element, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "";
            }
        }

        return "";
    }

    private static decimal ReadDecimal(JsonElement element, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return 0m;
    }

    private static long ReadLong(JsonElement element, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out var number))
            {
                return number;
            }
        }

        return 0L;
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            {
                return date;
            }
        }

        return null;
    }
}
