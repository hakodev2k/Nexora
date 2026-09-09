using Nexora.Api.Features.Identity;
using Nexora.Api.Features.Modules;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Security;
using Nexora.Domain.Access;
using Nexora.Domain.Identity;
using Nexora.Domain.Modules;
using Nexora.UnitTests;

var runner = new TestRunner();
ModulePolicyStoreTests.Register(runner);

runner.Add("email normalization trims and lowercases without provider alias rewriting", () =>
{
    var normalized = EmailNormalizer.Normalize("  User.Name+tag@Example.COM  ");
    AssertEx.Equal("user.name+tag@example.com", normalized, "Normalized email should keep dot and plus semantics");
});

runner.Add("email normalization applies IDNA domain normalization", () =>
{
    var normalized = EmailNormalizer.Normalize("USER@BÜCHER.example");
    AssertEx.Equal("user@xn--bcher-kva.example", normalized, "Unicode domain should normalize to lower-case IDNA ASCII");
});

runner.Add("password policy rejects short values", () =>
{
    var decision = PasswordPolicy.Validate("short-password");
    AssertEx.False(decision.Allowed, "Short password should be rejected");
    AssertEx.Equal("PasswordTooShort", decision.Code, "Expected short-password code");
});

runner.Add("password policy accepts fifteen unicode code points", () =>
{
    var decision = PasswordPolicy.Validate(new string('a', 15));
    AssertEx.True(decision.Allowed, "Fifteen-code-point password should pass length policy");
});

runner.Add("password hash verifies only the original secret", () =>
{
    var service = new PasswordHashService();
    var hash = service.Hash("correct-horse-phrase");
    AssertEx.True(service.Verify("correct-horse-phrase", hash), "Original password should verify");
    AssertEx.False(service.Verify("wrong-correct-horse", hash), "Different password must not verify");
});

runner.Add("registration defaults display name and locale", () =>
{
    var result = RegistrationCommandPolicy.Validate(new RegistrationCommand(
        "Example.User@example.test",
        "correct-horse-phrase",
        "Asia/Ho_Chi_Minh",
        null,
        null));

    AssertEx.True(result.IsValid, "Registration command should be valid");
    AssertEx.Equal("example.user@example.test", result.Draft!.NormalizedEmail, "Email should normalize");
    AssertEx.Equal("Example.User", result.Draft.DisplayName, "Display name should default to original local part");
    AssertEx.Equal("vi", result.Draft.Locale, "Locale should default to Vietnamese");
});

runner.Add("registration rejects unsupported locale", () =>
{
    var result = RegistrationCommandPolicy.Validate(new RegistrationCommand(
        "user@example.test",
        "correct-horse-phrase",
        "Asia/Ho_Chi_Minh",
        "User",
        "fr"));

    AssertEx.False(result.IsValid, "Unsupported locale should fail validation");
    AssertEx.True(result.Issues.Any(issue => issue.Code == "LocaleUnsupported"), "LocaleUnsupported issue should be present");
});

runner.Add("login denies pending verification", () =>
{
    var decision = AccountAccessPolicy.CanLogin(new UserLoginSnapshot(
        Guid.NewGuid(),
        UserState.PendingVerification,
        EmailConfirmed: false,
        IsDeleted: false,
        IsDisabled: false,
        HasEnabledMfa: false,
        SecurityStamp: "stamp"));

    AssertEx.False(decision.Allowed, "Pending verification account must not login");
    AssertEx.Equal("EmailVerificationRequired", decision.Code, "Expected email verification code");
});

runner.Add("login denies deleted account without restore side effects", () =>
{
    var decision = AccountAccessPolicy.CanLogin(new UserLoginSnapshot(
        Guid.NewGuid(),
        UserState.Deleted,
        EmailConfirmed: true,
        IsDeleted: true,
        IsDisabled: false,
        HasEnabledMfa: false,
        SecurityStamp: "stamp"));

    AssertEx.False(decision.Allowed, "Deleted account must not login");
    AssertEx.Equal("AccountUnavailable", decision.Code, "Expected account unavailable code");
});

runner.Add("login denies mfa-enabled fixture in M01", () =>
{
    var decision = AccountAccessPolicy.CanLogin(new UserLoginSnapshot(
        Guid.NewGuid(),
        UserState.Active,
        EmailConfirmed: true,
        IsDeleted: false,
        IsDisabled: false,
        HasEnabledMfa: true,
        SecurityStamp: "stamp"));

    AssertEx.False(decision.Allowed, "MFA-enabled fixture must fail closed in M01");
    AssertEx.Equal("MfaUnavailable", decision.Code, "Expected MfaUnavailable code");
});

runner.Add("password reset refuses to remove MFA", () =>
{
    var decision = AccountAccessPolicy.CanConfirmPasswordReset(new UserLoginSnapshot(
        Guid.NewGuid(),
        UserState.Active,
        EmailConfirmed: true,
        IsDeleted: false,
        IsDisabled: false,
        HasEnabledMfa: true,
        SecurityStamp: "stamp"));

    AssertEx.False(decision.Allowed, "Reset must not bypass MFA");
    AssertEx.Equal("MfaRecoveryRequired", decision.Code, "Expected MFA recovery requirement");
});

