using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Infrastructure.Persistence;

internal sealed class RegistrationStore : IRegistrationStore
{
    private readonly AppDbContext _context;

    public RegistrationStore(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
        => _context.Users.AddAsync(user, cancellationToken).AsTask();

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        => _context.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();
}
