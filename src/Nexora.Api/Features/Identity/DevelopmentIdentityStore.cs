using System.Security.Cryptography;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Domain.Identity;

namespace Nexora.Api.Features.Identity;

/// <summary>
/// Development-only identity store used to keep the early API surface runnable while
/// the SQL-backed M01 repositories are implemented. This is not the final
/// persistence boundary and must not be used as production authority.
/// </summary>
public sealed class DevelopmentIdentityStore
{
    private static readonly TimeSpan VerificationTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan ResetTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan IdleTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AbsoluteTtl = TimeSpan.FromHours(12);

    private readonly object _sync = new();
    private readonly PasswordHashService _passwords;
    private readonly SessionCookieService _sessions;
    private readonly Dictionary<string, UserRecord> _usersByNormalizedEmail = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, UserRecord> _usersById = new();
    private readonly Dictionary<string, TokenRecord> _tokensByHash = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, SessionRecord> _sessionsById = new();
    private readonly Dictionary<string, SessionRecord> _sessionsByHash = new(StringComparer.Ordinal);
    private readonly List<DevAccountMessage> _capturedMessages = new();

    public DevelopmentIdentityStore(PasswordHashService passwords, SessionCookieService sessions)
    {
        _passwords = passwords;
        _sessions = sessions;
    }

    public ApiResult<AcceptedResponse> Register(RegistrationRequest request)
    {
        var validation = ValidateRegistration(request);
        if (validation is not null)
        {
            return validation;
        }

        var normalizedEmail = EmailNormalizer.Normalize(request.Email);
        lock (_sync)
        {
            if (_usersByNormalizedEmail.ContainsKey(normalizedEmail))
            {
                return Accepted();
            }

            var now = DateTimeOffset.UtcNow;
            var trimmedDisplayName = request.DisplayName?.Trim();
            var user = new UserRecord
            {
                Id = Guid.NewGuid(),
                Email = request.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = _passwords.Hash(request.Password),
                SecurityStamp = NewStamp(),
                State = UserState.PendingVerification,
                EmailConfirmed = false,
                IsDeleted = false,
                DisplayName = string.IsNullOrWhiteSpace(trimmedDisplayName)
                    ? RegistrationPolicy.BuildDefaultDisplayName(request.Email)
                    : trimmedDisplayName[..Math.Min(trimmedDisplayName.Length, RegistrationPolicy.DisplayNameMaxLength)],
                TimeZoneId = request.TimeZoneId.Trim(),
                Locale = RegistrationPolicy.DefaultLocale,
                RowVersion = 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            _usersByNormalizedEmail.Add(user.NormalizedEmail, user);
            _usersById.Add(user.Id, user);
            CaptureToken(user, "EmailVerification", VerificationTtl);
        }

        return Accepted();
    }

    public ApiResult<VerificationResponse> Verify(TokenProofRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResult<VerificationResponse>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, "Token is required.");
        }

