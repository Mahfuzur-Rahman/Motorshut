using MotorsHut.DAL.Abstractions.Repositories;

namespace MotorsHut.DAL.Abstractions;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }
    ICarRepository Cars { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
