using Microsoft.EntityFrameworkCore;
using MotorsHut.DAL.Abstractions.Repositories;
using MotorsHut.DAL.Data;
using MotorsHut.DAL.Entities;

namespace MotorsHut.DAL.Repositories;

public sealed class PasswordResetTokenRepository : GenericRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(MotorsHutDbContext context) : base(context)
    {
    }

    public async Task<PasswordResetToken?> GetValidByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        return await Context.PasswordResetTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.UsedAtUtc == null && t.ExpiresAtUtc > utcNow,
                cancellationToken);
    }
}
