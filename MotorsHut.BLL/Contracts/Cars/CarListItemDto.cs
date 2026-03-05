namespace MotorsHut.BLL.Contracts.Cars;

public sealed class CarListItemDto
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
    public int InStock { get; init; }
    public int TotalSold { get; init; }
}
