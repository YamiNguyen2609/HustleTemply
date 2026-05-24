using System.Globalization;
using HustleTemply.Models;

namespace HustleTemply.Parsers;

public static class GoogleSheetParser
{
    private static readonly HashSet<string> KnownSections = new(StringComparer.Ordinal)
    {
        "Configuration",
        "Payment",
        "Complexity",
        "Category",
        "Social",
        "Benefit",
        "Mission",
        "Product",
        "Product Image"
    };

    /// <summary>
    /// Parses raw sheet rows into a structured <see cref="SheetDataResponse"/>.
    /// </summary>
    /// <param name="rows">Rows from Google Sheets API (column A through J).</param>
    /// <returns>Parsed sheet data with product images joined to products.</returns>
    public static SheetDataResponse Parse(IList<IList<object>>? rows)
    {
        var configurations = new List<ConfigurationItem>();
        var payments = new List<PaymentItem>();
        var complexities = new List<ComplexityItem>();
        var categories = new List<CategoryItem>();
        var socials = new List<SocialItem>();
        var benefits = new List<BenefitItem>();
        var missions = new List<MissionItem>();
        var products = new List<ProductItem>();
        var productImages = new List<(string ProductId, string Url)>();

        if (rows is null || rows.Count == 0)
        {
            return new SheetDataResponse(
                configurations, payments, complexities, categories,
                socials, benefits, missions, products);
        }

        string? currentSection = null;
        Dictionary<string, int>? headerMap = null;

        foreach (var row in rows)
        {
            if (IsRowEmpty(row))
                continue;

            var firstCell = GetCell(row, 0)?.Trim() ?? string.Empty;

            if (TryParseSectionMarker(firstCell, " Start", out var startSection))
            {
                if (KnownSections.Contains(startSection))
                {
                    currentSection = startSection;
                    headerMap = null;
                }
                else
                {
                    currentSection = null;
                    headerMap = null;
                }

                continue;
            }

            if (TryParseSectionMarker(firstCell, " End", out var endSection))
            {
                if (currentSection == endSection)
                {
                    currentSection = null;
                    headerMap = null;
                }

                continue;
            }

            if (currentSection is null)
                continue;

            if (headerMap is null)
            {
                headerMap = BuildHeaderMap(row);
                continue;
            }

            switch (currentSection)
            {
                case "Configuration":
                    configurations.Add(MapConfiguration(row, headerMap));
                    break;
                case "Payment":
                    payments.Add(MapPayment(row, headerMap));
                    break;
                case "Complexity":
                    complexities.Add(MapComplexity(row, headerMap));
                    break;
                case "Category":
                    categories.Add(MapCategory(row, headerMap));
                    break;
                case "Social":
                    socials.Add(MapSocial(row, headerMap));
                    break;
                case "Benefit":
                    benefits.Add(MapBenefit(row, headerMap));
                    break;
                case "Mission":
                    missions.Add(MapMission(row, headerMap));
                    break;
                case "Product":
                    products.Add(MapProduct(row, headerMap));
                    break;
                case "Product Image":
                    var productId = GetString(row, headerMap, "Product Id") ?? string.Empty;
                    var url = GetString(row, headerMap, "Url") ?? string.Empty;
                    if (!string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(url))
                        productImages.Add((productId, url));
                    break;
            }
        }

        var imagesByProduct = productImages
            .GroupBy(x => x.ProductId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Url).ToList(), StringComparer.OrdinalIgnoreCase);

        var productsWithImages = products
            .Select(p => p with
            {
                Images = imagesByProduct.TryGetValue(p.Id, out var imgs)
                    ? imgs
                    : []
            })
            .ToList();

