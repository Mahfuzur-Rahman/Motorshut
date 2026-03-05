using MotorsHut.BLL.Abstractions.Services;
using MotorsHut.BLL.Contracts.Cars;
using Microsoft.EntityFrameworkCore;
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
        var validation = Validate(request.Make, request.Model, request.Year, request.Price, request.MileageKm, request.InStock, request.TotalSold);
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
            FuelType = NormalizeOptional(request.FuelType),
            Transmission = NormalizeOptional(request.Transmission),
            ShortDescription = NormalizeOptional(request.ShortDescription),
            Color = NormalizeOptional(request.Color),
            Vin = NormalizeOptional(request.Vin),
            InStock = request.InStock,
            TotalSold = request.TotalSold,
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
        var validation = Validate(request.Make, request.Model, request.Year, request.Price, request.MileageKm, request.InStock, request.TotalSold);
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
        car.FuelType = NormalizeOptional(request.FuelType);
        car.Transmission = NormalizeOptional(request.Transmission);
        car.ShortDescription = NormalizeOptional(request.ShortDescription);
        car.Color = NormalizeOptional(request.Color);
        car.Vin = NormalizeOptional(request.Vin);
        car.InStock = request.InStock;
        car.TotalSold = request.TotalSold;
        car.UpdatedAtUtc = DateTime.UtcNow;

        var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return affected > 0
            ? CarOperationResultDto.Success("Car updated successfully.")
            : CarOperationResultDto.Failure("No changes were saved.");
    }

    public async Task<CarEditOperationResultDto> UpdateWithImagesAsync(UpdateCarWithImagesRequestDto request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request.Make, request.Model, request.Year, request.Price, request.MileageKm, request.InStock, request.TotalSold);
        if (validation is not null)
        {
            return CarEditOperationResultDto.Failure(validation.Message, validation.Errors.ToArray());
        }

        var existingImages = await _carRepository.ListImagesByCarIdAsync(request.Id, cancellationToken);
        if (existingImages.Count == 0)
        {
            var carExists = await _carRepository.GetByIdAsync(request.Id, cancellationToken);
            if (carExists is null)
            {
                return CarEditOperationResultDto.Failure("Car not found.");
            }
        }

        var now = DateTime.UtcNow;
        var updatedRows = await _carRepository.UpdateCarFieldsAsync(
            request.Id,
            request.Make.Trim(),
            request.Model.Trim(),
            NormalizeOptional(request.Variant),
            request.Year,
            request.Price,
            request.MileageKm,
            NormalizeOptional(request.FuelType),
            NormalizeOptional(request.Transmission),
            NormalizeOptional(request.ShortDescription),
            NormalizeOptional(request.Color),
            NormalizeOptional(request.Vin),
            request.InStock,
            request.TotalSold,
            now,
            cancellationToken);

        if (updatedRows == 0)
        {
            return CarEditOperationResultDto.Failure("Car not found.");
        }

        var deleteIds = request.DeleteImageIds.Distinct().ToHashSet();
        var deletedImageUrls = existingImages
            .Where(i => deleteIds.Contains(i.Id))
            .Select(i => i.ImageUrl)
            .ToArray();

        var remaining = existingImages
            .Where(i => !deleteIds.Contains(i.Id))
            .ToArray();

        var remainingById = remaining.ToDictionary(i => i.Id);
        var requestedOrders = request.ExistingImageOrders
            .Where(x => x.SortOrder > 0 && remainingById.ContainsKey(x.ImageId))
            .GroupBy(x => x.ImageId)
            .Select(g => g.First())
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ImageId)
            .ToArray();

        var requestedIds = requestedOrders.Select(x => x.ImageId).ToHashSet();
        var orderedExisting = requestedOrders
            .Select(x => remainingById[x.ImageId])
            .Concat(remaining
                .Where(x => !requestedIds.Contains(x.Id))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAtUtc))
            .ToList();

        var orderedNewImages = request.NewImages
            .Select((image, index) => new { image, index })
            .Where(x => !string.IsNullOrWhiteSpace(x.image.ImageUrl))
            .OrderBy(x => x.image.SortOrder > 0 ? x.image.SortOrder : int.MaxValue)
            .ThenBy(x => x.index)
            .Select(x => x.image)
            .ToArray();

        var rebuiltImages = new List<CarImage>(orderedExisting.Count + orderedNewImages.Length);
        rebuiltImages.AddRange(orderedExisting.Select(image => new CarImage
        {
            Id = Guid.NewGuid(),
            CarId = request.Id,
            ImageUrl = image.ImageUrl,
            IsPrimary = false,
            SortOrder = 0,
            CreatedAtUtc = image.CreatedAtUtc
        }));

        rebuiltImages.AddRange(orderedNewImages.Select(image => new CarImage
        {
            Id = Guid.NewGuid(),
            CarId = request.Id,
            ImageUrl = image.ImageUrl.Trim(),
            IsPrimary = false,
            SortOrder = 0,
            CreatedAtUtc = now
        }));

        for (var i = 0; i < rebuiltImages.Count; i++)
        {
            rebuiltImages[i].SortOrder = i + 1;
        }

        if (rebuiltImages.Count > 0)
        {
            if (request.SelectedPrimaryImageId.HasValue && remainingById.TryGetValue(request.SelectedPrimaryImageId.Value, out var selectedExisting))
            {
                var target = rebuiltImages.FirstOrDefault(i => string.Equals(i.ImageUrl, selectedExisting.ImageUrl, StringComparison.OrdinalIgnoreCase));
                if (target is not null)
                {
                    target.IsPrimary = true;
                }
            }
            else
            {
                var firstRequestedPrimaryUrl = orderedNewImages.FirstOrDefault(i => i.IsPrimary)?.ImageUrl?.Trim();
                if (!string.IsNullOrWhiteSpace(firstRequestedPrimaryUrl))
                {
                    var target = rebuiltImages.FirstOrDefault(i => string.Equals(i.ImageUrl, firstRequestedPrimaryUrl, StringComparison.OrdinalIgnoreCase));
                    if (target is not null)
                    {
                        target.IsPrimary = true;
                    }
                }
            }

            if (!rebuiltImages.Any(i => i.IsPrimary))
            {
                rebuiltImages[0].IsPrimary = true;
            }
        }

        await _carRepository.DeleteImagesByCarIdAsync(request.Id, cancellationToken);
        await _carRepository.AddImagesRangeAsync(rebuiltImages, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return CarEditOperationResultDto.Success("Car and images updated successfully.", request.Id, deletedImageUrls);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CarEditOperationResultDto.Failure("This car was modified during your update. Please reload and try again.");
        }
    }

    public async Task<CarOperationResultDto> AddImagesAsync(Guid carId, IReadOnlyList<CreateCarImageRequestDto> images, CancellationToken cancellationToken = default)
    {
        if (images.Count == 0)
        {
            return CarOperationResultDto.Success("No images to upload.");
        }

        var orderedImages = images
            .Select((image, index) => new { image, index })
            .Where(x => !string.IsNullOrWhiteSpace(x.image.ImageUrl))
            .OrderBy(x => x.image.SortOrder > 0 ? x.image.SortOrder : int.MaxValue)
            .ThenBy(x => x.index)
            .Select(x => x.image)
            .ToArray();

        if (orderedImages.Length == 0)
        {
            return CarOperationResultDto.Success("No valid images to upload.");
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var car = await _carRepository.GetByIdForUpdateWithImagesAsync(carId, cancellationToken);
            if (car is null)
            {
                return CarOperationResultDto.Failure("Car not found.");
            }

            var hasPrimary = car.Images.Any(i => i.IsPrimary);
            var nextSortOrder = car.Images.Count == 0 ? 1 : car.Images.Max(i => i.SortOrder) + 1;

            foreach (var image in orderedImages)
            {
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

            try
            {
                var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
                return affected > 0
                    ? CarOperationResultDto.Success("Images uploaded successfully.", car.Id)
                    : CarOperationResultDto.Failure("Could not save car images.");
            }
            catch (DbUpdateConcurrencyException) when (attempt == 0)
            {
                // Retry once with freshly loaded tracked state.
            }
            catch (DbUpdateConcurrencyException)
            {
                return CarOperationResultDto.Failure("This car was modified during image upload. Please reload and try again.");
            }
        }

        return CarOperationResultDto.Failure("This car was modified during image upload. Please reload and try again.");
    }

    public async Task<CarOperationResultDto> UpdateImageSortOrdersAsync(
        Guid carId,
        IReadOnlyList<UpdateCarImageSortOrderRequestDto> imageOrders,
        CancellationToken cancellationToken = default)
    {
        if (imageOrders.Count == 0)
        {
            return CarOperationResultDto.Success("No image order updates.");
        }

        var car = await _carRepository.GetByIdForUpdateWithImagesAsync(carId, cancellationToken);
        if (car is null)
        {
            return CarOperationResultDto.Failure("Car not found.");
        }

        var existingImagesById = car.Images.ToDictionary(i => i.Id);
        var requestedOrders = imageOrders
            .Where(x => x.SortOrder > 0 && existingImagesById.ContainsKey(x.ImageId))
            .GroupBy(x => x.ImageId)
            .Select(g => g.First())
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ImageId)
            .ToArray();

        if (requestedOrders.Length == 0)
        {
            return CarOperationResultDto.Success("No valid image order updates.");
        }

        var requestedIds = requestedOrders.Select(x => x.ImageId).ToHashSet();
        var reordered = requestedOrders
            .Select(x => existingImagesById[x.ImageId])
            .Concat(car.Images
                .Where(x => !requestedIds.Contains(x.Id))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAtUtc))
            .ToArray();

        var nextSortOrder = 1;
        foreach (var image in reordered)
        {
            image.SortOrder = nextSortOrder++;
        }

        try
        {
            var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
            return affected > 0
                ? CarOperationResultDto.Success("Image order updated.", car.Id)
                : CarOperationResultDto.Failure("Could not update image order.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return CarOperationResultDto.Failure("This car was modified during image reordering. Please reload and try again.");
        }
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

        try
        {
            var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
            return affected > 0
                ? CarOperationResultDto.Success("Image deleted successfully.", car.Id)
                : CarOperationResultDto.Failure("Could not delete image.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return CarOperationResultDto.Failure("This car was modified during image deletion. Please reload and try again.");
        }
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
        var affected = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return affected > 0
            ? CarOperationResultDto.Success("Primary image updated.", car.Id)
            : CarOperationResultDto.Failure("Could not update primary image.");
    }

    private static CarOperationResultDto? Validate(string make, string model, int year, decimal price, int mileageKm, int inStock, int totalSold)
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

        if (inStock < 0)
        {
            return CarOperationResultDto.Failure("Validation failed.", "In Stock cannot be negative.");
        }

        if (totalSold < 0)
        {
            return CarOperationResultDto.Failure("Validation failed.", "Total Sold cannot be negative.");
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
            FuelType = car.FuelType,
            Transmission = car.Transmission,
            InStock = car.InStock,
            TotalSold = car.TotalSold
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
            FuelType = car.FuelType,
            Transmission = car.Transmission,
            ShortDescription = car.ShortDescription,
            Color = car.Color,
            Vin = car.Vin,
            InStock = car.InStock,
            TotalSold = car.TotalSold,
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
