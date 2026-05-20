using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

internal sealed class LoginUserStore : ILoginUserStore
{
    private readonly AppDbContext _context;

    public LoginUserStore(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == normalizedEmail,
            cancellationToken);
}
