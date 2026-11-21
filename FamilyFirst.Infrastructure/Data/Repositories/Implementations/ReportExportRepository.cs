using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class ReportExportRepository(FamilyFirstDbContext dbContext) : IReportExportRepository
{
    public async Task AddAsync(ReportExport export, CancellationToken cancellationToken)
    {
        dbContext.ReportExports.Add(export);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
