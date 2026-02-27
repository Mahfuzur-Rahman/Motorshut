namespace MotorsHut.BLL.Contracts.Cars;

public sealed class UpdateCarRequestDto
{
    public Guid Id { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? Variant { get; init; }
    public int Year { get; init; }
    public decimal Price { get; init; }
    public int MileageKm { get; init; }
    public string? Color { get; init; }
    public string? Vin { get; init; }
    public bool IsSold { get; init; }
    public bool IsReturned { get; init; }
}
