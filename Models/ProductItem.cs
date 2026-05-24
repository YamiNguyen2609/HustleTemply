namespace HustleTemply.Models;

public record ProductItem(
    string Id,
    string Name,
    string Description,
    string Url,
    decimal? OldPricing,
    decimal? NewPricing,
    string? Thumb,
    bool BestSeller,
    string Level,
    string Category,
    List<string> Images);
