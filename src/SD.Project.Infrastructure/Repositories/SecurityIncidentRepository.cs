using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the security incident repository.
/// </summary>
public sealed class SecurityIncidentRepository : ISecurityIncidentRepository
{
    private readonly AppDbContext _context;

    public SecurityIncidentRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SecurityIncident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityIncidents
            .Include(i => i.StatusHistory)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SecurityIncident?> GetByIncidentNumberAsync(string incidentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityIncidents
            .Include(i => i.StatusHistory)
            .FirstOrDefaultAsync(i => i.IncidentNumber == incidentNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GetNextIncidentNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INC-{year}-";

        // Get the highest incident number for this year
        var lastNumber = await _context.SecurityIncidents
            .Where(i => i.IncidentNumber.StartsWith(prefix))
            .Select(i => i.IncidentNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSequence = 1;
        if (lastNumber != null)
        {
            // Extract the sequence number from the last incident number
            var sequencePart = lastNumber.Substring(prefix.Length);
            if (int.TryParse(sequencePart, out var lastSequence))
            {
                nextSequence = lastSequence + 1;
            }
        }

        return $"{prefix}{nextSequence:D5}";
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecurityIncident>> GetAsync(
        SecurityIncidentStatus? status = null,
        SecurityIncidentSeverity? severity = null,
        string? incidentType = null,
        Guid? assignedToUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityIncidents.AsNoTracking();

        query = ApplyFilters(query, status, severity, incidentType, assignedToUserId, fromDate, toDate);

        return await query
            .OrderByDescending(i => i.DetectedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(
        SecurityIncidentStatus? status = null,
        SecurityIncidentSeverity? severity = null,
        string? incidentType = null,
        Guid? assignedToUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityIncidents.AsQueryable();

        query = ApplyFilters(query, status, severity, incidentType, assignedToUserId, fromDate, toDate);

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecurityIncident>> GetForExportAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.SecurityIncidents
            .AsNoTracking()
            .Where(i => i.DetectedAt >= fromDate && i.DetectedAt <= toDate)
            .OrderByDescending(i => i.DetectedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecurityIncident>> GetByAffectedUserIdAsync(
        Guid affectedUserId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.SecurityIncidents
            .AsNoTracking()
            .Where(i => i.AffectedUserId == affectedUserId)
            .OrderByDescending(i => i.DetectedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(SecurityIncident incident, CancellationToken cancellationToken = default)
    {
        await _context.SecurityIncidents.AddAsync(incident, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(SecurityIncident incident)
    {
        _context.SecurityIncidents.Update(incident);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<SecurityIncident> ApplyFilters(
        IQueryable<SecurityIncident> query,
        SecurityIncidentStatus? status,
        SecurityIncidentSeverity? severity,
        string? incidentType,
        Guid? assignedToUserId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(i => i.Severity == severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(incidentType))
        {
            query = query.Where(i => i.IncidentType == incidentType);
        }

        if (assignedToUserId.HasValue)
        {
            query = query.Where(i => i.AssignedToUserId == assignedToUserId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(i => i.DetectedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(i => i.DetectedAt <= toDate.Value);
        }

        return query;
    }
}
