using MotorsHut.BLL.Contracts.Cars;

namespace MotorsHut.BLL.Abstractions.Services;

public interface ICarService
{
    Task<IReadOnlyList<CarListItemDto>> SearchAsync(string? search, CancellationToken cancellationToken = default);
    Task<CarDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CarOperationResultDto> CreateAsync(CreateCarRequestDto request, CancellationToken cancellationToken = default);
    Task<CarOperationResultDto> UpdateAsync(UpdateCarRequestDto request, CancellationToken cancellationToken = default);
    Task<CarOperationResultDto> AddImagesAsync(Guid carId, IReadOnlyList<CreateCarImageRequestDto> images, CancellationToken cancellationToken = default);
    Task<CarOperationResultDto> RemoveImageAsync(Guid carId, Guid imageId, CancellationToken cancellationToken = default);
    Task<CarOperationResultDto> SetPrimaryImageAsync(Guid carId, Guid imageId, CancellationToken cancellationToken = default);
}
