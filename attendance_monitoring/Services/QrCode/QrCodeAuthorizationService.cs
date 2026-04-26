using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;

namespace attendance_monitoring.Services.QrCode;

internal sealed class QrCodeAuthorizationService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IStudentEnrollmentService _studentEnrollmentService;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<QrCodeAuthorizationService> _logger;

    public QrCodeAuthorizationService(
        ISessionRepository sessionRepository,
        IStudentEnrollmentService studentEnrollmentService,
        IUserContextService userContextService,
        ILogger<QrCodeAuthorizationService> logger)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _studentEnrollmentService = studentEnrollmentService ?? throw new ArgumentNullException(nameof(studentEnrollmentService));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IUserContextService UserContext => _userContextService;

    /// <summary>
    /// Validates that the session exists and is active. Returns an error message string on failure, null on success.
    /// </summary>
    public async Task<string?> ValidateSessionExistsAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

        if (session == null)
        {
            _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
            return "The specified session does not exist";
        }

        if (session.Status != SessionStatusConstants.Active)
        {
            _logger.LogWarning("Session with ID {SessionId} is not active (status: {Status})", sessionId, session.Status);

            return session.Status switch
            {
                SessionStatusConstants.NotStarted => "Session has not started yet. Please start the session before generating QR codes.",
                SessionStatusConstants.Ended => "Session has already ended. QR codes cannot be generated for completed sessions.",
                SessionStatusConstants.Cancelled => "Session has been cancelled. QR codes cannot be generated for cancelled sessions.",
                _ => $"Session is not active. Current status: {session.Status}. Only active sessions can generate QR codes."
            };
        }

        return null;
    }

    /// <summary>
    /// Validates that the session exists and is active. Throws on failure.
    /// </summary>
    public async Task ValidateSessionExistsOrThrowAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

        if (session == null)
        {
            _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
            throw new EntityNotFoundException<Guid>("Session", sessionId);
        }

        if (session.Status != SessionStatusConstants.Active)
        {
            _logger.LogWarning("Session with ID {SessionId} is not active (status: {Status})", sessionId, session.Status);

            var message = session.Status switch
            {
                SessionStatusConstants.NotStarted => "Session has not started yet. Please start the session before generating QR codes.",
                SessionStatusConstants.Ended => "Session has already ended. QR codes cannot be generated for completed sessions.",
                SessionStatusConstants.Cancelled => "Session has been cancelled. QR codes cannot be generated for cancelled sessions.",
                _ => $"Session is not active. Current status: {session.Status}. Only active sessions can generate QR codes."
            };
            throw new ValidationException(message);
        }
    }

    public async Task<bool> IsStudentEnrolledInSectionSubjectAsync(Guid studentId, Guid sectionId, Guid subjectId)
    {
        try
        {
            return await _studentEnrollmentService
                .IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking student enrollment for Student: {StudentId}, Section: {SectionId}, Subject: {SubjectId}",
                studentId, sectionId, subjectId);
            return false;
        }
    }
}
