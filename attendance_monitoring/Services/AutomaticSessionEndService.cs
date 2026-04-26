using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace attendance_monitoring.Services;

public sealed class AutomaticSessionEndService(
    ISessionRepository sessionRepository,
    IOptions<SessionAutoEndOptions> options,
    ConfiguredTimeZoneProvider clock,
    ILogger<AutomaticSessionEndService> logger) : IAutomaticSessionEndService
{
    private const string AutoEndNote = "Auto-ended by system after scheduled end grace period.";
    private readonly SessionAutoEndOptions _options = options.Value;

    public DateTime CalculateAutoEndBoundary(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Schedule == null)
        {
            throw new InvalidOperationException("Session schedule information is required to calculate auto-end boundary.");
        }

        return session.SessionDate.Date
            .Add(session.Schedule.TimeOut.ToTimeSpan())
            .Add(_options.GracePeriod);
    }

    public bool IsPastAutoEndBoundary(Session session)
    {
        if (!_options.Enabled || session.Status != SessionStatusConstants.Active)
        {
            return false;
        }

        return clock.GetLocalNow() >= CalculateAutoEndBoundary(session);
    }

    public async Task<Session> AutoEndIfExpiredAsync(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsPastAutoEndBoundary(session))
        {
            return session;
        }

        var boundary = CalculateAutoEndBoundary(session);
        var processedAt = clock.GetLocalNow();

        session.Status = SessionStatusConstants.Ended;
        session.ActualEndTime = boundary;
        session.EndedBy = null;
        session.Description = AppendAutoEndNote(session.Description);

        try
        {
            await sessionRepository.UpdateSessionAsync(session).ConfigureAwait(false);
            await sessionRepository.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation(
                "Auto-ended session {SessionId} at boundary {AutoEndBoundary:o}; processed at {ProcessedAt:o}",
                session.Id,
                boundary,
                processedAt);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogInformation(
                ex,
                "Skipped auto-ending session {SessionId} because another update won the race",
                session.Id);
        }

        return session;
    }

    public async Task<int> AutoEndExpiredSessionsAsync()
    {
        if (!_options.Enabled)
        {
            logger.LogDebug("Session auto-end scan skipped because auto-end is disabled.");
            return 0;
        }

        var now = clock.GetLocalNow();
        var candidates = await sessionRepository
            .GetActiveSessionsForAutoEndScanAsync(now.Date)
            .ConfigureAwait(false);

        var endedCount = 0;
        foreach (var session in candidates)
        {
            if (!IsPastAutoEndBoundary(session))
            {
                continue;
            }

            await AutoEndIfExpiredAsync(session).ConfigureAwait(false);
            endedCount++;
        }

        logger.LogInformation(
            "Session auto-end scan completed. Auto-ended {EndedCount} session(s).",
            endedCount);

        return endedCount;
    }

    private static string AppendAutoEndNote(string? existingDescription)
    {
        if (string.IsNullOrWhiteSpace(existingDescription))
        {
            return AutoEndNote;
        }

        if (existingDescription.Contains(AutoEndNote, StringComparison.Ordinal))
        {
            return existingDescription;
        }

        var combined = $"{existingDescription}\n\n{AutoEndNote}";
        return combined.Length <= 500
            ? combined
            : combined[^500..];
    }
}
