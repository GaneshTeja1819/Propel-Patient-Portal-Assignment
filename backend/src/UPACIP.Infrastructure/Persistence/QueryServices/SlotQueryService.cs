using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Persistence.QueryServices;

/// <summary>
/// Implementation of <see cref="ISlotQueryService"/> using EF Core.
/// Provides efficient database queries for appointment slot availability.
/// </summary>
internal sealed class SlotQueryService : ISlotQueryService
{
    private readonly AppDbContext _dbContext;

    public SlotQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SlotQueryDto>> GetAvailableSlotsAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var queryDate = date.Date;
        
        var slots = await _dbContext.AppointmentSlots
            .Where(s => s.StartTime.Date == queryDate && s.IsAvailable)
            .OrderBy(s => s.StartTime)
            .Select(s => new SlotQueryDto
            {
                Id = s.Id,
                ProviderId = s.ProviderId,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                DurationMinutes = s.DurationMinutes,
                IsAvailable = s.IsAvailable
            })
            .ToListAsync(cancellationToken);

        return slots;
    }
}
