namespace HustleTemply.Models;

public record SheetDataResponse(
    List<ConfigurationItem> Configurations,
    List<PaymentItem> Payments,
    List<ComplexityItem> Complexities,
    List<CategoryItem> Categories,
    List<SocialItem> Socials,
    List<BenefitItem> Benefits,
    List<MissionItem> Missions,
    List<ProductItem> Products);