        lock (_sync)
        {
            if (!TryConsumeToken(request.Token, "EmailVerification", out var user, consume: true))
            {
                return ApiResult<VerificationResponse>.Failure("TokenUnavailable", StatusCodes.Status410Gone, "Token unavailable.");
            }

            if (user.State != UserState.PendingVerification)
            {
                return ApiResult<VerificationResponse>.Failure("TokenUnavailable", StatusCodes.Status410Gone, "Token unavailable.");
            }

            user.State = UserState.Active;
            user.EmailConfirmed = true;
            user.VerifiedAt = DateTimeOffset.UtcNow;
            user.PersonalSpaceId ??= Guid.NewGuid();
            Touch(user);

            var profile = ToProfile(user);
            return ApiResult<VerificationResponse>.Success(new VerificationResponse("Verified", "EmailVerified", profile));
        }
    }

    public ApiResult<AcceptedResponse> ResendVerification(EmailRequest request)
    {
        var normalizedEmail = TryNormalizeEmail(request.Email);
        lock (_sync)
        {
            if (normalizedEmail is not null &&
                _usersByNormalizedEmail.TryGetValue(normalizedEmail, out var user) &&
                user.State == UserState.PendingVerification)
            {
                CaptureToken(user, "EmailVerification", VerificationTtl);
            }
        }

        return Accepted();
    }

    public ApiResult<LoginIssue> Login(CredentialsRequest request)
    {
        var normalizedEmail = TryNormalizeEmail(request.Email);
        UserRecord? user = null;

        lock (_sync)
        {
            if (normalizedEmail is not null)
            {
                _usersByNormalizedEmail.TryGetValue(normalizedEmail, out user);
            }

            if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
            {
                return ApiResult<LoginIssue>.Failure("InvalidCredentials", StatusCodes.Status401Unauthorized, "Invalid credentials.");
            }

            var decision = AccountAccessPolicy.CanLogin(user.ToSnapshot());
            if (!decision.Allowed)
            {
                var status = decision.Code == "InvalidCredentials" ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden;
                return ApiResult<LoginIssue>.Failure(decision.Code, status, decision.Message);
            }

            var rawHandle = _sessions.IssueRawHandle();
            var now = DateTimeOffset.UtcNow;
            var record = new SessionRecord
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                HandleHash = _sessions.Digest(rawHandle),
                SecurityStamp = user.SecurityStamp,
                DeviceLabel = "Local browser session",
                CreatedAt = now,
                LastSeenAt = now,
                IdleExpiresAt = now.Add(IdleTtl),
                AbsoluteExpiresAt = now.Add(AbsoluteTtl),
                RecentAuthenticatedAt = now
            };

            _sessionsById.Add(record.Id, record);
            _sessionsByHash.Add(record.HandleHash, record);

            return ApiResult<LoginIssue>.Success(new LoginIssue(ToProfile(user), rawHandle, record.AbsoluteExpiresAt));
        }
    }

    public ApiResult<object?> Logout(string? rawHandle)
    {
        lock (_sync)
        {
            var session = FindSession(rawHandle);
            if (session is not null)
            {
                session.RevokedAt ??= DateTimeOffset.UtcNow;
            }
        }

        return ApiResult<object?>.NoContent();
    }

    public ApiResult<object?> Reauth(string? rawHandle, PasswordProofRequest request)
    {
        lock (_sync)
        {
            var auth = AuthenticateSession(rawHandle);
            if (!auth.Succeeded)
            {
                return ApiResult<object?>.Failure(auth.Code, auth.StatusCode, auth.Title);
            }

            if (!_passwords.Verify(request.Password, auth.User!.PasswordHash))
            {
                return ApiResult<object?>.Failure("InvalidCredentials", StatusCodes.Status401Unauthorized, "Invalid credentials.");
            }

            auth.Session!.RecentAuthenticatedAt = DateTimeOffset.UtcNow;
            return ApiResult<object?>.NoContent();
        }
    }

    public ApiResult<AcceptedResponse> RequestPasswordReset(EmailRequest request)
    {
        var normalizedEmail = TryNormalizeEmail(request.Email);
        lock (_sync)
        {
            if (normalizedEmail is not null &&
                _usersByNormalizedEmail.TryGetValue(normalizedEmail, out var user) &&
                !user.IsDeleted)
            {
                CaptureToken(user, "PasswordReset", ResetTtl);
            }
        }

        return Accepted();
    }

    public ApiResult<object?> ConfirmPasswordReset(ResetProofRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResult<object?>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, "Token is required.");
        }

        var passwordDecision = PasswordPolicy.Validate(request.NewPassword);
        if (!passwordDecision.Allowed)
        {
            return ApiResult<object?>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, passwordDecision.Message);
        }

        lock (_sync)
        {
            if (!TryFindToken(request.Token, "PasswordReset", out var token, out var user))
            {
                return ApiResult<object?>.Failure("TokenUnavailable", StatusCodes.Status410Gone, "Token unavailable.");
            }

            var decision = AccountAccessPolicy.CanConfirmPasswordReset(user.ToSnapshot());
            if (!decision.Allowed)
            {
                var status = decision.Code == "MfaRecoveryRequired" ? StatusCodes.Status409Conflict : StatusCodes.Status403Forbidden;
                return ApiResult<object?>.Failure(decision.Code, status, decision.Message);
            }

            token.ConsumedAt = DateTimeOffset.UtcNow;
            user.PasswordHash = _passwords.Hash(request.NewPassword);
            user.SecurityStamp = NewStamp();
            Touch(user);

            foreach (var session in _sessionsById.Values.Where(s => s.UserId == user.Id && s.RevokedAt is null))
            {
                session.RevokedAt = DateTimeOffset.UtcNow;
            }

            return ApiResult<object?>.NoContent();
        }
    }

    public ApiResult<ProfileReadResult> GetMe(string? rawHandle)
    {
        lock (_sync)
        {
            var auth = AuthenticateSession(rawHandle);
            if (!auth.Succeeded)
            {
                return ApiResult<ProfileReadResult>.Failure(auth.Code, auth.StatusCode, auth.Title);
            }

            return ApiResult<ProfileReadResult>.Success(new ProfileReadResult(ToProfile(auth.User!), ETag(auth.User!.RowVersion)));
        }
    }

    public ApiResult<ProfileReadResult> UpdateMe(string? rawHandle, string? ifMatch, ProfilePatchRequest request)
    {
        lock (_sync)
        {
            var auth = AuthenticateSession(rawHandle);
            if (!auth.Succeeded)
            {
                return ApiResult<ProfileReadResult>.Failure(auth.Code, auth.StatusCode, auth.Title);
            }

            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return ApiResult<ProfileReadResult>.Failure("PreconditionRequired", StatusCodes.Status428PreconditionRequired, "If-Match is required.");
            }

            var user = auth.User!;
            if (!string.Equals(ifMatch, ETag(user.RowVersion), StringComparison.Ordinal))
            {
                return ApiResult<ProfileReadResult>.Failure("RevisionConflict", StatusCodes.Status412PreconditionFailed, "Resource revision changed.");
            }

            if (request.DisplayName is null && request.TimeZoneId is null && request.Locale is null)
            {
                return ApiResult<ProfileReadResult>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, "At least one allowed field is required.");
            }

            if (request.DisplayName is { } displayName)
            {
                if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > RegistrationPolicy.DisplayNameMaxLength)
                {
                    return ApiResult<ProfileReadResult>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, "displayName is invalid.");
                }

                user.DisplayName = displayName.Trim();
            }

            if (request.TimeZoneId is { } timeZoneId)
            {
                var decision = RegistrationPolicy.ValidateTimeZoneId(timeZoneId);
                if (!decision.Allowed)
                {
                    return ApiResult<ProfileReadResult>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, decision.Message);
                }

                user.TimeZoneId = timeZoneId.Trim();
            }

            if (request.Locale is { } locale)
            {
                var decision = RegistrationPolicy.ValidateLocale(locale);
                if (!decision.Allowed)
                {
                    return ApiResult<ProfileReadResult>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, decision.Message);
                }

                user.Locale = locale;
            }

            Touch(user);
            return ApiResult<ProfileReadResult>.Success(new ProfileReadResult(ToProfile(user), ETag(user.RowVersion)));
        }
    }

    public ApiResult<SessionPage> ListSessions(string? rawHandle)
    {
        lock (_sync)
        {
            var auth = AuthenticateSession(rawHandle);
            if (!auth.Succeeded)
            {
                return ApiResult<SessionPage>.Failure(auth.Code, auth.StatusCode, auth.Title);
            }

            var current = auth.Session!;
            var now = DateTimeOffset.UtcNow;
            var sessions = _sessionsById.Values
                .Where(s => s.UserId == auth.User!.Id && s.RevokedAt is null && s.AbsoluteExpiresAt > now && s.IdleExpiresAt > now)
                .OrderByDescending(s => s.CreatedAt)
                .ThenBy(s => s.Id)
                .Take(25)
                .Select(s => new SessionProjection(s.Id, s.DeviceLabel, s.CreatedAt, s.LastSeenAt, s.AbsoluteExpiresAt, s.Id == current.Id))
                .ToArray();

            return ApiResult<SessionPage>.Success(new SessionPage(sessions, null));
        }
    }

    public ApiResult<object?> RevokeSession(string? rawHandle, Guid sessionId)
    {
        lock (_sync)
        {
            var auth = AuthenticateSession(rawHandle);
            if (!auth.Succeeded)
            {
                return ApiResult<object?>.Failure(auth.Code, auth.StatusCode, auth.Title);
            }

            if (!_sessionsById.TryGetValue(sessionId, out var target) || target.UserId != auth.User!.Id)
            {
                return ApiResult<object?>.Failure("ResourceUnavailable", StatusCodes.Status404NotFound, "Session unavailable.");
            }

            target.RevokedAt ??= DateTimeOffset.UtcNow;
            return ApiResult<object?>.NoContent();
        }
    }

    public ApiResult<object?> RevokeAll(string? rawHandle)
    {
        lock (_sync)
        {
            var auth = AuthenticateSession(rawHandle);
            if (!auth.Succeeded)
            {
                return ApiResult<object?>.Failure(auth.Code, auth.StatusCode, auth.Title);
            }

            foreach (var session in _sessionsById.Values.Where(s => s.UserId == auth.User!.Id && s.RevokedAt is null))
            {
                session.RevokedAt = DateTimeOffset.UtcNow;
            }

            return ApiResult<object?>.NoContent();
        }
    }

    public IReadOnlyList<DevAccountMessage> CapturedMessages()
    {
        lock (_sync)
        {
            return _capturedMessages.OrderByDescending(message => message.CreatedAt).ToArray();
        }
    }

    private ApiResult<AcceptedResponse>? ValidateRegistration(RegistrationRequest request)
    {
        try
        {
            _ = EmailNormalizer.Normalize(request.Email);
        }
        catch (Exception)
        {
            return ApiResult<AcceptedResponse>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, "Email is invalid.");
        }

        var passwordDecision = PasswordPolicy.Validate(request.Password);
        if (!passwordDecision.Allowed)
        {
            return ApiResult<AcceptedResponse>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, passwordDecision.Message);
        }

        var timeZoneDecision = RegistrationPolicy.ValidateTimeZoneId(request.TimeZoneId);
        if (!timeZoneDecision.Allowed)
        {
            return ApiResult<AcceptedResponse>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, timeZoneDecision.Message);
        }

        if (request.DisplayName is not null && request.DisplayName.Length > RegistrationPolicy.DisplayNameMaxLength)
        {
            return ApiResult<AcceptedResponse>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, "displayName is too long.");
        }

        return null;
    }

    private static ApiResult<AcceptedResponse> Accepted() =>
        ApiResult<AcceptedResponse>.Success(new AcceptedResponse("Accepted", "CheckEmailIfEligible"), StatusCodes.Status202Accepted, "Accepted");

    private static string? TryNormalizeEmail(string? email)
    {
        try
        {
            return email is null ? null : EmailNormalizer.Normalize(email);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void CaptureToken(UserRecord user, string purpose, TimeSpan ttl)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        var token = new TokenRecord(Guid.NewGuid(), user.Id, purpose, HashToken(rawToken), expiresAt, null);
        _tokensByHash[token.TokenHash] = token;
        _capturedMessages.Add(new DevAccountMessage(token.Id, purpose, user.Email, rawToken, DateTimeOffset.UtcNow, expiresAt));
    }

    private bool TryConsumeToken(string rawToken, string purpose, out UserRecord user, bool consume)
    {
        if (!TryFindToken(rawToken, purpose, out var token, out user))
        {
            return false;
        }

        if (consume)
        {
            token.ConsumedAt = DateTimeOffset.UtcNow;
        }

        return true;
    }

    private bool TryFindToken(string rawToken, string purpose, out TokenRecord token, out UserRecord user)
    {
        token = null!;
        user = null!;
        var hash = HashToken(rawToken);
        if (!_tokensByHash.TryGetValue(hash, out var found) ||
            found.Purpose != purpose ||
            found.ConsumedAt is not null ||
            found.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        if (!_usersById.TryGetValue(found.UserId, out var foundUser))
        {
            return false;
        }

        token = found;
        user = foundUser;
        return true;
    }

    private AuthResult AuthenticateSession(string? rawHandle)
    {
        var session = FindSession(rawHandle);
        if (session is null || !_usersById.TryGetValue(session.UserId, out var user))
        {
            return AuthResult.Fail("AuthenticationRequired", StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        var now = DateTimeOffset.UtcNow;
        if (session.RevokedAt is not null ||
            session.AbsoluteExpiresAt <= now ||
            session.IdleExpiresAt <= now ||
            session.SecurityStamp != user.SecurityStamp)
        {
            return AuthResult.Fail("AuthenticationRequired", StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        var decision = AccountAccessPolicy.CanLogin(user.ToSnapshot());
        if (!decision.Allowed)
        {
            return AuthResult.Fail(decision.Code, StatusCodes.Status403Forbidden, decision.Message);
        }

        session.LastSeenAt = now;
        session.IdleExpiresAt = now.Add(IdleTtl);
        return AuthResult.Ok(user, session);
    }

    private SessionRecord? FindSession(string? rawHandle)
    {
        if (string.IsNullOrWhiteSpace(rawHandle))
        {
            return null;
        }

        return _sessionsByHash.TryGetValue(_sessions.Digest(rawHandle), out var session) ? session : null;
    }

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));

    private static string NewStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string ETag(long rowVersion) => $"\"{Convert.ToBase64String(BitConverter.GetBytes(rowVersion))}\"";

    private static void Touch(UserRecord user)
    {
        user.RowVersion++;
        user.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static ProfileResponse ToProfile(UserRecord user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.TimeZoneId,
            user.Locale,
            user.State.ToString(),
            user.PersonalSpaceId,
            new[]
            {
                new ModuleProjection("identity", true, null),
                new ModuleProjection("settings", true, null)
            });

    private sealed class UserRecord
    {
        public Guid Id { get; init; }
        public required string Email { get; init; }
        public required string NormalizedEmail { get; init; }
        public required string PasswordHash { get; set; }
        public required string SecurityStamp { get; set; }
        public UserState State { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsDeleted { get; set; }
        public bool HasEnabledMfa { get; set; }
        public required string DisplayName { get; set; }
        public required string TimeZoneId { get; set; }
        public required string Locale { get; set; }
        public Guid? PersonalSpaceId { get; set; }
        public long RowVersion { get; set; }
        public DateTimeOffset? VerifiedAt { get; set; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; set; }

        public UserLoginSnapshot ToSnapshot() =>
            new(Id, State, EmailConfirmed, IsDeleted, State == UserState.Disabled, HasEnabledMfa, SecurityStamp);
    }

    private sealed class TokenRecord
    {
        public TokenRecord(Guid id, Guid userId, string purpose, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset? consumedAt)
        {
            Id = id;
            UserId = userId;
            Purpose = purpose;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            ConsumedAt = consumedAt;
        }

        public Guid Id { get; }
        public Guid UserId { get; }
        public string Purpose { get; }
        public string TokenHash { get; }
        public DateTimeOffset ExpiresAt { get; }
        public DateTimeOffset? ConsumedAt { get; set; }
    }

    private sealed class SessionRecord
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public required string HandleHash { get; init; }
        public required string SecurityStamp { get; init; }
        public required string DeviceLabel { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset LastSeenAt { get; set; }
        public DateTimeOffset IdleExpiresAt { get; set; }
        public DateTimeOffset AbsoluteExpiresAt { get; init; }
        public DateTimeOffset? RecentAuthenticatedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
    }

    private sealed record AuthResult(bool Succeeded, UserRecord? User, SessionRecord? Session, string Code, int StatusCode, string Title)
    {
        public static AuthResult Ok(UserRecord user, SessionRecord session) => new(true, user, session, "Ok", StatusCodes.Status200OK, "Ok");
        public static AuthResult Fail(string code, int statusCode, string title) => new(false, null, null, code, statusCode, title);
    }
}
