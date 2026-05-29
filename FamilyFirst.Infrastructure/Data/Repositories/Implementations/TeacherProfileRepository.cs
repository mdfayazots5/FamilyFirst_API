using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class TeacherProfileRepository : ITeacherProfileRepository
{
    private readonly FamilyFirstDbContext _dbContext;

    public TeacherProfileRepository(FamilyFirstDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TeacherProfile?> GetByIdAsync(Guid teacherProfileId, CancellationToken cancellationToken)
    {
        return QueryProfiles()
            .SingleOrDefaultAsync(profile => profile.Id == teacherProfileId, cancellationToken);
    }

    public Task<TeacherProfile?> GetByFamilyMemberIdAsync(Guid familyMemberId, CancellationToken cancellationToken)
    {
        return QueryProfiles()
            .SingleOrDefaultAsync(profile => profile.FamilyMemberId == familyMemberId, cancellationToken);
    }

    public async Task AddAsync(TeacherProfile teacherProfile, CancellationToken cancellationToken)
    {
        await _dbContext.TeacherProfiles.AddAsync(teacherProfile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TeacherProfile> QueryProfiles()
    {
        return _dbContext.TeacherProfiles
            .Include(profile => profile.FamilyMember)
            .ThenInclude(member => member!.User)
            .Include(profile => profile.User)
            .Include(profile => profile.Family);
    }
}
