namespace MotorsHut.BLL.Contracts.Cars;

public sealed class UpdateCarWithImagesRequestDto
{
    public Guid Id { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? Variant { get; init; }
    public int Year { get; init; }
    public decimal Price { get; init; }
    public int MileageKm { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? ShortDescription { get; init; }
    public string? Color { get; init; }
    public string? Vin { get; init; }
    public int InStock { get; init; }
    public int TotalSold { get; init; }
    public IReadOnlyList<Guid> DeleteImageIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<UpdateCarImageSortOrderRequestDto> ExistingImageOrders { get; init; } = Array.Empty<UpdateCarImageSortOrderRequestDto>();
    public IReadOnlyList<CreateCarImageRequestDto> NewImages { get; init; } = Array.Empty<CreateCarImageRequestDto>();
    public Guid? SelectedPrimaryImageId { get; init; }
}