        return new SheetDataResponse(
            configurations,
            payments,
            complexities,
            categories,
            socials,
            benefits,
            missions,
            productsWithImages);
    }

    private static bool TryParseSectionMarker(string cell, string suffix, out string sectionName)
    {
        sectionName = string.Empty;
        if (!cell.EndsWith(suffix, StringComparison.Ordinal))
            return false;

        sectionName = cell[..^suffix.Length].Trim();
        return !string.IsNullOrEmpty(sectionName);
    }

    private static bool IsRowEmpty(IList<object> row)
    {
        for (var i = 0; i < row.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(row[i]?.ToString()))
                return false;
        }

        return true;
    }

    private static string? GetCell(IList<object> row, int index)
    {
        if (index < 0 || index >= row.Count)
            return null;

        return row[index]?.ToString();
    }

    private static Dictionary<string, int> BuildHeaderMap(IList<object> headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerRow.Count; i++)
        {
            var name = headerRow[i]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name))
                map[name] = i;
        }

        return map;
    }

    private static string? GetString(IList<object> row, Dictionary<string, int> headerMap, string columnName)
    {
        if (!headerMap.TryGetValue(columnName, out var index))
            return null;

        var value = GetCell(row, index);
        return ParseString(value);
    }

    private static string ParseStringRequired(IList<object> row, Dictionary<string, int> headerMap, string columnName)
        => GetString(row, headerMap, columnName) ?? string.Empty;

    private static string? ParseString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static bool ParseBool(IList<object> row, Dictionary<string, int> headerMap, string columnName)
    {
        var value = GetString(row, headerMap, columnName);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal? ParseDecimal(IList<object> row, Dictionary<string, int> headerMap, string columnName)
    {
        var value = GetString(row, headerMap, columnName);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static ConfigurationItem MapConfiguration(IList<object> row, Dictionary<string, int> headerMap) =>
        new(
            ParseStringRequired(row, headerMap, "Id"),
            ParseStringRequired(row, headerMap, "Name"),
            GetString(row, headerMap, "Value"));

    private static PaymentItem MapPayment(IList<object> row, Dictionary<string, int> headerMap) =>
        new(
            ParseStringRequired(row, headerMap, "Id"),
            ParseStringRequired(row, headerMap, "Account Name"),
            ParseStringRequired(row, headerMap, "Bank Name"),
            ParseStringRequired(row, headerMap, "Bank Account"),
            GetString(row, headerMap, "Bank QR"));

    private static ComplexityItem MapComplexity(IList<object> row, Dictionary<string, int> headerMap) =>
        new(
            ParseStringRequired(row, headerMap, "Id"),
            ParseStringRequired(row, headerMap, "Name"),
            ParseStringRequired(row, headerMap, "Display Name"));

    private static CategoryItem MapCategory(IList<object> row, Dictionary<string, int> headerMap) =>
        new(
            ParseStringRequired(row, headerMap, "Id"),
            ParseStringRequired(row, headerMap, "Name"),
            ParseStringRequired(row, headerMap, "Display Name"));

    private static SocialItem MapSocial(IList<object> row, Dictionary<string, int> headerMap) =>
        new(
            ParseStringRequired(row, headerMap, "Id"),
            ParseStringRequired(row, headerMap, "Name"),
            ParseStringRequired(row, headerMap, "Display Name"),
            ParseStringRequired(row, headerMap, "Url"),
            ParseBool(row, headerMap, "Active Bio"),
            ParseBool(row, headerMap, "Active Temply"));

    private static BenefitItem MapBenefit(IList<object> row, Dictionary<string, int> headerMap) =>
        new(
            ParseStringRequired(row, headerMap, "Id"),
            ParseStringRequired(row, headerMap, "Name"),
            ParseStringRequired(row, headerMap, "Description"),
            ParseStringRequired(row, headerMap, "Icon"));

    private static MissionItem MapMission(IList<object> row, Dictionary<string, int> headerMap) =>
        new(
            ParseStringRequired(row, headerMap, "Id"),
            ParseStringRequired(row, headerMap, "Name"),
            ParseStringRequired(row, headerMap, "Description"),
            ParseStringRequired(row, headerMap, "Icon"));

    private static ProductItem MapProduct(IList<object> row, Dictionary<string, int> headerMap) =>
        new(
            ParseStringRequired(row, headerMap, "Id"),
            ParseStringRequired(row, headerMap, "Name"),
            ParseStringRequired(row, headerMap, "Description"),
            ParseStringRequired(row, headerMap, "Url"),
            ParseDecimal(row, headerMap, "Old Pricing"),
            ParseDecimal(row, headerMap, "New Pricing"),
            GetString(row, headerMap, "Thumb"),
            ParseBool(row, headerMap, "Best Seller"),
            ParseStringRequired(row, headerMap, "Level"),
            ParseStringRequired(row, headerMap, "Category"),
            []);
}
