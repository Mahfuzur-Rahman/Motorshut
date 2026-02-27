using MotorsHut.DAL.Entities;

namespace MotorsHut.DAL.Abstractions.Repositories;

public interface ICarRepository : IGenericRepository<Car>
{
    Task<IReadOnlyList<Car>> SearchAsync(string? search, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdForUpdateWithImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CarImage?> GetImageByIdForUpdateAsync(Guid carId, Guid imageId, CancellationToken cancellationToken = default);
}
