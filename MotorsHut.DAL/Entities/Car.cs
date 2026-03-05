namespace MotorsHut.DAL.Entities;

public sealed class Car
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int MileageKm { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? ShortDescription { get; set; }
    public string? Color { get; set; }
    public string? Vin { get; set; }
    public int InStock { get; set; }
    public int TotalSold { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<CarImage> Images { get; set; } = new List<CarImage>();
}
