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
        var (_, normalizedSession) = await TryAutoEndIfExpiredAsync(session).ConfigureAwait(false);
        return normalizedSession;
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
            var (updatedHere, _) = await TryAutoEndIfExpiredAsync(session).ConfigureAwait(false);
            if (updatedHere)
            {
                endedCount++;
            }
        }

        logger.LogInformation(
            "Session auto-end scan completed. Auto-ended {EndedCount} session(s).",
            endedCount);

        return endedCount;
    }

    private async Task<(bool UpdatedHere, Session Session)> TryAutoEndIfExpiredAsync(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsPastAutoEndBoundary(session))
        {
            return (false, session);
        }

        var boundary = CalculateAutoEndBoundary(session);
        var processedAt = clock.GetLocalNow();
        var originalStatus = session.Status;
        var originalActualEndTime = session.ActualEndTime;
        var originalEndedBy = session.EndedBy;
        var originalDescription = session.Description;

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
            return (true, session);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogInformation(
                ex,
                "Reloading session {SessionId} because another update won the auto-end race",
                session.Id);

            var currentSession = await sessionRepository.GetSessionByIdAsync(session.Id).ConfigureAwait(false);
            if (currentSession != null)
            {
                return (false, currentSession);
            }

            session.Status = originalStatus;
            session.ActualEndTime = originalActualEndTime;
            session.EndedBy = originalEndedBy;
            session.Description = originalDescription;
        }

        return (false, session);
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

        const string separator = "\n\n";
        var maxExistingDescriptionLength = Math.Max(0, 500 - separator.Length - AutoEndNote.Length);
        var trimmedDescription = existingDescription.Length <= maxExistingDescriptionLength
            ? existingDescription
            : existingDescription[..maxExistingDescriptionLength];

        return $"{trimmedDescription}{separator}{AutoEndNote}";
    }
}
