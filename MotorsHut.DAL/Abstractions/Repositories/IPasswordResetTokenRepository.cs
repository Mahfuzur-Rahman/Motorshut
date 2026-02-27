using MotorsHut.DAL.Entities;

namespace MotorsHut.DAL.Abstractions.Repositories;

public interface IPasswordResetTokenRepository : IGenericRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetValidByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
