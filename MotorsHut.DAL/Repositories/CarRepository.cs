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
}
