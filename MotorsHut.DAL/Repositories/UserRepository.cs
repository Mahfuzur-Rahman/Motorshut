using Microsoft.EntityFrameworkCore;
using MotorsHut.DAL.Abstractions.Repositories;
using MotorsHut.DAL.Data;
using MotorsHut.DAL.Entities;

namespace MotorsHut.DAL.Repositories;

public sealed class UserRepository : GenericRepository<ApplicationUser>, IUserRepository
{
    public UserRepository(MotorsHutDbContext context) : base(context)
    {
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpper(), cancellationToken);
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await Context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == userName.ToUpper(), cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Users.AnyAsync(u => u.NormalizedEmail == email.ToUpper(), cancellationToken);
    }
}
