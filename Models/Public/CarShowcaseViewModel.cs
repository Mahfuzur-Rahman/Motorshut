namespace MotorsHut.Models.Public;

public sealed class CarShowcaseViewModel
{
    public IReadOnlyList<CarShowcaseItemViewModel> Cars { get; init; } = Array.Empty<CarShowcaseItemViewModel>();
}

public sealed class CarShowcaseItemViewModel
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Variant { get; init; }
    public int Year { get; init; }
    public decimal Price { get; init; }
    public int MileageKm { get; init; }
    public string? Color { get; init; }
    public string? Vin { get; init; }
    public bool IsSold { get; init; }
    public bool IsReturned { get; init; }
    public string CardImageUrl { get; init; } = string.Empty;
    public IReadOnlyList<string> ImageUrls { get; init; } = Array.Empty<string>();
}
