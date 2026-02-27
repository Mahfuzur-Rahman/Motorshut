using MotorsHut.BLL.Abstractions.Services;
using MotorsHut.BLL.Contracts.Cars;
using MotorsHut.DAL.Abstractions;
using MotorsHut.DAL.Abstractions.Repositories;
using MotorsHut.DAL.Entities;

namespace MotorsHut.BLL.Services;

public sealed class CarService : ICarService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICarRepository _carRepository;

    public CarService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _carRepository = unitOfWork.Cars;
    }

    public async Task<IReadOnlyList<CarListItemDto>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var cars = await _carRepository.SearchAsync(search, cancellationToken);
        return cars.Select(MapListItem).ToArray();
    }

    public async Task<CarDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdWithImagesAsync(id, cancellationToken);
        return car is null ? null : MapDetails(car);
    }

    public async Task<CarOperationResultDto> CreateAsync(CreateCarRequestDto request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request.Make, request.Model, request.Year, request.Price, request.MileageKm);
        if (validation is not null)
        {
            return validation;
        }

        var car = new Car
        {
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Variant = NormalizeOptional(request.Variant),
            Year = request.Year,
            Price = request.Price,
            MileageKm = request.MileageKm,
            Color = NormalizeOptional(request.Color),
            Vin = NormalizeOptional(request.Vin),
            IsSold = request.IsSold,
            IsReturned = request.IsReturned,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _carRepository.AddAsync(car, cancellationToken);
        var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return affected > 0
            ? CarOperationResultDto.Success("Car added successfully.", car.Id)
            : CarOperationResultDto.Failure("Could not save car.");
    }

    public async Task<CarOperationResultDto> UpdateAsync(UpdateCarRequestDto request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request.Make, request.Model, request.Year, request.Price, request.MileageKm);
        if (validation is not null)
        {
            return validation;
        }

        var car = await _carRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (car is null)
        {
            return CarOperationResultDto.Failure("Car not found.");
        }

        car.Make = request.Make.Trim();
        car.Model = request.Model.Trim();
        car.Variant = NormalizeOptional(request.Variant);
        car.Year = request.Year;
        car.Price = request.Price;
        car.MileageKm = request.MileageKm;
        car.Color = NormalizeOptional(request.Color);
        car.Vin = NormalizeOptional(request.Vin);
        car.IsSold = request.IsSold;
        car.IsReturned = request.IsReturned;
        car.UpdatedAtUtc = DateTime.UtcNow;

        _carRepository.Update(car);
        var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return affected > 0
            ? CarOperationResultDto.Success("Car updated successfully.")
            : CarOperationResultDto.Failure("No changes were saved.");
    }

    public async Task<CarOperationResultDto> AddImagesAsync(Guid carId, IReadOnlyList<CreateCarImageRequestDto> images, CancellationToken cancellationToken = default)
    {
        if (images.Count == 0)
        {
            return CarOperationResultDto.Success("No images to upload.");
        }

        var car = await _carRepository.GetByIdForUpdateWithImagesAsync(carId, cancellationToken);
        if (car is null)
        {
            return CarOperationResultDto.Failure("Car not found.");
        }

        var hasPrimary = car.Images.Any(i => i.IsPrimary);
        var nextSortOrder = car.Images.Count == 0 ? 1 : car.Images.Max(i => i.SortOrder) + 1;

        foreach (var image in images)
        {
            if (string.IsNullOrWhiteSpace(image.ImageUrl))
            {
                continue;
            }

            var shouldBePrimary = image.IsPrimary || !hasPrimary;
            if (shouldBePrimary)
            {
                foreach (var existing in car.Images.Where(i => i.IsPrimary))
                {
                    existing.IsPrimary = false;
                }

                hasPrimary = true;
            }

            car.Images.Add(new CarImage
            {
                Id = Guid.NewGuid(),
                CarId = car.Id,
                ImageUrl = image.ImageUrl.Trim(),
                IsPrimary = shouldBePrimary,
                SortOrder = nextSortOrder++,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        car.UpdatedAtUtc = DateTime.UtcNow;
        _carRepository.Update(car);
        var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return affected > 0
            ? CarOperationResultDto.Success("Images uploaded successfully.", car.Id)
            : CarOperationResultDto.Failure("Could not save car images.");
    }

    public async Task<CarOperationResultDto> RemoveImageAsync(Guid carId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdForUpdateWithImagesAsync(carId, cancellationToken);
        if (car is null)
        {
            return CarOperationResultDto.Failure("Car not found.");
        }

        var image = car.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return CarOperationResultDto.Failure("Image not found.");
        }

        var wasPrimary = image.IsPrimary;
        car.Images.Remove(image);

        if (wasPrimary && car.Images.Count > 0)
        {
            var nextPrimary = car.Images
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.CreatedAtUtc)
                .First();
            nextPrimary.IsPrimary = true;
        }

        car.UpdatedAtUtc = DateTime.UtcNow;
        _carRepository.Update(car);
        var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return affected > 0
            ? CarOperationResultDto.Success("Image deleted successfully.", car.Id)
            : CarOperationResultDto.Failure("Could not delete image.");
    }

    public async Task<CarOperationResultDto> SetPrimaryImageAsync(Guid carId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdForUpdateWithImagesAsync(carId, cancellationToken);
        if (car is null)
        {
            return CarOperationResultDto.Failure("Car not found.");
        }

        var target = car.Images.FirstOrDefault(i => i.Id == imageId);
        if (target is null)
        {
            return CarOperationResultDto.Failure("Image not found.");
        }

        foreach (var image in car.Images)
        {
            image.IsPrimary = image.Id == imageId;
        }

        car.UpdatedAtUtc = DateTime.UtcNow;
        _carRepository.Update(car);
        var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return affected > 0
            ? CarOperationResultDto.Success("Primary image updated.", car.Id)
            : CarOperationResultDto.Failure("Could not update primary image.");
    }

    private static CarOperationResultDto? Validate(string make, string model, int year, decimal price, int mileageKm)
    {
        if (string.IsNullOrWhiteSpace(make))
        {
            return CarOperationResultDto.Failure("Validation failed.", "Make is required.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            return CarOperationResultDto.Failure("Validation failed.", "Model is required.");
        }

        var currentYear = DateTime.UtcNow.Year + 1;
        if (year < 1980 || year > currentYear)
        {
            return CarOperationResultDto.Failure("Validation failed.", $"Year must be between 1980 and {currentYear}.");
        }

        if (price <= 0)
        {
            return CarOperationResultDto.Failure("Validation failed.", "Price must be greater than 0.");
        }

        if (mileageKm < 0)
        {
            return CarOperationResultDto.Failure("Validation failed.", "Mileage cannot be negative.");
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static CarListItemDto MapListItem(Car car)
    {
        return new CarListItemDto
        {
            Id = car.Id,
            Make = car.Make,
            Model = car.Model,
            Variant = car.Variant,
            Year = car.Year,
            Price = car.Price,
            MileageKm = car.MileageKm,
            IsSold = car.IsSold,
            IsReturned = car.IsReturned
        };
    }

    private static CarDetailsDto MapDetails(Car car)
    {
        return new CarDetailsDto
        {
            Id = car.Id,
            Make = car.Make,
            Model = car.Model,
            Variant = car.Variant,
            Year = car.Year,
            Price = car.Price,
            MileageKm = car.MileageKm,
            Color = car.Color,
            Vin = car.Vin,
            IsSold = car.IsSold,
            IsReturned = car.IsReturned,
            Images = car.Images
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.CreatedAtUtc)
                .Select(MapImage)
                .ToArray()
        };
    }

    private static CarImageItemDto MapImage(CarImage image)
    {
        return new CarImageItemDto
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder
        };
    }
}
