using MotorsHut.DAL.Entities;

namespace MotorsHut.DAL.Abstractions.Repositories;

public interface ICarRepository : IGenericRepository<Car>
{
    Task<IReadOnlyList<Car>> SearchAsync(string? search, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdForUpdateWithImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CarImage?> GetImageByIdForUpdateAsync(Guid carId, Guid imageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarImage>> ListImagesByCarIdAsync(Guid carId, CancellationToken cancellationToken = default);
    Task<int> UpdateCarFieldsAsync(
        Guid carId,
        string make,
        string model,
        string? variant,
        int year,
        decimal price,
        int mileageKm,
        string? fuelType,
        string? transmission,
        string? shortDescription,
        string? color,
        string? vin,
        int inStock,
        int totalSold,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default);
    Task<int> DeleteImagesByCarIdAsync(Guid carId, CancellationToken cancellationToken = default);
    Task AddImagesRangeAsync(IReadOnlyList<CarImage> images, CancellationToken cancellationToken = default);
}