runner.Add("profile patch rejects privileged or ownership fields", () =>
{
    var decision = ProfilePatchPolicy.ValidatePatchFields(new[] { "displayName", "ownerId" });
    AssertEx.False(decision.Allowed, "OwnerId must not be editable through profile patch");
    AssertEx.Equal("UnknownField", decision.Code, "Expected UnknownField code");
});

runner.Add("last active SuperAdmin cannot be downgraded", () =>
{
    var decision = SuperAdminSafetyPolicy.CanRemoveOrDowngradeSuperAdmin(1, targetIsActiveSuperAdmin: true);
    AssertEx.False(decision.Allowed, "Last active SuperAdmin removal must be blocked");
    AssertEx.Equal("LastSuperAdmin", decision.Code, "Expected LastSuperAdmin blocker");
});

runner.Add("paused action cannot receive Allow grant", () =>
{
    var decision = ActionGrantPolicy.CanGrantAllow("automation.definition.enable");
    AssertEx.False(decision.Allowed, "Paused Automation action should be blocked");
    AssertEx.Equal("DecisionBlocked", decision.Code, "Expected decision blocker");
});

runner.Add("approved M01 action can receive Allow grant", () =>
{
    var decision = ActionGrantPolicy.CanGrantAllow("access.user.read");
    AssertEx.True(decision.Allowed, "M01 approved action should be grantable");
});

runner.Add("paused module cannot be enabled", () =>
{
    var decision = ModulePolicy.CanEnable("FX34", ModuleRuntimeState.Ready, Array.Empty<ModuleDependencyStatus>());
    AssertEx.False(decision.Allowed, "FX34 must remain PO_PAUSED");
    AssertEx.Equal("DecisionBlocked", decision.Code, "Expected decision blocker");
});

runner.Add("module disable blocks enabled hard dependent", () =>
{
    var decision = ModulePolicy.CanDisable(new[] { new ModuleDependencyStatus("FX12", SystemEnabled: true, Required: true) });
    AssertEx.False(decision.Allowed, "Enabled hard dependent should block disablement");
    AssertEx.Equal("DependencyEnabled", decision.Code, "Expected dependency blocker");
});

runner.Add("idempotency digest requires uuid key", () =>
{
    var secret = Enumerable.Repeat((byte)7, 32).ToArray();
    AssertEx.Throws<FormatException>(() => IdempotencyDigest.ComputeKeyHash(secret, "not-a-uuid"), "Invalid idempotency key should throw");
});

runner.Add("idempotency digest is stable and body-sensitive", () =>
{
    var secret = Enumerable.Repeat((byte)9, 32).ToArray();
    var left = IdempotencyDigest.ComputeRequestDigest(secret, "operation:register;email-hash:a");
    var same = IdempotencyDigest.ComputeRequestDigest(secret, "operation:register;email-hash:a");
    var different = IdempotencyDigest.ComputeRequestDigest(secret, "operation:register;email-hash:b");

    AssertEx.True(IdempotencyDigest.FixedTimeEquals(left, same), "Same canonical request should produce same digest");
    AssertEx.False(IdempotencyDigest.FixedTimeEquals(left, different), "Different canonical request should produce different digest");
});

runner.Add("runtime store can register verify and login through service layer", () =>
{
    var store = new DevelopmentIdentityStore(new PasswordHashService(), new SessionCookieService());
    var registration = store.Register(new RegistrationRequest("user@example.test", "correct-horse-phrase", "Asia/Ho_Chi_Minh", null));
    AssertEx.True(registration.Succeeded, "Registration should return generic success");
    AssertEx.Equal(202, registration.StatusCode, "Registration should return 202");

    var message = store.CapturedMessages().Single(item => item.Purpose == "EmailVerification");
    var verification = store.Verify(new TokenProofRequest(message.Token));
    AssertEx.True(verification.Succeeded, "Verification token should activate account");
    AssertEx.Equal("Active", verification.Value!.Profile.State, "Verified account should be active");

    var login = store.Login(new CredentialsRequest("user@example.test", "correct-horse-phrase"));
    AssertEx.True(login.Succeeded, "Verified account should login");
    AssertEx.True(!string.IsNullOrWhiteSpace(login.Value!.RawSessionHandle), "Login should issue an opaque session handle");
});

runner.Add("runtime store prevents duplicate verification token replay", () =>
{
    var store = new DevelopmentIdentityStore(new PasswordHashService(), new SessionCookieService());
    _ = store.Register(new RegistrationRequest("replay@example.test", "correct-horse-phrase", "Asia/Ho_Chi_Minh", null));
    var token = store.CapturedMessages().Single(item => item.Purpose == "EmailVerification").Token;

    var first = store.Verify(new TokenProofRequest(token));
    var second = store.Verify(new TokenProofRequest(token));

    AssertEx.True(first.Succeeded, "First verification should succeed");
    AssertEx.False(second.Succeeded, "Second verification should be rejected");
    AssertEx.Equal("TokenUnavailable", second.Code, "Replay should produce TokenUnavailable");
});

return runner.Run();
