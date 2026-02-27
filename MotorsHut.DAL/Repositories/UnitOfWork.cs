using MotorsHut.DAL.Abstractions;
using MotorsHut.DAL.Abstractions.Repositories;
using MotorsHut.DAL.Data;

namespace MotorsHut.DAL.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MotorsHutDbContext _context;

    public UnitOfWork(
        MotorsHutDbContext context,
        IUserRepository users,
        IPasswordResetTokenRepository passwordResetTokens,
        ICarRepository cars)
    {
        _context = context;
        Users = users;
        PasswordResetTokens = passwordResetTokens;
        Cars = cars;
    }

    public IUserRepository Users { get; }
    public IPasswordResetTokenRepository PasswordResetTokens { get; }
    public ICarRepository Cars { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
