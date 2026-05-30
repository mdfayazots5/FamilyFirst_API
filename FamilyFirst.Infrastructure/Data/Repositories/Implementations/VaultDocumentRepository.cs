using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class VaultDocumentRepository : IVaultDocumentRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public VaultDocumentRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<VaultDocument?> GetByIdAsync(Guid documentId, Guid familyId, CancellationToken cancellationToken)
    {
        return QueryDocuments()
            .SingleOrDefaultAsync(
                d => d.Id == documentId && d.FamilyId == familyId,
                cancellationToken);
    }

    public async Task<(IReadOnlyCollection<VaultDocument> Items, int TotalCount)> ListAsync(
        Guid familyId,
        DocumentCategory? category,
        Guid? memberId,
        string? search,
        string? expiryStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? sortBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<VaultDocument>()
            .Include(d => d.Member)
            .Where(d => d.FamilyId == familyId && d.IsCurrentVersion);

        if (category.HasValue)
            query = query.Where(d => d.Category == category.Value);

        if (memberId.HasValue)
            query = query.Where(d => d.MemberId == memberId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.DocumentName.Contains(search) || (d.Tags != null && d.Tags.Contains(search)));

        if (dateFrom.HasValue)
            query = query.Where(d => d.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(d => d.CreatedAt <= dateTo.Value);

        if (expiryStatus == "expiring-soon")
        {
            var cutoff = DateTime.UtcNow.AddDays(30);
            query = query.Where(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value <= cutoff);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = sortBy switch
        {
            "expiry" => query.OrderBy(d => d.ExpiryDate),
            "name"   => query.OrderBy(d => d.DocumentName),
            _        => query.OrderByDescending(d => d.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyCollection<VaultDocument>> ListExpiringAsync(
        Guid familyId,
        int withinDays,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        return await _dbContext.Set<VaultDocument>()
            .Include(d => d.Member)
            .Where(d => d.FamilyId == familyId
                     && d.IsCurrentVersion
                     && d.ExpiryDate.HasValue
                     && d.ExpiryDate.Value <= cutoff)
            .OrderBy(d => d.ExpiryDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VaultDocument>> ListEmergencyAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<VaultDocument>()
            .Include(d => d.Member)
            .Where(d => d.FamilyId == familyId && d.IsCurrentVersion && d.IsEmergencyPriority)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountEmergencyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<VaultDocument>()
            .CountAsync(
                d => d.FamilyId == familyId && d.IsCurrentVersion && d.IsEmergencyPriority,
                cancellationToken);
    }

    public async Task<VaultDocument> AddAsync(VaultDocument document, CancellationToken cancellationToken)
    {
        _dbContext.Set<VaultDocument>().Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task UpdateAsync(VaultDocument document, CancellationToken cancellationToken)
    {
        _dbContext.Set<VaultDocument>().Update(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VaultDocumentVersion>> GetVersionHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<VaultDocumentVersion>()
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddVersionAsync(VaultDocumentVersion version, CancellationToken cancellationToken)
    {
        _dbContext.Set<VaultDocumentVersion>().Add(version);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<VaultShareLink?> GetShareLinkByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return _dbContext.Set<VaultShareLink>()
            .Include(s => s.Document)
            .ThenInclude(d => d!.Member)
            .SingleOrDefaultAsync(s => s.Token == token, cancellationToken);
    }

    public async Task<VaultShareLink> AddShareLinkAsync(VaultShareLink shareLink, CancellationToken cancellationToken)
    {
        _dbContext.Set<VaultShareLink>().Add(shareLink);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return shareLink;
    }

    public async Task UpdateShareLinkAsync(VaultShareLink shareLink, CancellationToken cancellationToken)
    {
        _dbContext.Set<VaultShareLink>().Update(shareLink);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ReminderAlreadySentAsync(
        Guid documentId,
        int thresholdDays,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<VaultExpiryReminderLog>()
            .AnyAsync(
                r => r.DocumentId == documentId && r.ThresholdDays == thresholdDays,
                cancellationToken);
    }

    public async Task RecordReminderSentAsync(
        Guid documentId,
        Guid familyId,
        int thresholdDays,
        CancellationToken cancellationToken)
    {
        var log = new VaultExpiryReminderLog
        {
            DocumentId    = documentId,
            FamilyId      = familyId,
            ThresholdDays = thresholdDays,
            SentAt        = DateTime.UtcNow
        };

        _dbContext.Set<VaultExpiryReminderLog>().Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VaultDocument>> GetDocumentsDueForReminderAsync(
        int thresholdDays,
        CancellationToken cancellationToken)
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(thresholdDays);
        var targetStart = targetDate;
        var targetEnd   = targetDate.AddDays(1);

        return await _dbContext.Set<VaultDocument>()
            .Where(d => d.IsCurrentVersion
                     && d.ExpiryDate.HasValue
                     && d.ExpiryDate.Value >= targetStart
                     && d.ExpiryDate.Value < targetEnd)
            .ToArrayAsync(cancellationToken);
    }

    public Task<VaultFamilySettings?> GetVaultFamilySettingsAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<VaultFamilySettings>()
            .SingleOrDefaultAsync(s => s.FamilyId == familyId, cancellationToken);
    }

    public async Task UpsertVaultFamilySettingsAsync(
        VaultFamilySettings settings,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Set<VaultFamilySettings>()
            .SingleOrDefaultAsync(s => s.FamilyId == settings.FamilyId, cancellationToken);

        if (existing is null)
        {
            _dbContext.Set<VaultFamilySettings>().Add(settings);
        }
        else
        {
            existing.EmergencyAccessMode = settings.EmergencyAccessMode;
            existing.EmergencyPinHash    = settings.EmergencyPinHash;
            _dbContext.Set<VaultFamilySettings>().Update(existing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<VaultDocument> QueryDocuments()
    {
        return _dbContext.Set<VaultDocument>()
            .Include(d => d.Member)
            .Include(d => d.UploadedByUser)
            .Include(d => d.ShareLinks.Where(s => !s.IsDeleted && !s.IsRevoked));
    }
}
