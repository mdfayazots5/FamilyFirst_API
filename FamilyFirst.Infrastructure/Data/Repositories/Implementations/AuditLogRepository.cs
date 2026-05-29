using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public AuditLogRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
