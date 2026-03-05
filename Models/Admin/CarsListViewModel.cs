namespace MotorsHut.Models.Admin;

public sealed class CarsListViewModel
{
    public string? Search { get; set; }
    public IReadOnlyList<CarListItemViewModel> Cars { get; set; } = Array.Empty<CarListItemViewModel>();
}

public sealed class CarListItemViewModel
{
    public Guid Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int MileageKm { get; set; }
    public int InStock { get; set; }
    public int TotalSold { get; set; }
}
