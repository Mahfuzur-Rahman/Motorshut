using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MotorsHut.Models.Admin;

public sealed class CarFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Variant { get; set; }

    [Range(1980, 2100)]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [Range(typeof(decimal), "1", "1000000000")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int MileageKm { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(50)]
    public string? Vin { get; set; }

    public bool IsSold { get; set; }
    public bool IsReturned { get; set; }

    [Display(Name = "Car Images")]
    public List<IFormFile> NewImages { get; set; } = [];

    public IReadOnlyList<CarImageViewModel> ExistingImages { get; set; } = Array.Empty<CarImageViewModel>();
    public List<Guid> DeleteImageIds { get; set; } = [];
    public Guid? SelectedPrimaryImageId { get; set; }
}

public sealed class CarImageViewModel
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
