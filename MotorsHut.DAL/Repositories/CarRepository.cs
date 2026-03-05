using Microsoft.EntityFrameworkCore;
using MotorsHut.DAL.Abstractions.Repositories;
using MotorsHut.DAL.Data;
using MotorsHut.DAL.Entities;

namespace MotorsHut.DAL.Repositories;

public sealed class CarRepository : GenericRepository<Car>, ICarRepository
{
    public CarRepository(MotorsHutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Car>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        IQueryable<Car> query = Context.Set<Car>()
            .AsNoTracking()
            .Include(c => c.Images);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Make.Contains(term) ||
                c.Model.Contains(term) ||
                (c.Variant != null && c.Variant.Contains(term)) ||
                (c.Color != null && c.Color.Contains(term)) ||
                (c.Vin != null && c.Vin.Contains(term)));
        }

        return await query
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ThenByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Car?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Car>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Car?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Car>()
            .AsNoTracking()
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Car?> GetByIdForUpdateWithImagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Car>()
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CarImage?> GetImageByIdForUpdateAsync(Guid carId, Guid imageId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<CarImage>()
            .FirstOrDefaultAsync(i => i.CarId == carId && i.Id == imageId, cancellationToken);
    }

    public async Task<IReadOnlyList<CarImage>> ListImagesByCarIdAsync(Guid carId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<CarImage>()
            .AsNoTracking()
            .Where(i => i.CarId == carId)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> UpdateCarFieldsAsync(
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
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Car>()
            .Where(c => c.Id == carId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Make, make)
                .SetProperty(c => c.Model, model)
                .SetProperty(c => c.Variant, variant)
                .SetProperty(c => c.Year, year)
                .SetProperty(c => c.Price, price)
                .SetProperty(c => c.MileageKm, mileageKm)
                .SetProperty(c => c.FuelType, fuelType)
                .SetProperty(c => c.Transmission, transmission)
                .SetProperty(c => c.ShortDescription, shortDescription)
                .SetProperty(c => c.Color, color)
                .SetProperty(c => c.Vin, vin)
                .SetProperty(c => c.InStock, inStock)
                .SetProperty(c => c.TotalSold, totalSold)
                .SetProperty(c => c.UpdatedAtUtc, updatedAtUtc),
                cancellationToken);
    }

    public async Task<int> DeleteImagesByCarIdAsync(Guid carId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<CarImage>()
            .Where(i => i.CarId == carId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AddImagesRangeAsync(IReadOnlyList<CarImage> images, CancellationToken cancellationToken = default)
    {
        if (images.Count == 0)
        {
            return;
        }

        await Context.Set<CarImage>().AddRangeAsync(images, cancellationToken);
    }
}
