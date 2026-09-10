import { FormEvent, useEffect, useRef, useState } from 'react';
import {
  BookmarkRecord,
  CalendarEventRecord,
  DocumentRecord,
  DocumentSummary,
  FinanceCategoryRecord,
  FinanceManualRecord,
  FinanceSummary,
  GoalDetail,
  GoalRecord,
  GoalTargetRecord,
  DashboardSnapshot,
  SearchPage,
  AdminUserAccess,
  AdminUserRecord,
  NotificationRecord,
  PreferenceRecord,
  ReadingItemRecord,
  TagRecord,
  ToolboxTool,
  ToolboxRunResult,
  TrashItemRecord,
  NexoraApiError,
  ProjectRecord,
  ProfilePatch,
  ProfileResponse,
  SessionProjection,
  SnippetRecord,
  TaskRecord,
  clearProfileRevision,
  confirmPasswordReset,
  createBookmark,
  createCalendarEvent,
  createDocument,
  createFinanceCategory,
  createFinanceRecord,
  createIdempotencyKey,
  createProject,
  createTask,
  deleteCalendarEvent,
  deleteProject,
  deleteTask,
  deleteNotifications,
  disableAdminUser,
  getCsrf,
  getMe,
  getDashboard,
  searchResources,
  getDocument,
  getGoal,
  getAdminUserAccess,
  listCalendarEvents,
  listDocuments,
  listFinanceCategories,
  listFinanceRecords,
  listNotifications,
  listPreferences,
  listAdminUsers,
  listBookmarks,
  listReadingQueue,
  listProjects,
  listSessions,
  listSnippets,
  listTasks,
  login,
  markAllNotificationsRead,
  markNotificationRead,
  listTrash,
  purgeTrashBatch,
  restoreTrashBatch,
  setAdminModuleGrant,
  setAdminActionGrant,
  setAdminUserRole,
  saveDocument,
  saveSnippet,
  transitionDocument,
  updatePreference,
  logout,
  registerUser,
  requestPasswordReset,
  resendVerification,
  revokeAllSessions,
  revokeSession,
  updateCalendarEvent,
  updateMe,
  updateProject,
  updateTask,
  verifyEmail,
  transitionProject,
  transitionCalendarEvent,
  transitionBookmark,
  transitionSnippet,
  removeFinanceCategory,
  updateFinanceCategory,
  updateFinanceRecord,
  updateBookmark,
  createSnippet,
  saveReadingItem,
  removeReadingItem,
  updateReadingItem,
  listOrganizationTags,
  createOrganizationTag,
  renameOrganizationTag,
  removeOrganizationTag,
  listDeveloperTools,
  runDeveloperTool,
  listGoals,
  createGoal,
  updateGoal,
  recordGoalProgress,
  transitionGoal
} from './api';

type Screen = 'home' | 'search' | 'login' | 'register' | 'verify' | 'forgot' | 'reset' | 'profile' | 'security' | 'notifications' | 'trash' | 'admin' | 'finance' | 'bookmarks' | 'snippets' | 'readLater' | 'tags' | 'tools' | 'goals' | 'module';
type LocationState = { screen: Screen; moduleCode?: string };
type SessionState = 'checking' | 'anonymous' | 'authenticated' | 'unavailable';
type NoticeKind = 'info' | 'success' | 'error';

const PUBLIC_SCREENS = new Set<Screen>(['login', 'register', 'verify', 'forgot', 'reset']);
const DEFAULT_TIME_ZONE = 'UTC';

function routeFromPath(pathname: string): LocationState {
  const path = pathname.replace(/\/+$/, '') || '/';
  switch (path) {
    case '/register':
      return { screen: 'register' };
    case '/verify-email':
      return { screen: 'verify' };
    case '/password/forgot':
      return { screen: 'forgot' };
    case '/password/reset':
      return { screen: 'reset' };
    case '/settings/profile':
      return { screen: 'profile' };
    case '/settings/security':
      return { screen: 'security' };
    case '/search':
      return { screen: 'search' };
    case '/notifications':
      return { screen: 'notifications' };
    case '/trash':
      return { screen: 'trash' };
    case '/admin/access':
      return { screen: 'admin' };
    case '/finance':
      return { screen: 'finance' };
    case '/bookmarks':
      return { screen: 'bookmarks' };
    case '/snippets':
      return { screen: 'snippets' };
    case '/read-later':
      return { screen: 'readLater' };
    case '/organize/tags':
      return { screen: 'tags' };
    case '/developer/tools':
      return { screen: 'tools' };
    case '/goals':
      return { screen: 'goals' };
    case '/login':
      return { screen: 'login' };
    case '/':
      return { screen: 'home' };
    default:
      if (path.startsWith('/modules/')) {
        return { screen: 'module', moduleCode: decodeURIComponent(path.slice('/modules/'.length)).toUpperCase() };
      }
      return { screen: 'home' };
  }
}

function pathForLocation(location: LocationState): string {
  switch (location.screen) {
    case 'register':
      return '/register';
    case 'verify':
      return '/verify-email';
    case 'forgot':
      return '/password/forgot';
    case 'reset':
      return '/password/reset';
    case 'profile':
      return '/settings/profile';
    case 'security':
      return '/settings/security';
    case 'search':
      return '/search';
    case 'notifications':
      return '/notifications';
    case 'trash':
      return '/trash';
    case 'admin':
      return '/admin/access';
    case 'finance':
      return '/finance';
    case 'bookmarks':
      return '/bookmarks';
    case 'snippets':
      return '/snippets';
    case 'readLater':
      return '/read-later';
    case 'tags':
      return '/organize/tags';
    case 'tools':
      return '/developer/tools';
    case 'goals':
      return '/goals';
    case 'login':
      return '/login';
    case 'module':
      return `/modules/${encodeURIComponent(location.moduleCode ?? '')}`;
    default:
      return '/';
  }
}

function normalizeProfile(profile: ProfileResponse): ProfileResponse {
  return {
    ...profile,
    role: profile.role || 'User',
    modules: Array.isArray(profile.modules) ? profile.modules : []
  };
}

function currentTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || DEFAULT_TIME_ZONE;
  } catch {
    return DEFAULT_TIME_ZONE;
  }
}

function asApiError(error: unknown): NexoraApiError {
  if (error instanceof NexoraApiError) {
    return error;
  }
  if (error instanceof Error) {
    return new NexoraApiError(error.message, 0, 'ClientError');
  }
  return new NexoraApiError('Yêu cầu không thành công.', 0, 'UnknownError');
}

function firstFieldError(error: NexoraApiError, field: string): string | undefined {
  const match = Object.entries(error.fieldErrors).find(([key]) => key.toLowerCase() === field.toLowerCase());
  return match?.[1][0];
}

function passwordError(password: string): string | undefined {
  const length = Array.from(password).length;
  if (length < 15) {
    return 'Mật khẩu cần ít nhất 15 ký tự Unicode.';
  }
  if (length > 128) {
    return 'Mật khẩu không được vượt quá 128 ký tự Unicode.';
  }
  return undefined;
}

function dateTime(value: string): string {
  try {
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
  } catch {
    return 'Không xác định';
  }
}

function Notice({ kind, children, onDismiss }: { kind: NoticeKind; children: React.ReactNode; onDismiss?: () => void }) {
  return (
    <div className={`notice notice-${kind}`} role={kind === 'error' ? 'alert' : 'status'} aria-live="polite">
      <span>{children}</span>
      {onDismiss && (
        <button className="icon-button" type="button" aria-label="Đóng thông báo" onClick={onDismiss}>
          ×
        </button>
      )}
    </div>
  );
}

function FieldError({ id, message }: { id: string; message?: string }) {
  if (!message) {
    return null;
  }
  return (
    <p className="field-error" id={id}>
      {message}
    </p>
  );
}

function SubmitButton({ busy, children }: { busy: boolean; children: React.ReactNode }) {
  return (
    <button className="primary-button" type="submit" disabled={busy}>
      {busy ? 'Đang xử lý…' : children}
    </button>
  );
}

function PublicFrame({
  children,
  navigate,
  notice,
  onDismissNotice
}: {
  children: React.ReactNode;
  navigate: (screen: Screen) => void;
  notice?: { kind: NoticeKind; text: string };
  onDismissNotice?: () => void;
}) {
  return (
    <div className="public-layout">
      <header className="public-header">
        <button className="brand" type="button" onClick={() => navigate('login')} aria-label="Nexora — tới trang đăng nhập">
          <span className="brand-mark" aria-hidden="true">N</span>
          <span>Nexora</span>
        </button>
        <span className="environment-label">Local application</span>
      </header>
      <main className="public-main">
        {notice && <Notice kind={notice.kind} onDismiss={onDismissNotice}>{notice.text}</Notice>}
        {children}
      </main>
      <footer className="public-footer">Phiên xác thực do server quản lý bằng cookie HttpOnly; trình duyệt không lưu token đăng nhập.</footer>
    </div>
  );
}

function AuthLinks({ navigate, current }: { navigate: (screen: Screen) => void; current: Screen }) {
  return (
    <nav className="auth-links" aria-label="Điều hướng tài khoản">
      {current !== 'login' && <button className="link-button" type="button" onClick={() => navigate('login')}>Đăng nhập</button>}
      {current !== 'register' && <button className="link-button" type="button" onClick={() => navigate('register')}>Tạo tài khoản</button>}
      {current !== 'forgot' && current !== 'reset' && <button className="link-button" type="button" onClick={() => navigate('forgot')}>Quên mật khẩu?</button>}
    </nav>
  );
}

function FormCard({ title, description, children }: { title: string; description: string; children: React.ReactNode }) {
  return (
    <section className="form-card" aria-labelledby="form-title">
      <p className="eyebrow">NEXORA ACCOUNT</p>
      <h1 id="form-title">{title}</h1>
      <p className="lead">{description}</p>
      {children}
    </section>
  );
}

function RegisterScreen({
  navigate,
  onRegistered,
  notice,
  onDismissNotice
}: {
  navigate: (screen: Screen) => void;
  onRegistered: (email: string) => void;
  notice?: { kind: NoticeKind; text: string };
  onDismissNotice?: () => void;
}) {
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [timeZoneId, setTimeZoneId] = useState(currentTimeZone);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [clientError, setClientError] = useState<string>();
  const requestKey = useRef<string | null>(null);

  function resetRequestKey() {
    requestKey.current = null;
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setClientError(undefined);
    const trimmedEmail = email.trim();
    if (!trimmedEmail || !trimmedEmail.includes('@')) {
      setClientError('Nhập một địa chỉ email hợp lệ.');
      return;
    }
    const passwordIssue = passwordError(password);
    if (passwordIssue) {
      setClientError(passwordIssue);
      return;
    }
    if (!timeZoneId.trim()) {
      setClientError('Timezone IANA là bắt buộc.');
      return;
    }
    if (displayName.trim().length > 100) {
      setClientError('Tên hiển thị không được vượt quá 100 ký tự.');
      return;
    }

    requestKey.current ??= createIdempotencyKey();
    setBusy(true);
    try {
      await registerUser(trimmedEmail, password, timeZoneId.trim(), displayName.trim(), requestKey.current);
      requestKey.current = null;
      onRegistered(trimmedEmail);
    } catch (requestError) {
      setError(asApiError(requestError));
    } finally {
      setBusy(false);
    }
  }

  return (
    <PublicFrame navigate={navigate} notice={notice} onDismissNotice={onDismissNotice}>
      <FormCard title="Tạo tài khoản" description="Đăng ký tài khoản cá nhân. Bạn cần xác minh email trước khi dùng dữ liệu và module riêng của mình.">
        <form className="stack-form" onSubmit={submit} noValidate>
          <div className="field-group">
            <label htmlFor="register-email">Email</label>
            <input id="register-email" type="email" autoComplete="email" value={email} onChange={(event) => { resetRequestKey(); setEmail(event.target.value); }} required aria-describedby="register-email-help register-email-error" />
            <p className="field-help" id="register-email-help">Địa chỉ này được dùng để gửi hướng dẫn xác minh.</p>
            <FieldError id="register-email-error" message={error ? firstFieldError(error, 'email') : undefined} />
          </div>
          <div className="field-group">
            <label htmlFor="register-display-name">Tên hiển thị <span className="optional">(tùy chọn)</span></label>
            <input id="register-display-name" type="text" autoComplete="name" maxLength={100} value={displayName} onChange={(event) => { resetRequestKey(); setDisplayName(event.target.value); }} aria-describedby="register-display-name-error" />
            <FieldError id="register-display-name-error" message={error ? firstFieldError(error, 'displayName') : undefined} />
          </div>
          <div className="field-group">
            <label htmlFor="register-password">Mật khẩu</label>
            <input id="register-password" type="password" autoComplete="new-password" minLength={15} maxLength={128} value={password} onChange={(event) => { resetRequestKey(); setPassword(event.target.value); }} aria-describedby="register-password-help register-password-error" required />
            <p className="field-help" id="register-password-help">Từ 15 đến 128 ký tự Unicode. Không dùng mật khẩu phổ biến.</p>
            <FieldError id="register-password-error" message={error ? firstFieldError(error, 'password') : undefined} />
          </div>
          <div className="field-group">
            <label htmlFor="register-timezone">Timezone IANA</label>
            <input id="register-timezone" type="text" autoComplete="off" value={timeZoneId} onChange={(event) => { resetRequestKey(); setTimeZoneId(event.target.value); }} aria-describedby="register-timezone-help register-timezone-error" required />
            <p className="field-help" id="register-timezone-help">Phát hiện từ trình duyệt; bạn có thể sửa, ví dụ <code>Asia/Ho_Chi_Minh</code>.</p>
            <FieldError id="register-timezone-error" message={error ? firstFieldError(error, 'timeZoneId') : undefined} />
          </div>
          {(clientError || error) && <Notice kind="error">{clientError ?? error?.message ?? 'Không thể tạo tài khoản.'}{error?.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
          <SubmitButton busy={busy}>Tạo tài khoản</SubmitButton>
        </form>
        <AuthLinks navigate={navigate} current="register" />
      </FormCard>
    </PublicFrame>
  );
}

function VerifyScreen({
  email,
  setEmail,
  navigate,
  onVerified,
  notice,
  onDismissNotice
}: {
  email: string;
  setEmail: (email: string) => void;
  navigate: (screen: Screen) => void;
  onVerified: () => void;
  notice?: { kind: NoticeKind; text: string };
  onDismissNotice?: () => void;
}) {
  const [token, setToken] = useState('');
  const [busy, setBusy] = useState(false);
  const [resendBusy, setResendBusy] = useState(false);
  const [verified, setVerified] = useState(false);
  const [resendUntil, setResendUntil] = useState(0);
  const [clock, setClock] = useState(() => Date.now());
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [resendError, setResendError] = useState<NexoraApiError | null>(null);
  const requestKey = useRef<string | null>(null);
  const resendKey = useRef<string | null>(null);
  const secondsRemaining = Math.max(0, Math.ceil((resendUntil - clock) / 1000));

  useEffect(() => {
    if (resendUntil <= Date.now()) {
      return undefined;
    }
    const timer = window.setInterval(() => setClock(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [resendUntil]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    if (!token.trim()) {
      setError(new NexoraApiError('Mã xác minh là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy(true);
    try {
      await verifyEmail(token.trim(), requestKey.current);
      requestKey.current = null;
      setVerified(true);
      onVerified();
    } catch (requestError) {
      setError(asApiError(requestError));
    } finally {
      setBusy(false);
    }
  }

  async function resend() {
    setResendError(null);
    if (!email.trim()) {
      setResendError(new NexoraApiError('Email là bắt buộc để gửi lại mã.', 422, 'ValidationFailed'));
      return;
    }
    resendKey.current ??= createIdempotencyKey();
    setResendBusy(true);
    try {
      await resendVerification(email.trim(), resendKey.current);
      resendKey.current = null;
      setResendUntil(Date.now() + 60_000);
      setClock(Date.now());
    } catch (requestError) {
      setResendError(asApiError(requestError));
    } finally {
      setResendBusy(false);
    }
  }

  return (
    <PublicFrame navigate={navigate} notice={notice} onDismissNotice={onDismissNotice}>
      <FormCard title="Xác minh email" description="Nhập mã từ kênh email/transport đã cấu hình. Nexora không hiển thị mailbox không được bảo vệ trong trình duyệt.">
        {verified ? (
          <div className="success-panel">
            <h2>Email đã được xác minh</h2>
            <p>PersonalSpace sẽ được tạo theo transaction xác minh. Hãy đăng nhập để tiếp tục.</p>
            <button className="primary-button" type="button" onClick={() => navigate('login')}>Tới đăng nhập</button>
          </div>
        ) : (
          <>
            <form className="stack-form" onSubmit={submit} noValidate>
              <div className="field-group">
                <label htmlFor="verify-email">Email</label>
                <input id="verify-email" type="email" autoComplete="email" value={email} onChange={(event) => { setEmail(event.target.value); requestKey.current = null; }} required />
              </div>
              <div className="field-group">
                <label htmlFor="verify-token">Mã xác minh</label>
                <input id="verify-token" type="text" inputMode="text" autoComplete="one-time-code" value={token} onChange={(event) => { setToken(event.target.value); requestKey.current = null; }} required aria-describedby="verify-token-help" />
                <p className="field-help" id="verify-token-help">Chỉ dán mã vào trường này; mã không được lưu vào storage của trình duyệt.</p>
              </div>
              {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
              <SubmitButton busy={busy}>Xác minh email</SubmitButton>
            </form>
            <div className="secondary-action">
              <button className="secondary-button" type="button" disabled={resendBusy || secondsRemaining > 0} onClick={resend}>
                {resendBusy ? 'Đang gửi…' : secondsRemaining > 0 ? `Gửi lại sau ${secondsRemaining}s` : 'Gửi lại email xác minh'}
              </button>
              {resendError && <Notice kind="error">{resendError.message}</Notice>}
            </div>
          </>
        )}
        <div className="auth-links">
          <button className="link-button" type="button" onClick={() => navigate('register')}>Quay lại đăng ký</button>
          <button className="link-button" type="button" onClick={() => navigate('login')}>Đăng nhập</button>
        </div>
      </FormCard>
    </PublicFrame>
  );
}

function LoginScreen({
  navigate,
  onAuthenticated,
  onPendingVerification,
  notice,
  onDismissNotice
}: {
  navigate: (screen: Screen) => void;
  onAuthenticated: (profile: ProfileResponse) => void;
  onPendingVerification: (email: string) => void;
  notice?: { kind: NoticeKind; text: string };
  onDismissNotice?: () => void;
}) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const requestKey = useRef<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    if (!email.trim() || !password) {
      setError(new NexoraApiError('Email và mật khẩu là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy(true);
    try {
      const response = await login(email.trim(), password, requestKey.current);
      requestKey.current = null;
      onAuthenticated(normalizeProfile(response.profile));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.code?.toLowerCase().includes('verification')) {
        onPendingVerification(email.trim());
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <PublicFrame navigate={navigate} notice={notice} onDismissNotice={onDismissNotice}>
      <FormCard title="Đăng nhập" description="Đăng nhập vào PersonalSpace của bạn. Quyền và module luôn được server kiểm tra lại.">
        <form className="stack-form" onSubmit={submit} noValidate>
          <div className="field-group">
            <label htmlFor="login-email">Email</label>
            <input id="login-email" type="email" autoComplete="email" value={email} onChange={(event) => { requestKey.current = null; setEmail(event.target.value); }} required />
          </div>
          <div className="field-group">
            <label htmlFor="login-password">Mật khẩu</label>
            <input id="login-password" type="password" autoComplete="current-password" value={password} onChange={(event) => { requestKey.current = null; setPassword(event.target.value); }} required />
          </div>
          {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
          <SubmitButton busy={busy}>Đăng nhập</SubmitButton>
        </form>
        <AuthLinks navigate={navigate} current="login" />
      </FormCard>
    </PublicFrame>
  );
}

function ForgotPasswordScreen({
  navigate,
  notice,
  onDismissNotice
}: {
  navigate: (screen: Screen) => void;
  notice?: { kind: NoticeKind; text: string };
  onDismissNotice?: () => void;
}) {
  const [email, setEmail] = useState('');
  const [busy, setBusy] = useState(false);
  const [accepted, setAccepted] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const requestKey = useRef<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    if (!email.trim() || !email.includes('@')) {
      setError(new NexoraApiError('Nhập một địa chỉ email hợp lệ.', 422, 'ValidationFailed'));
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy(true);
    try {
      await requestPasswordReset(email.trim(), requestKey.current);
      requestKey.current = null;
      setAccepted(true);
    } catch (requestError) {
      setError(asApiError(requestError));
    } finally {
      setBusy(false);
    }
  }

  return (
    <PublicFrame navigate={navigate} notice={notice} onDismissNotice={onDismissNotice}>
      <FormCard title="Đặt lại mật khẩu" description="Nhập email để nhận hướng dẫn nếu tài khoản đủ điều kiện. Phản hồi luôn giống nhau để không tiết lộ account tồn tại.">
        {accepted ? (
          <div className="success-panel">
            <h2>Đã tiếp nhận yêu cầu</h2>
            <p>Nếu email đủ điều kiện, hãy dùng mã reset từ kênh được cấu hình. Không nhập mã vào URL hoặc lưu mã trong trình duyệt.</p>
            <button className="primary-button" type="button" onClick={() => navigate('reset')}>Nhập mã reset</button>
          </div>
        ) : (
          <form className="stack-form" onSubmit={submit} noValidate>
            <div className="field-group">
              <label htmlFor="forgot-email">Email</label>
              <input id="forgot-email" type="email" autoComplete="email" value={email} onChange={(event) => { requestKey.current = null; setEmail(event.target.value); }} required />
            </div>
            {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
            <SubmitButton busy={busy}>Gửi yêu cầu reset</SubmitButton>
          </form>
        )}
        <AuthLinks navigate={navigate} current="forgot" />
      </FormCard>
    </PublicFrame>
  );
}

function ResetPasswordScreen({
  navigate,
  notice,
  onDismissNotice
}: {
  navigate: (screen: Screen) => void;
  notice?: { kind: NoticeKind; text: string };
  onDismissNotice?: () => void;
}) {
  const [token, setToken] = useState('');
  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [clientError, setClientError] = useState<string>();
  const requestKey = useRef<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setClientError(undefined);
    const passwordIssue = passwordError(password);
    if (!token.trim()) {
      setClientError('Mã reset là bắt buộc.');
      return;
    }
    if (passwordIssue) {
      setClientError(passwordIssue);
      return;
    }
    if (password !== confirmation) {
      setClientError('Hai mật khẩu không khớp.');
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy(true);
    try {
      await confirmPasswordReset(token.trim(), password, requestKey.current);
      requestKey.current = null;
      navigate('login');
    } catch (requestError) {
      setError(asApiError(requestError));
    } finally {
      setBusy(false);
    }
  }

  return (
    <PublicFrame navigate={navigate} notice={notice} onDismissNotice={onDismissNotice}>
      <FormCard title="Xác nhận mật khẩu mới" description="Mã reset chỉ dùng một lần và không tự đăng nhập. Sau khi thành công, các session cũ bị thu hồi theo policy.">
        <form className="stack-form" onSubmit={submit} noValidate>
          <div className="field-group">
            <label htmlFor="reset-token">Mã reset</label>
            <input id="reset-token" type="text" autoComplete="one-time-code" value={token} onChange={(event) => { requestKey.current = null; setToken(event.target.value); }} required />
          </div>
          <div className="field-group">
            <label htmlFor="reset-password">Mật khẩu mới</label>
            <input id="reset-password" type="password" autoComplete="new-password" minLength={15} maxLength={128} value={password} onChange={(event) => { requestKey.current = null; setPassword(event.target.value); }} required />
            <p className="field-help">Từ 15 đến 128 ký tự Unicode.</p>
          </div>
          <div className="field-group">
            <label htmlFor="reset-confirmation">Nhập lại mật khẩu mới</label>
            <input id="reset-confirmation" type="password" autoComplete="new-password" minLength={15} maxLength={128} value={confirmation} onChange={(event) => { requestKey.current = null; setConfirmation(event.target.value); }} required />
          </div>
          {(clientError || error) && <Notice kind="error">{clientError ?? error?.message ?? 'Không thể reset mật khẩu.'}{error?.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
          <SubmitButton busy={busy}>Đặt mật khẩu mới</SubmitButton>
        </form>
        <AuthLinks navigate={navigate} current="reset" />
      </FormCard>
    </PublicFrame>
  );
}

function Shell({
  profile,
  location,
  navigate,
  onLogout,
  onProfileUpdated,
  onAuthLost,
  notice,
  onDismissNotice
}: {
  profile: ProfileResponse;
  location: LocationState;
  navigate: (screen: Screen, moduleCode?: string) => void;
  onLogout: () => Promise<void>;
  onProfileUpdated: (profile: ProfileResponse) => void;
  onAuthLost: () => Promise<void>;
  notice?: { kind: NoticeKind; text: string };
  onDismissNotice?: () => void;
}) {
  const [logoutBusy, setLogoutBusy] = useState(false);
  const selectedModule = profile.modules.find((module) => module.code.toUpperCase() === location.moduleCode);
  const canFinance = profile.modules.some((module) => module.code.toUpperCase() === 'FX27' && module.enabled);
  const canBookmarks = profile.modules.some((module) => module.code.toUpperCase() === 'FX21' && module.enabled);
  const canSnippets = profile.modules.some((module) => module.code.toUpperCase() === 'FX22' && module.enabled);
  const canReadLater = profile.modules.some((module) => module.code.toUpperCase() === 'FX23' && module.enabled);
  const canOrganization = profile.modules.some((module) => module.code.toUpperCase() === 'FX24' && module.enabled);
  const canToolbox = profile.modules.some((module) => module.code.toUpperCase() === 'FX32' && module.enabled);
  const canGoals = profile.modules.some((module) => module.code.toUpperCase() === 'FX16' && module.enabled);
  const canSearch = profile.modules.some((module) => module.code.toUpperCase() === 'FX25' && module.enabled);
  const navigableModules = profile.modules.filter((module) => !['FX16', 'FX25', 'FX27', 'FX21', 'FX22', 'FX23', 'FX24', 'FX32'].includes(module.code.toUpperCase()));

  async function signOut() {
    setLogoutBusy(true);
    try {
      await onLogout();
    } finally {
      setLogoutBusy(false);
    }
  }

  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Nexora navigation">
        <div className="sidebar-brand"><span className="brand-mark" aria-hidden="true">N</span><span>Nexora</span></div>
        <nav className="primary-nav" aria-label="Điều hướng chính">
          <button className={location.screen === 'home' ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'home' ? 'page' : undefined} onClick={() => navigate('home')}>⌂ <span>Home</span></button>
          {canSearch && <button className={location.screen === 'search' ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'search' ? 'page' : undefined} onClick={() => navigate('search')}>⌕ <span>Search</span></button>}
          {canFinance && <button className={location.screen === 'finance' || (location.screen === 'module' && location.moduleCode === 'FX27') ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'finance' || (location.screen === 'module' && location.moduleCode === 'FX27') ? 'page' : undefined} onClick={() => navigate('finance')}>₫ <span>Finance</span></button>}
          {canBookmarks && <button className={location.screen === 'bookmarks' || (location.screen === 'module' && location.moduleCode === 'FX21') ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'bookmarks' || (location.screen === 'module' && location.moduleCode === 'FX21') ? 'page' : undefined} onClick={() => navigate('bookmarks')}>🔖 <span>Bookmarks</span></button>}
          {canSnippets && <button className={location.screen === 'snippets' || (location.screen === 'module' && location.moduleCode === 'FX22') ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'snippets' || (location.screen === 'module' && location.moduleCode === 'FX22') ? 'page' : undefined} onClick={() => navigate('snippets')}>⌘ <span>Snippets</span></button>}
          {canReadLater && <button className={location.screen === 'readLater' || (location.screen === 'module' && location.moduleCode === 'FX23') ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'readLater' || (location.screen === 'module' && location.moduleCode === 'FX23') ? 'page' : undefined} onClick={() => navigate('readLater')}>▤ <span>Read Later</span></button>}
          {canOrganization && <button className={location.screen === 'tags' || (location.screen === 'module' && location.moduleCode === 'FX24') ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'tags' || (location.screen === 'module' && location.moduleCode === 'FX24') ? 'page' : undefined} onClick={() => navigate('tags')}># <span>Tags</span></button>}
          {canToolbox && <button className={location.screen === 'tools' || (location.screen === 'module' && location.moduleCode === 'FX32') ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'tools' || (location.screen === 'module' && location.moduleCode === 'FX32') ? 'page' : undefined} onClick={() => navigate('tools')}>⌘ <span>Developer tools</span></button>}
          {canGoals && <button className={location.screen === 'goals' || (location.screen === 'module' && location.moduleCode === 'FX16') ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'goals' || (location.screen === 'module' && location.moduleCode === 'FX16') ? 'page' : undefined} onClick={() => navigate('goals')}>◎ <span>Goals</span></button>}
          <button className={location.screen === 'notifications' ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'notifications' ? 'page' : undefined} onClick={() => navigate('notifications')}>✉ <span>Notifications</span></button>
          <button className={location.screen === 'trash' ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'trash' ? 'page' : undefined} onClick={() => navigate('trash')}>▱ <span>Trash</span></button>
          {profile.role === 'SuperAdmin' && <button className={location.screen === 'admin' ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'admin' ? 'page' : undefined} onClick={() => navigate('admin')}>♙ <span>Admin access</span></button>}
          <p className="nav-section-label">Modules</p>
          {profile.modules.length === 0 ? (
            <p className="nav-empty">Server chưa cấp module cho phiên này.</p>
          ) : navigableModules.length === 0 ? (
            <p className="nav-empty">Các module còn lại chưa được cấp cho phiên này.</p>
          ) : (
            navigableModules.map((module) => {
              const enabled = module.enabled;
              const active = location.screen === 'module' && location.moduleCode === module.code.toUpperCase();
              return (
                <button key={module.code} className={active ? 'nav-item active' : 'nav-item'} type="button" aria-current={active ? 'page' : undefined} disabled={!enabled} title={enabled ? undefined : module.unavailableReason ?? 'Module chưa khả dụng'} onClick={() => navigate('module', module.code.toUpperCase())}>
                  <span className="module-dot" aria-hidden="true">{enabled ? '●' : '○'}</span><span>{module.code}</span>
                </button>
              );
            })
          )}
        </nav>
        <nav className="utility-nav" aria-label="Cài đặt tài khoản">
          <button className={location.screen === 'profile' ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'profile' ? 'page' : undefined} onClick={() => navigate('profile')}>⚙ <span>Profile</span></button>
          <button className={location.screen === 'security' ? 'nav-item active' : 'nav-item'} type="button" aria-current={location.screen === 'security' ? 'page' : undefined} onClick={() => navigate('security')}>▣ <span>Security & sessions</span></button>
          <button className="nav-item logout-item" type="button" onClick={signOut} disabled={logoutBusy}>↪ <span>{logoutBusy ? 'Đang đăng xuất…' : 'Đăng xuất'}</span></button>
        </nav>
      </aside>
      <div className="shell-content">
        <header className="shell-header">
          <div>
            <p className="eyebrow">PERSONAL SPACE</p>
            <p className="signed-in">{profile.email}</p>
          </div>
          <button className="mobile-logout" type="button" onClick={signOut} disabled={logoutBusy}>{logoutBusy ? 'Đang đăng xuất…' : 'Đăng xuất'}</button>
        </header>
        <main className="shell-main">
          {notice && <Notice kind={notice.kind} onDismiss={onDismissNotice}>{notice.text}</Notice>}
          {location.screen === 'search' && <SearchScreen onAuthLost={onAuthLost} />}
          {location.screen === 'profile' && <ProfileScreen profile={profile} onProfileUpdated={onProfileUpdated} onAuthLost={onAuthLost} />}
          {location.screen === 'security' && <SecurityScreen onAuthLost={onAuthLost} />}
          {location.screen === 'notifications' && <NotificationsScreen onAuthLost={onAuthLost} />}
          {location.screen === 'trash' && <TrashScreen onAuthLost={onAuthLost} />}
          {location.screen === 'admin' && <AdminAccessScreen onAuthLost={onAuthLost} />}
          {location.screen === 'finance' && <FinanceScreen onAuthLost={onAuthLost} />}
          {location.screen === 'bookmarks' && <BookmarksScreen onAuthLost={onAuthLost} />}
          {location.screen === 'snippets' && <SnippetsScreen onAuthLost={onAuthLost} />}
          {location.screen === 'readLater' && <ReadLaterScreen onAuthLost={onAuthLost} />}
          {location.screen === 'tags' && <OrganizationTagsScreen onAuthLost={onAuthLost} />}
          {location.screen === 'tools' && <DeveloperToolsScreen onAuthLost={onAuthLost} />}
          {location.screen === 'goals' && <GoalsScreen onAuthLost={onAuthLost} />}
          {location.screen === 'module' && <ModuleScreen profile={profile} module={selectedModule} navigate={navigate} onAuthLost={onAuthLost} />}
          {location.screen === 'home' && <HomeScreen profile={profile} navigate={navigate} onAuthLost={onAuthLost} />}
        </main>
      </div>
    </div>
  );
}

function NotificationsScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<NotificationRecord[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const page = await listNotifications(unreadOnly, 100);
      setItems(page.items);
      setUnreadCount(page.unreadCount);
      setSelected(new Set());
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [unreadOnly]);

  async function toggleRead(item: NotificationRecord) {
    setBusy(item.id);
    setError(null);
    try {
      const updated = await markNotificationRead(item.id, item.etag, item.readAt === null);
      setItems((current) => current.map((candidate) => candidate.id === updated.id ? updated : candidate));
      setUnreadCount((current) => Math.max(0, current + (item.readAt === null ? -1 : 1)));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
      if (apiError.status === 412) await load();
    } finally {
      setBusy(null);
    }
  }

  async function markAll() {
    setBusy('all');
    setError(null);
    try {
      await markAllNotificationsRead();
      await load();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function removeSelected() {
    if (selected.size === 0) return;
    setBusy('delete');
    setError(null);
    try {
      await deleteNotifications(Array.from(selected));
      await load();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  function select(id: string, checked: boolean) {
    setSelected((current) => {
      const next = new Set(current);
      if (checked) next.add(id); else next.delete(id);
      return next;
    });
  }

  return (
    <section className="content-section" aria-labelledby="notifications-title">
      <div className="content-heading"><div><p className="eyebrow">FX06 / INBOX</p><h1 id="notifications-title">Notifications</h1><p className="lead">Thông báo thuộc PersonalSpace hiện tại; delivery state được hiển thị đúng theo SQL projection.</p></div><button className="secondary-button" type="button" onClick={load} disabled={loading}>Tải lại</button></div>
      <div className="toolbar"><label className="check-label"><input type="checkbox" checked={unreadOnly} onChange={(event) => setUnreadOnly(event.target.checked)} /> Chỉ chưa đọc ({unreadCount})</label><button className="secondary-button" type="button" onClick={markAll} disabled={busy !== null || unreadCount === 0}>Đánh dấu tất cả đã đọc</button><button className="danger-button" type="button" onClick={() => void removeSelected()} disabled={busy !== null || selected.size === 0}>Xóa mục đã chọn</button></div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {loading ? <div className="loading-state" role="status">Đang tải thông báo…</div> : items.length === 0 ? <div className="empty-state"><h2>Inbox trống</h2><p>Không có thông báo phù hợp trong projection hiện tại.</p></div> : <div className="notification-list">{items.map((item) => <article className={item.readAt ? 'notification-card read' : 'notification-card'} key={item.id}><label className="notification-select"><input type="checkbox" checked={selected.has(item.id)} onChange={(event) => select(item.id, event.target.checked)} aria-label={`Chọn ${item.title}`} /></label><div className="notification-content"><div className="notification-heading"><h2>{item.title}</h2><span className="state-pill">{item.readAt ? 'Đã đọc' : 'Chưa đọc'}</span></div><p>{item.body}</p><span className="muted">{item.kind} · {dateTime(item.createdAt)}</span><div className="delivery-summary">{item.deliveries.map((delivery) => <span key={delivery.channel} title={delivery.lastErrorCode ?? undefined}>{delivery.channel}: {delivery.state}</span>)}</div></div><button className="secondary-button" type="button" onClick={() => void toggleRead(item)} disabled={busy !== null}>{item.readAt ? 'Đánh dấu chưa đọc' : 'Đánh dấu đã đọc'}</button></article>)}</div>}
    </section>
  );
}

function TrashScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<TrashItemRecord[]>([]);
  const [confirmation, setConfirmation] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setItems((await listTrash(200)).items);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, []);

  async function restore(batchId: string) {
    setBusy(batchId);
    setError(null);
    try {
      await restoreTrashBatch(batchId);
      await load();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function purge(batchId: string) {
    if (confirmation[batchId] !== 'PURGE') return;
    setBusy(batchId);
    setError(null);
    try {
      await purgeTrashBatch(batchId, confirmation[batchId]);
      await load();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  const batches = Array.from(items.reduce((groups, item) => {
    const group = groups.get(item.deletionBatchId) ?? [];
    group.push(item);
    groups.set(item.deletionBatchId, group);
    return groups;
  }, new Map<string, TrashItemRecord[]>()).entries());

  return (
    <section className="content-section" aria-labelledby="trash-title">
      <div className="content-heading"><div><p className="eyebrow">FX08 / LIFECYCLE</p><h1 id="trash-title">Trash</h1><p className="lead">Restore theo deletion batch đã ghi trong SQL. Calendar event không vào Trash; thao tác xóa Calendar là Cancel.</p></div><button className="secondary-button" type="button" onClick={load} disabled={loading}>Tải lại</button></div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {loading ? <div className="loading-state" role="status">Đang tải Trash…</div> : batches.length === 0 ? <div className="empty-state"><h2>Trash trống</h2><p>Không có Project hoặc Task đang chờ restore/purge.</p></div> : <div className="notification-list">{batches.map(([batchId, batch]) => <article className="resource-card" key={batchId}><div><h2>Batch {batchId.slice(0, 8)}</h2><p>{batch.length} item · xóa lúc {dateTime(batch[0].deletedAt)}</p><ul className="trash-members">{batch.map((item) => <li key={item.id}>{item.resourceType} · {item.resourceId} · trạng thái trước: {item.priorStatus}</li>)}</ul></div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => void restore(batchId)} disabled={busy !== null}>Restore batch</button><label className="purge-confirm"><span className="sr-only">Nhập PURGE để xóa vĩnh viễn batch</span><input value={confirmation[batchId] ?? ''} onChange={(event) => setConfirmation((current) => ({ ...current, [batchId]: event.target.value }))} placeholder="Nhập PURGE" autoComplete="off" /><button className="danger-button" type="button" onClick={() => void purge(batchId)} disabled={busy !== null || confirmation[batchId] !== 'PURGE'}>Purge</button></label></div></article>)}</div>}
    </section>
  );
}

function DocumentsScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<DocumentSummary[]>([]);
  const [selected, setSelected] = useState<DocumentRecord | null>(null);
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [documentType, setDocumentType] = useState('Document');
  const [editorMode, setEditorMode] = useState('Markdown');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setItems((await listDocuments(undefined, 100)).items);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, []);

  function startNew() {
    setSelected(null);
    setTitle('');
    setBody('');
    setDocumentType('Document');
    setEditorMode('Markdown');
    setError(null);
  }

  async function openDocument(item: DocumentSummary) {
    setBusy(`open:${item.id}`);
    setError(null);
    try {
      const document = await getDocument(item.id);
      setSelected(document);
      setTitle(document.title);
      setBody(document.body);
      setDocumentType(document.documentType);
      setEditorMode(document.editorMode);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!title.trim()) {
      setError(new NexoraApiError('Tiêu đề là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    setBusy('save');
    setError(null);
    try {
      const saved = selected
        ? await saveDocument(selected.id, selected.etag, title.trim(), body)
        : await createDocument(title.trim(), documentType, editorMode, body);
      setSelected(saved);
      setTitle(saved.title);
      setBody(saved.body);
      setItems((current) => selected
        ? current.map((candidate) => candidate.id === saved.id ? saved : candidate)
        : [saved, ...current]);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
      if (apiError.status === 412 && selected) await openDocument(selected);
    } finally {
      setBusy(null);
    }
  }

  async function transition(status: string) {
    if (!selected) return;
    setBusy('transition');
    setError(null);
    try {
      const saved = await transitionDocument(selected.id, selected.etag, status);
      setSelected(saved);
      setItems((current) => current.map((candidate) => candidate.id === saved.id ? saved : candidate));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
      if (apiError.status === 412) await openDocument(selected);
    } finally {
      setBusy(null);
    }
  }

  return (
    <section className="content-section" aria-labelledby="documents-title">
      <div className="content-heading"><div><p className="eyebrow">FX20 / KNOWLEDGE</p><h1 id="documents-title">Documents</h1><p className="lead">Page thuộc PersonalSpace hiện tại; Save tạo version SQL mới và body chỉ xuất hiện ở detail của owner.</p></div><button className="secondary-button" type="button" onClick={load} disabled={loading}>Tải lại</button></div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      <div className="resource-layout">
        <form className="form-panel resource-form" onSubmit={save} noValidate>
          <div className="section-heading"><h2>{selected ? 'Sửa page' : 'Tạo page'}</h2>{selected && <button className="link-button" type="button" onClick={startNew}>Tạo mới</button>}</div>
          <div className="field-group"><label htmlFor="document-title">Tiêu đề</label><input id="document-title" value={title} maxLength={200} onChange={(event) => setTitle(event.target.value)} required /></div>
          <div className="form-grid"><div className="field-group"><label htmlFor="document-type">Document type</label><select id="document-type" value={documentType} onChange={(event) => setDocumentType(event.target.value)} disabled={selected !== null}><option>Document</option><option>Note</option><option>Knowledge</option></select></div><div className="field-group"><label htmlFor="document-editor">Editor mode</label><select id="document-editor" value={editorMode} onChange={(event) => setEditorMode(event.target.value)} disabled={selected !== null}><option>Markdown</option><option>Block</option></select></div></div>
          <div className="field-group"><label htmlFor="document-body">Body <span className="optional">(tối đa 1 MiB)</span></label><textarea id="document-body" value={body} onChange={(event) => setBody(event.target.value)} rows={12} maxLength={1048576} disabled={selected?.status === 'Archived'} /></div>
          <div className="form-actions"><button className="secondary-button" type="button" onClick={startNew} disabled={busy !== null}>Làm mới</button><SubmitButton busy={busy === 'save'}>{selected ? 'Save version' : 'Tạo page'}</SubmitButton></div>
          {selected && <div className="form-actions"><button className="secondary-button" type="button" onClick={() => void transition(selected.status === 'Draft' ? 'Published' : selected.status === 'Published' ? 'Archived' : selected.preArchiveStatus ?? 'Draft')} disabled={busy !== null}>{selected.status === 'Draft' ? 'Publish' : selected.status === 'Published' ? 'Archive' : 'Unarchive'}</button><span className="muted">{selected.status} · version {selected.versionNumber} · {selected.etag}</span></div>}
        </form>
        <div className="resource-list"><div className="section-heading"><h2>Page của bạn</h2><span className="muted">{items.length} bản ghi</span></div>{loading ? <div className="loading-state" role="status">Đang tải Documents…</div> : items.length === 0 ? <div className="empty-state"><h3>Chưa có page</h3><p>Tạo Document, Note hoặc Knowledge page đầu tiên.</p></div> : <div className="resource-cards">{items.map((item) => <article className={selected?.id === item.id ? 'resource-card selected' : 'resource-card'} key={item.id}><div><h3>{item.title}</h3><p>{item.documentType} · {item.editorMode} · version {item.versionNumber}</p><span className="muted">{item.status} · cập nhật {dateTime(item.updatedAt)}</span></div><button className="secondary-button" type="button" onClick={() => void openDocument(item)} disabled={busy !== null}>Mở</button></article>)}</div>}</div>
      </div>
    </section>
  );
}

function AdminAccessScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [users, setUsers] = useState<AdminUserRecord[]>([]);
  const [selectedId, setSelectedId] = useState<string>('');
  const [access, setAccess] = useState<AdminUserAccess | null>(null);
  const [role, setRole] = useState('User');
  const [actionKey, setActionKey] = useState('');
  const [effect, setEffect] = useState('Allow');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);

  async function loadUsers() {
    setLoading(true);
    setError(null);
    try {
      const page = await listAdminUsers();
      setUsers(page.items);
      if (selectedId && page.items.some((user) => user.id === selectedId)) return;
      if (page.items[0]) {
        setSelectedId(page.items[0].id);
        await selectUser(page.items[0]);
      } else {
        setSelectedId('');
        setAccess(null);
      }
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  async function selectUser(user: AdminUserRecord) {
    setSelectedId(user.id);
    setBusy(`load:${user.id}`);
    setError(null);
    try {
      const loaded = await getAdminUserAccess(user.id);
      setAccess(loaded);
      setRole(loaded.user.role);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  useEffect(() => { void loadUsers(); }, []);

  async function saveRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!access) return;
    setBusy('role');
    setError(null);
    try {
      const updated = await setAdminUserRole(access.user.id, access.user.etag, role);
      setAccess(updated);
      setUsers((current) => current.map((user) => user.id === updated.user.id ? updated.user : user));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
      if (apiError.status === 412) await selectUser(access.user);
    } finally {
      setBusy(null);
    }
  }

  async function saveGrant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!access || !actionKey.trim()) return;
    setBusy('permission');
    setError(null);
    try {
      const updated = await setAdminActionGrant(access.user.id, access.user.etag, actionKey.trim(), effect);
      setAccess(updated);
      setUsers((current) => current.map((user) => user.id === updated.user.id ? updated.user : user));
      setActionKey('');
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function toggleModule(code: string, enabled: boolean) {
    if (!access) return;
    setBusy(`module:${code}`);
    setError(null);
    try {
      const updated = await setAdminModuleGrant(access.user.id, access.user.etag, code, enabled);
      setAccess(updated);
      setUsers((current) => current.map((user) => user.id === updated.user.id ? updated.user : user));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function disable() {
    if (!access || access.user.state === 'Disabled') return;
    setBusy('disable');
    setError(null);
    try {
      await disableAdminUser(access.user.id, access.user.etag);
      await loadUsers();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  return (
    <section className="content-section" aria-labelledby="admin-access-title">
      <div className="content-heading"><div><p className="eyebrow">FX02 / SUPERADMIN</p><h1 id="admin-access-title">Admin access</h1><p className="lead">Operational account metadata, roles, action grants and module enablement. Business-resource payloads không xuất hiện trong projection này.</p></div><button className="secondary-button" type="button" onClick={loadUsers} disabled={loading}>Tải lại users</button></div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {loading ? <div className="loading-state" role="status">Đang tải users…</div> : users.length === 0 ? <div className="empty-state"><h2>Chưa có account</h2><p>SQL chưa trả operational user projection.</p></div> : <div className="admin-layout"><div className="admin-user-list"><h2>Users</h2>{users.map((user) => <button key={user.id} type="button" className={selectedId === user.id ? 'admin-user-row active' : 'admin-user-row'} onClick={() => void selectUser(user)} disabled={busy !== null}><strong>{user.displayName}</strong><span>{user.email}</span><span>{user.role} · {user.state}</span></button>)}</div><div className="admin-detail">{!access ? <div className="empty-state"><h2>Chọn user</h2></div> : <><div className="section-heading"><div><h2>{access.user.displayName}</h2><p className="muted">{access.user.email} · {access.user.state} · ETag {access.user.etag}</p></div><button className="danger-button" type="button" onClick={() => void disable()} disabled={busy !== null || access.user.state === 'Disabled'}>Disable user</button></div><form className="form-panel" onSubmit={saveRole}><div className="field-group"><label htmlFor="admin-role">Role</label><select id="admin-role" value={role} onChange={(event) => setRole(event.target.value)}><option>User</option><option>Admin</option><option>SuperAdmin</option></select></div><button className="primary-button" type="submit" disabled={busy !== null}>Lưu role</button></form><form className="form-panel" onSubmit={saveGrant}><div className="section-heading"><h3>Action grant</h3><span className="muted">Allow chỉ cho action đã có policy</span></div><div className="form-grid"><div className="field-group"><label htmlFor="admin-action">Action key</label><input id="admin-action" value={actionKey} onChange={(event) => setActionKey(event.target.value)} maxLength={160} required /></div><div className="field-group"><label htmlFor="admin-effect">Effect</label><select id="admin-effect" value={effect} onChange={(event) => setEffect(event.target.value)}><option>Allow</option><option>Deny</option></select></div></div><button className="secondary-button" type="submit" disabled={busy !== null}>Cập nhật grant</button></form><div className="form-panel"><div className="section-heading"><h3>Module grants</h3><span className="muted">Server rechecks state/dependency</span></div><div className="admin-module-list">{access.moduleGrants.map((grant) => <label key={grant.code} className="admin-module-row"><span><strong>{grant.code}</strong><small>{grant.state}{grant.systemEnabled ? '' : ' · system disabled'}</small></span><input type="checkbox" checked={grant.enabled} onChange={(event) => void toggleModule(grant.code, event.target.checked)} disabled={busy !== null || !grant.systemEnabled || grant.state !== 'Ready'} /></label>)}</div></div><div className="form-panel"><h3>Current action grants</h3>{access.actionGrants.length === 0 ? <p className="muted">No explicit grants.</p> : <ul className="grant-list">{access.actionGrants.map((grant) => <li key={grant.actionKey}><code>{grant.actionKey}</code><span>{grant.effect} · {grant.status}</span></li>)}</ul>}</div></>}
      </div></div>}
    </section>
  );
}

function SearchScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [query, setQuery] = useState('');
  const [resourceType, setResourceType] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [includeArchived, setIncludeArchived] = useState(false);
  const [page, setPage] = useState<SearchPage | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);

  async function run(event?: FormEvent) {
    event?.preventDefault();
    if (!query.trim()) {
      setPage(null);
      setError(null);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setPage(await searchResources(query, resourceType, from, to, includeArchived));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="content-section" aria-labelledby="search-title">
      <div className="content-heading"><div><p className="eyebrow">FX25 / DISCOVERY</p><h1 id="search-title">Global Search</h1><p className="lead">Tìm trong các nguồn đã được cấp quyền. Search không tạo authority mới và không trả Vault/secret payload.</p></div></div>
      <form className="form-panel" onSubmit={(event) => void run(event)}>
        <div className="form-grid">
          <div className="field-group"><label htmlFor="global-search-query">Query</label><input id="global-search-query" value={query} onChange={(event) => setQuery(event.target.value)} maxLength={500} placeholder="Tiêu đề, mô tả hoặc nội dung…" autoComplete="off" /></div>
          <div className="field-group"><label htmlFor="global-search-type">Resource type</label><select id="global-search-type" value={resourceType} onChange={(event) => setResourceType(event.target.value)}><option value="">All enabled sources</option><option value="Project">Projects</option><option value="Task">Tasks</option><option value="Event">Calendar events</option><option value="Document">Documents</option><option value="Bookmark">Bookmarks</option><option value="Snippet">Snippets</option><option value="Goal">Goals</option></select></div>
          <div className="field-group"><label htmlFor="global-search-from">Updated from</label><input id="global-search-from" type="date" value={from} onChange={(event) => setFrom(event.target.value)} /></div>
          <div className="field-group"><label htmlFor="global-search-to">Updated to</label><input id="global-search-to" type="date" value={to} onChange={(event) => setTo(event.target.value)} /></div>
        </div>
        <label className="check-row"><input type="checkbox" checked={includeArchived} onChange={(event) => setIncludeArchived(event.target.checked)} /> Include archived source records</label>
        <div className="form-actions"><button className="primary-button" type="submit" disabled={loading || !query.trim()}>{loading ? 'Đang tìm…' : 'Tìm kiếm'}</button></div>
      </form>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {page && <>
        {page.providers.some((provider) => provider.state === 'Degraded' || provider.state === 'Unavailable') && <Notice kind="info">Một số nguồn không khả dụng; kết quả hiện tại là partial và không đại diện cho toàn bộ dữ liệu.</Notice>}
        <div className="section-heading"><h2>Kết quả cho “{page.query}”</h2><span className="muted">{page.items.length} kết quả hiển thị</span></div>
        <div className="module-grid">
          {page.items.length === 0 ? <div className="empty-state"><h3>Không có kết quả</h3><p>Thử từ khóa khác hoặc xóa bộ lọc.</p></div> : page.items.map((item) => <article className="module-card" key={`${item.resourceType}-${item.id}`}><div className="module-card-heading"><h3>{item.title}</h3><span className="state-pill">{item.resourceType}</span></div><p>{item.snippet ?? 'Không có preview an toàn.'}</p><p className="muted">{item.status ?? 'Active'} · {new Date(item.updatedAt).toLocaleString()}</p><button className="secondary-button" type="button" onClick={() => window.location.assign(item.route)}>Mở nguồn</button></article>)}
        </div>
        <div className="form-panel"><div className="section-heading"><h3>Source status</h3><span className="muted">Current access rechecked at query time</span></div><ul className="grant-list">{page.providers.map((provider) => <li key={provider.resourceType}><span><strong>{provider.resourceType}</strong><small>{provider.sourceModule} · {provider.message ?? provider.state}</small></span><span>{provider.count}</span></li>)}</ul></div>
      </>}
      {!page && !loading && <div className="empty-state"><h3>Bắt đầu tìm kiếm</h3><p>Nhập query để tìm trong các nguồn local đã được server cấp quyền.</p></div>}
    </section>
  );
}

function HomeScreen({ profile, navigate, onAuthLost }: { profile: ProfileResponse; navigate: (screen: Screen, moduleCode?: string) => void; onAuthLost: () => Promise<void> }) {
  const enabledModules = profile.modules.filter((module) => module.enabled);
  const [dashboard, setDashboard] = useState<DashboardSnapshot | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<NexoraApiError | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    getDashboard().then((result) => {
      if (!cancelled) setDashboard(result);
    }).catch((requestError) => {
      if (cancelled) return;
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) void onAuthLost();
    }).finally(() => {
      if (!cancelled) setLoading(false);
    });
    return () => { cancelled = true; };
  }, [profile.id, onAuthLost]);

  return (
    <section className="content-section" aria-labelledby="home-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">HOME</p>
          <h1 id="home-title">Xin chào, {profile.displayName}</h1>
          <p className="lead">Đây là PersonalSpace riêng của bạn. Shell chỉ hiển thị các capability server đã cấp.</p>
        </div>
        <span className="state-pill state-active">{profile.state}</span>
      </div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {loading ? <div className="loading-state" role="status">Đang tải các widget…</div> : dashboard && <section className="module-section" aria-labelledby="dashboard-attention-title">
        <div className="section-heading"><div><h2 id="dashboard-attention-title">Attention</h2><p className="muted">Nguồn dữ liệu giữ nguyên owner và timezone của PersonalSpace.</p></div><span className="muted">{new Date(dashboard.generatedAt).toLocaleString()}</span></div>
        <div className="module-grid">
          {dashboard.widgets.map((widget) => (
            <article key={widget.id} className={widget.state === 'Unavailable' ? 'module-card unavailable' : 'module-card'}>
              <div className="module-card-heading"><h3>{widget.title}</h3><span className={widget.state === 'Ready' ? 'state-pill state-active' : 'state-pill'}>{widget.state}</span></div>
              <p>{widget.message ?? `${widget.count} item${widget.count === 1 ? '' : 's'}`}</p>
              {widget.items.length === 0 ? <p className="muted">Không có mục cần chú ý.</p> : <ul className="grant-list">{widget.items.map((item) => <li key={item.id}><span><strong>{item.title}</strong><small>{item.status ?? item.kind}{item.at ? ` · ${new Date(item.at).toLocaleString()}` : ''}</small></span></li>)}</ul>}
            </article>
          ))}
        </div>
      </section>}
      <div className="info-grid">
        <article className="info-card">
          <p className="card-label">Locale</p>
          <strong>{profile.locale === 'vi' ? 'Tiếng Việt' : 'English'}</strong>
          <span>{profile.timeZoneId}</span>
        </article>
        <article className="info-card">
          <p className="card-label">Module availability</p>
          <strong>{enabledModules.length} enabled</strong>
          <span>{profile.modules.length} server projections</span>
        </article>
      </div>
      <section className="module-section" aria-labelledby="available-modules-title">
        <div className="section-heading"><h2 id="available-modules-title">Module catalog</h2><span className="muted">No client-side authority</span></div>
        {profile.modules.length === 0 ? (
          <div className="empty-state"><h3>Chưa có module được cấp</h3><p>Server chưa trả projection module cho PersonalSpace này.</p></div>
        ) : (
          <div className="module-grid">
            {profile.modules.map((module) => (
              <article key={module.code} className={module.enabled ? 'module-card' : 'module-card unavailable'}>
                <div className="module-card-heading"><h3>{module.code}</h3><span className="state-pill">{module.enabled ? 'Available' : 'Unavailable'}</span></div>
                <p>{module.enabled ? 'Module đã được server bật cho phiên này.' : module.unavailableReason ?? 'Module chưa khả dụng trong policy hiện tại.'}</p>
                {module.enabled && <button className="secondary-button" type="button" onClick={() => navigate('module', module.code)}>Mở module</button>}
              </article>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}

type ProductivityModuleCode = 'FX11' | 'FX12' | 'FX13';
type ProjectDraft = { name: string; description: string; startAt: string; endAt: string; priority: string; tagsJson: string; notes: string };
type TaskDraft = { projectId: string; title: string; description: string; status: string; dueAt: string; startAt: string; endAt: string; priority: string; tagsJson: string; acceptanceCriteriaJson: string; reminderAt: string };
type EventDraft = { title: string; description: string; startAt: string; endAt: string; timeZoneId: string };

function localInputToIso(value: string): string | null {
  if (!value.trim()) return null;
  const parsed = new Date(value);
  return Number.isNaN(parsed.valueOf()) ? null : parsed.toISOString();
}

function isoToLocalInput(value: string | null): string {
  if (!value) return '';
  const parsed = new Date(value);
  if (Number.isNaN(parsed.valueOf())) return '';
  const pad = (part: number) => String(part).padStart(2, '0');
  return `${parsed.getFullYear()}-${pad(parsed.getMonth() + 1)}-${pad(parsed.getDate())}T${pad(parsed.getHours())}:${pad(parsed.getMinutes())}`;
}

function ProductivityScreen({
  profile,
  moduleCode,
  navigate,
  onAuthLost
}: {
  profile: ProfileResponse;
  moduleCode: ProductivityModuleCode;
  navigate: (screen: Screen, moduleCode?: string) => void;
  onAuthLost: () => Promise<void>;
}) {
  const [projects, setProjects] = useState<ProjectRecord[]>([]);
  const [tasks, setTasks] = useState<TaskRecord[]>([]);
  const [events, setEvents] = useState<CalendarEventRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [editingProject, setEditingProject] = useState<ProjectRecord | null>(null);
  const [editingTask, setEditingTask] = useState<TaskRecord | null>(null);
  const [editingEvent, setEditingEvent] = useState<CalendarEventRecord | null>(null);
  const [projectDraft, setProjectDraft] = useState<ProjectDraft>({ name: '', description: '', startAt: '', endAt: '', priority: 'P3', tagsJson: '[]', notes: '' });
  const [taskDraft, setTaskDraft] = useState<TaskDraft>({ projectId: '', title: '', description: '', status: 'NotStarted', dueAt: '', startAt: '', endAt: '', priority: 'P3', tagsJson: '[]', acceptanceCriteriaJson: '[]', reminderAt: '' });
  const [eventDraft, setEventDraft] = useState<EventDraft>({ title: '', description: '', startAt: '', endAt: '', timeZoneId: profile.timeZoneId });

  const canProjects = profile.modules.some((module) => module.code.toUpperCase() === 'FX11' && module.enabled);
  const canTasks = profile.modules.some((module) => module.code.toUpperCase() === 'FX12' && module.enabled);
  const canCalendar = profile.modules.some((module) => module.code.toUpperCase() === 'FX13' && module.enabled);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [projectPage, taskPage, eventPage] = await Promise.all([
        canProjects || canTasks ? listProjects() : Promise.resolve(null),
        canTasks ? listTasks() : Promise.resolve(null),
        canCalendar ? listCalendarEvents() : Promise.resolve(null)
      ]);
      if (projectPage) setProjects(Array.isArray(projectPage.items) ? projectPage.items : []);
      if (taskPage) setTasks(Array.isArray(taskPage.items) ? taskPage.items : []);
      if (eventPage) setEvents(Array.isArray(eventPage.items) ? eventPage.items : []);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [moduleCode]);

  function resetProject() {
    setEditingProject(null);
    setProjectDraft({ name: '', description: '', startAt: '', endAt: '', priority: 'P3', tagsJson: '[]', notes: '' });
  }

  function resetTask() {
    setEditingTask(null);
    setTaskDraft({ projectId: projects[0]?.id ?? '', title: '', description: '', status: 'NotStarted', dueAt: '', startAt: '', endAt: '', priority: 'P3', tagsJson: '[]', acceptanceCriteriaJson: '[]', reminderAt: '' });
  }

  function resetEvent() {
    setEditingEvent(null);
    setEventDraft({ title: '', description: '', startAt: '', endAt: '', timeZoneId: profile.timeZoneId });
  }

  function beginProjectEdit(project: ProjectRecord) {
    setEditingProject(project);
    setProjectDraft({ name: project.name, description: project.description ?? '', startAt: isoToLocalInput(project.startAt), endAt: isoToLocalInput(project.endAt), priority: project.priority, tagsJson: project.tagsJson, notes: project.notes ?? '' });
  }

  function beginTaskEdit(task: TaskRecord) {
    setEditingTask(task);
    setTaskDraft({ projectId: task.projectId, title: task.title, description: task.description ?? '', status: task.status, dueAt: isoToLocalInput(task.dueAt), startAt: isoToLocalInput(task.startAt), endAt: isoToLocalInput(task.endAt), priority: task.priority, tagsJson: task.tagsJson, acceptanceCriteriaJson: task.acceptanceCriteriaJson, reminderAt: isoToLocalInput(task.reminderAt) });
  }

  function beginEventEdit(event: CalendarEventRecord) {
    setEditingEvent(event);
    setEventDraft({ title: event.title, description: event.description ?? '', startAt: isoToLocalInput(event.startAt), endAt: isoToLocalInput(event.endAt), timeZoneId: event.timeZoneId });
  }

  async function saveProject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const startAt = localInputToIso(projectDraft.startAt);
    const endAt = localInputToIso(projectDraft.endAt);
    if (!projectDraft.name.trim() || !startAt || !endAt) {
      setError(new NexoraApiError('Tên Project là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    setBusy('project');
    setError(null);
    try {
      const saved = editingProject
        ? await updateProject(editingProject.id, editingProject.etag, projectDraft.name.trim(), projectDraft.description.trim() || null, startAt, endAt, projectDraft.priority, projectDraft.tagsJson || '[]', projectDraft.notes.trim() || null)
        : await createProject(projectDraft.name.trim(), projectDraft.description.trim() || null, startAt, endAt, projectDraft.priority, projectDraft.tagsJson || '[]', projectDraft.notes.trim() || null);
      setProjects((current) => editingProject ? current.map((item) => item.id === saved.id ? saved : item) : [saved, ...current]);
      resetProject();
      if (!editingProject && !taskDraft.projectId) setTaskDraft((current) => ({ ...current, projectId: saved.id }));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function removeProject(project: ProjectRecord) {
    if (!window.confirm(`Đưa Project “${project.name}” và các Task vào Trash?`)) return;
    setBusy(`project:${project.id}`);
    setError(null);
    try {
      await deleteProject(project.id, project.etag);
      setProjects((current) => current.filter((item) => item.id !== project.id));
      setTasks((current) => current.filter((item) => item.projectId !== project.id));
      if (editingProject?.id === project.id) resetProject();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function closeProject(project: ProjectRecord, status: 'Completed' | 'Skipped') {
    const reason = window.prompt(`Lý do chuyển Project sang ${status}:`, 'Hoàn tất theo kế hoạch');
    if (reason === null) return;
    setBusy(`project:${project.id}`);
    setError(null);
    try {
      const saved = await transitionProject(project.id, project.etag, status, reason.trim() || null);
      setProjects((current) => current.map((item) => item.id === saved.id ? saved : item));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function saveTask(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const dueAt = taskDraft.dueAt ? localInputToIso(taskDraft.dueAt) : null;
    const startAt = localInputToIso(taskDraft.startAt);
    const endAt = localInputToIso(taskDraft.endAt);
    const reminderAt = taskDraft.reminderAt ? localInputToIso(taskDraft.reminderAt) : null;
    if (!taskDraft.projectId || !taskDraft.title.trim() || !startAt || !endAt) {
      setError(new NexoraApiError('Project và tiêu đề Task là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    if ((taskDraft.dueAt && !dueAt) || (taskDraft.reminderAt && !reminderAt)) {
      setError(new NexoraApiError('Due date không hợp lệ.', 422, 'ValidationFailed'));
      return;
    }
    setBusy('task');
    setError(null);
    try {
      const saved = editingTask
        ? await updateTask(editingTask.id, editingTask.etag, taskDraft.projectId, taskDraft.title.trim(), taskDraft.description.trim() || null, taskDraft.status, dueAt, startAt, endAt, taskDraft.priority, taskDraft.tagsJson || '[]', taskDraft.acceptanceCriteriaJson || '[]', 0, reminderAt)
        : await createTask(taskDraft.projectId, taskDraft.title.trim(), taskDraft.description.trim() || null, taskDraft.status, dueAt, startAt, endAt, taskDraft.priority, taskDraft.tagsJson || '[]', taskDraft.acceptanceCriteriaJson || '[]', 0, reminderAt);
      setTasks((current) => editingTask ? current.map((item) => item.id === saved.id ? saved : item) : [saved, ...current]);
      resetTask();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function removeTask(task: TaskRecord) {
    if (!window.confirm(`Xóa Task “${task.title}”?`)) return;
    setBusy(`task:${task.id}`);
    setError(null);
    try {
      await deleteTask(task.id, task.etag);
      setTasks((current) => current.filter((item) => item.id !== task.id));
      if (editingTask?.id === task.id) resetTask();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function saveEvent(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const startAt = localInputToIso(eventDraft.startAt);
    const endAt = localInputToIso(eventDraft.endAt);
    if (!eventDraft.title.trim() || !startAt || !endAt) {
      setError(new NexoraApiError('Tiêu đề, thời gian bắt đầu và kết thúc là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    setBusy('event');
    setError(null);
    try {
      const saved = editingEvent
        ? await updateCalendarEvent(editingEvent.id, editingEvent.etag, eventDraft.title.trim(), eventDraft.description.trim() || null, startAt, endAt, eventDraft.timeZoneId.trim())
        : await createCalendarEvent(eventDraft.title.trim(), eventDraft.description.trim() || null, startAt, endAt, eventDraft.timeZoneId.trim());
      setEvents((current) => editingEvent ? current.map((item) => item.id === saved.id ? saved : item) : [saved, ...current]);
      resetEvent();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function removeEvent(item: CalendarEventRecord) {
    if (!window.confirm(`Xóa lịch “${item.title}”?`)) return;
    setBusy(`event:${item.id}`);
    setError(null);
    try {
      await deleteCalendarEvent(item.id, item.etag);
      setEvents((current) => current.filter((eventItem) => eventItem.id !== item.id));
      if (editingEvent?.id === item.id) resetEvent();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  const title = moduleCode === 'FX11' ? 'Projects' : moduleCode === 'FX12' ? 'Tasks' : 'Calendar';
  if (loading) {
    return <section className="content-section"><div className="loading-state" role="status">Đang tải {title}…</div></section>;
  }

  return (
    <section className="content-section" aria-labelledby="productivity-title">
      <div className="content-heading">
        <div><p className="eyebrow">{moduleCode} / PERSONAL PRODUCTIVITY</p><h1 id="productivity-title">{title}</h1><p className="lead">Dữ liệu được lọc theo PersonalSpace hiện tại; server giữ quyền sở hữu, lifecycle và revision.</p></div>
        <button className="secondary-button" type="button" onClick={() => void load()} disabled={busy !== null}>Tải lại</button>
      </div>
      <div className="module-tabs" role="tablist" aria-label="Productivity modules">
        {canProjects && <button className={moduleCode === 'FX11' ? 'tab-button active' : 'tab-button'} type="button" role="tab" aria-selected={moduleCode === 'FX11'} onClick={() => navigate('module', 'FX11')}>Projects</button>}
        {canTasks && <button className={moduleCode === 'FX12' ? 'tab-button active' : 'tab-button'} type="button" role="tab" aria-selected={moduleCode === 'FX12'} onClick={() => navigate('module', 'FX12')}>Tasks</button>}
        {canCalendar && <button className={moduleCode === 'FX13' ? 'tab-button active' : 'tab-button'} type="button" role="tab" aria-selected={moduleCode === 'FX13'} onClick={() => navigate('module', 'FX13')}>Calendar</button>}
      </div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}{error.status === 412 && ' Hãy tải lại revision rồi áp dụng lại thay đổi.'}</Notice>}

      {moduleCode === 'FX11' && canProjects && (
        <div className="resource-layout">
          <form className="form-panel resource-form" onSubmit={saveProject} noValidate>
            <div className="section-heading"><h2>{editingProject ? 'Sửa Project' : 'Tạo Project'}</h2>{editingProject && <button className="link-button" type="button" onClick={resetProject}>Hủy sửa</button>}</div>
            <div className="field-group"><label htmlFor="project-name">Tên Project</label><input id="project-name" value={projectDraft.name} maxLength={160} onChange={(event) => setProjectDraft({ ...projectDraft, name: event.target.value })} required /></div>
            <div className="field-group"><label htmlFor="project-description">Mô tả <span className="optional">(tùy chọn)</span></label><textarea id="project-description" value={projectDraft.description} maxLength={2000} onChange={(event) => setProjectDraft({ ...projectDraft, description: event.target.value })} rows={4} /></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="project-start">Bắt đầu</label><input id="project-start" type="datetime-local" value={projectDraft.startAt} onChange={(event) => setProjectDraft({ ...projectDraft, startAt: event.target.value })} required /></div><div className="field-group"><label htmlFor="project-end">Kết thúc</label><input id="project-end" type="datetime-local" value={projectDraft.endAt} onChange={(event) => setProjectDraft({ ...projectDraft, endAt: event.target.value })} required /></div></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="project-priority">Ưu tiên</label><select id="project-priority" value={projectDraft.priority} onChange={(event) => setProjectDraft({ ...projectDraft, priority: event.target.value })}><option>P0</option><option>P1</option><option>P2</option><option>P3</option></select></div><div className="field-group"><label htmlFor="project-tags">Tags JSON <span className="optional">(mảng)</span></label><input id="project-tags" value={projectDraft.tagsJson} onChange={(event) => setProjectDraft({ ...projectDraft, tagsJson: event.target.value })} /></div></div>
            <div className="form-actions"><button className="secondary-button" type="button" onClick={resetProject} disabled={busy === 'project'}>Làm mới</button><SubmitButton busy={busy === 'project'}>{editingProject ? 'Lưu Project' : 'Tạo Project'}</SubmitButton></div>
          </form>
          <div className="resource-list"><div className="section-heading"><h2>Projects của bạn</h2><span className="muted">{projects.length} bản ghi</span></div>{projects.length === 0 ? <div className="empty-state"><h3>Chưa có Project</h3><p>Tạo Project đầu tiên để bắt đầu gom Task.</p></div> : <div className="resource-cards">{projects.map((project) => <article className="resource-card" key={project.id}><div><h3>{project.name}</h3><p>{project.description || 'Không có mô tả.'}</p><span className="muted">{dateTime(project.startAt)} — {dateTime(project.endAt)} · {project.priority} · {project.status}</span></div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => beginProjectEdit(project)} disabled={busy !== null || project.status === 'Completed' || project.status === 'Skipped'}>Sửa</button>{(project.status === 'NotStarted' || project.status === 'InProgress') && <><button className="secondary-button" type="button" onClick={() => void closeProject(project, 'Completed')} disabled={busy !== null}>Hoàn tất</button><button className="secondary-button" type="button" onClick={() => void closeProject(project, 'Skipped')} disabled={busy !== null}>Bỏ qua</button></>}<button className="danger-button" type="button" onClick={() => void removeProject(project)} disabled={busy !== null}>Xóa</button></div></article>)}</div>}</div>
        </div>
      )}

      {moduleCode === 'FX12' && canTasks && (
        <div className="resource-layout">
          <form className="form-panel resource-form" onSubmit={saveTask} noValidate>
            <div className="section-heading"><h2>{editingTask ? 'Sửa Task' : 'Tạo Task'}</h2>{editingTask && <button className="link-button" type="button" onClick={resetTask}>Hủy sửa</button>}</div>
            <div className="field-group"><label htmlFor="task-project">Project</label><select id="task-project" value={taskDraft.projectId} onChange={(event) => setTaskDraft({ ...taskDraft, projectId: event.target.value })} disabled={projects.length === 0} required><option value="">{projects.length === 0 ? 'Tạo Project trước' : 'Chọn Project'}</option>{projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</select></div>
            <div className="field-group"><label htmlFor="task-title">Tiêu đề Task</label><input id="task-title" value={taskDraft.title} maxLength={240} onChange={(event) => setTaskDraft({ ...taskDraft, title: event.target.value })} required /></div>
            <div className="field-group"><label htmlFor="task-description">Mô tả <span className="optional">(tùy chọn)</span></label><textarea id="task-description" value={taskDraft.description} maxLength={4000} onChange={(event) => setTaskDraft({ ...taskDraft, description: event.target.value })} rows={3} /></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="task-status">Trạng thái</label><select id="task-status" value={taskDraft.status} onChange={(event) => setTaskDraft({ ...taskDraft, status: event.target.value })}><option value="NotStarted">Chưa bắt đầu</option><option value="InProgress">Đang làm</option><option value="Completed">Hoàn thành</option><option value="Skipped">Bỏ qua</option></select></div><div className="field-group"><label htmlFor="task-priority">Ưu tiên</label><select id="task-priority" value={taskDraft.priority} onChange={(event) => setTaskDraft({ ...taskDraft, priority: event.target.value })}><option>P0</option><option>P1</option><option>P2</option><option>P3</option></select></div></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="task-start">Bắt đầu</label><input id="task-start" type="datetime-local" value={taskDraft.startAt} onChange={(event) => setTaskDraft({ ...taskDraft, startAt: event.target.value })} required /></div><div className="field-group"><label htmlFor="task-end">Kết thúc</label><input id="task-end" type="datetime-local" value={taskDraft.endAt} onChange={(event) => setTaskDraft({ ...taskDraft, endAt: event.target.value })} required /></div></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="task-due">Hạn hoàn thành <span className="optional">(tùy chọn)</span></label><input id="task-due" type="datetime-local" value={taskDraft.dueAt} onChange={(event) => setTaskDraft({ ...taskDraft, dueAt: event.target.value })} /></div><div className="field-group"><label htmlFor="task-reminder">Nhắc lúc <span className="optional">(tùy chọn)</span></label><input id="task-reminder" type="datetime-local" value={taskDraft.reminderAt} onChange={(event) => setTaskDraft({ ...taskDraft, reminderAt: event.target.value })} /></div></div>
            {projects.length === 0 && <p className="field-help">Tasks yêu cầu Project cùng PersonalSpace. Mở tab Projects để tạo một Project.</p>}
            <div className="form-actions"><button className="secondary-button" type="button" onClick={resetTask} disabled={busy === 'task'}>Làm mới</button><SubmitButton busy={busy === 'task'}> {editingTask ? 'Lưu Task' : 'Tạo Task'} </SubmitButton></div>
          </form>
          <div className="resource-list"><div className="section-heading"><h2>Tasks của bạn</h2><span className="muted">{tasks.length} bản ghi</span></div>{tasks.length === 0 ? <div className="empty-state"><h3>Chưa có Task</h3><p>Tạo Task trong một Project đang hoạt động.</p></div> : <div className="resource-cards">{tasks.map((task) => <article className="resource-card" key={task.id}><div><h3>{task.title}</h3><p>{projects.find((project) => project.id === task.projectId)?.name ?? 'Project không còn trong projection'}</p><span className="state-pill">{task.status}</span>{task.dueAt && <span className="muted">Hạn {dateTime(task.dueAt)}</span>}</div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => beginTaskEdit(task)} disabled={busy !== null}>Sửa</button><button className="danger-button" type="button" onClick={() => void removeTask(task)} disabled={busy !== null}>Xóa</button></div></article>)}</div>}</div>
        </div>
      )}

      {moduleCode === 'FX13' && canCalendar && (
        <div className="resource-layout">
          <form className="form-panel resource-form" onSubmit={saveEvent} noValidate>
            <div className="section-heading"><h2>{editingEvent ? 'Sửa sự kiện' : 'Tạo sự kiện'}</h2>{editingEvent && <button className="link-button" type="button" onClick={resetEvent}>Hủy sửa</button>}</div>
            <div className="field-group"><label htmlFor="event-title">Tiêu đề</label><input id="event-title" value={eventDraft.title} maxLength={240} onChange={(event) => setEventDraft({ ...eventDraft, title: event.target.value })} required /></div>
            <div className="field-group"><label htmlFor="event-description">Mô tả <span className="optional">(tùy chọn)</span></label><textarea id="event-description" value={eventDraft.description} maxLength={4000} onChange={(event) => setEventDraft({ ...eventDraft, description: event.target.value })} rows={3} /></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="event-start">Bắt đầu</label><input id="event-start" type="datetime-local" value={eventDraft.startAt} onChange={(event) => setEventDraft({ ...eventDraft, startAt: event.target.value })} required /></div><div className="field-group"><label htmlFor="event-end">Kết thúc</label><input id="event-end" type="datetime-local" value={eventDraft.endAt} onChange={(event) => setEventDraft({ ...eventDraft, endAt: event.target.value })} required /></div></div>
            <div className="field-group"><label htmlFor="event-timezone">Timezone IANA</label><input id="event-timezone" value={eventDraft.timeZoneId} onChange={(event) => setEventDraft({ ...eventDraft, timeZoneId: event.target.value })} required /><p className="field-help">Server lưu instant UTC và giữ timezone hiển thị theo contract.</p></div>
            <div className="form-actions"><button className="secondary-button" type="button" onClick={resetEvent} disabled={busy === 'event'}>Làm mới</button><SubmitButton busy={busy === 'event'}>{editingEvent ? 'Lưu sự kiện' : 'Tạo sự kiện'}</SubmitButton></div>
          </form>
          <div className="resource-list"><div className="section-heading"><h2>Lịch của bạn</h2><span className="muted">{events.length} bản ghi</span></div>{events.length === 0 ? <div className="empty-state"><h3>Chưa có sự kiện</h3><p>Tạo lịch đầu tiên trong timezone của bạn.</p></div> : <div className="resource-cards">{events.map((item) => <article className="resource-card" key={item.id}><div><h3>{item.title}</h3><p>{dateTime(item.startAt)} — {dateTime(item.endAt)}</p><span className="muted">{item.timeZoneId} · {item.status}</span></div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => beginEventEdit(item)} disabled={busy !== null || item.status !== 'Scheduled'}>Sửa</button>{item.status === 'Scheduled' && <button className="secondary-button" type="button" onClick={() => void transitionCalendarEvent(item.id, item.etag, 'Completed').then((saved) => setEvents((current) => current.map((eventItem) => eventItem.id === saved.id ? saved : eventItem))).catch((requestError) => setError(asApiError(requestError)))} disabled={busy !== null}>Hoàn tất</button>}<button className="danger-button" type="button" onClick={() => void removeEvent(item)} disabled={busy !== null || item.status !== 'Scheduled'}>Hủy</button></div></article>)}</div>}</div>
        </div>
      )}
    </section>
  );
}

type FinanceRecordDraft = {
  categoryId: string;
  amount: string;
  currencyCode: string;
  occurredOn: string;
  note: string;
};

type FinanceFilters = {
  categoryId?: string;
  currencyCode?: string;
  from?: string;
  to?: string;
  query?: string;
};

function todayDateInput(): string {
  const now = new Date();
  const pad = (part: number) => String(part).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

type BookmarkDraft = {
  url: string;
  title: string;
  description: string;
};

function BookmarksScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<BookmarkRecord[]>([]);
  const [includeArchived, setIncludeArchived] = useState(false);
  const [query, setQuery] = useState('');
  const [queryDraft, setQueryDraft] = useState('');
  const [draft, setDraft] = useState<BookmarkDraft>({ url: '', title: '', description: '' });
  const [editing, setEditing] = useState<BookmarkRecord | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [conflict, setConflict] = useState(false);
  const requestKey = useRef<string | null>(null);

  async function load(nextQuery = query, nextIncludeArchived = includeArchived) {
    setLoading(true);
    setError(null);
    setConflict(false);
    try {
      const page = await listBookmarks(nextIncludeArchived, nextQuery, 100);
      setItems(Array.isArray(page.items) ? page.items : []);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [query, includeArchived]);

  function resetEditor() {
    setEditing(null);
    setDraft({ url: '', title: '', description: '' });
    requestKey.current = null;
    setConflict(false);
    setError(null);
  }

  function beginEdit(item: BookmarkRecord) {
    setEditing(item);
    setDraft({ url: item.url, title: item.title, description: item.description ?? '' });
    requestKey.current = null;
    setConflict(false);
    setError(null);
  }

  function showError(requestError: unknown) {
    const apiError = asApiError(requestError);
    setError(apiError);
    if (apiError.status === 401) void onAuthLost();
    return apiError;
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setConflict(false);
    const url = draft.url.trim();
    const title = draft.title.trim();
    const description = draft.description.trim() || null;
    if (!url || !/^https?:\/\//i.test(url)) {
      setError(new NexoraApiError('URL phải là địa chỉ HTTP(S) đầy đủ.', 422, 'ValidationFailed', null, { url: ['URL phải là địa chỉ HTTP(S) đầy đủ.'] }));
      return;
    }
    if (!title || title.length > 200) {
      setError(new NexoraApiError('Tiêu đề là bắt buộc và tối đa 200 ký tự.', 422, 'ValidationFailed', null, { title: ['Tiêu đề là bắt buộc và tối đa 200 ký tự.'] }));
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy('save');
    try {
      if (editing) {
        await updateBookmark(editing.id, editing.etag, url, title, description, requestKey.current);
      } else {
        await createBookmark(url, title, description, requestKey.current);
      }
      requestKey.current = null;
      resetEditor();
      await load();
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) {
        setConflict(true);
        await load();
      }
    } finally {
      setBusy(null);
    }
  }

  async function transition(item: BookmarkRecord) {
    const nextStatus = item.status === 'Archived' ? 'Active' : 'Archived';
    if (!window.confirm(`${nextStatus === 'Archived' ? 'Archive' : 'Unarchive'} bookmark “${item.title}”?`)) return;
    const key = createIdempotencyKey();
    setBusy(`transition:${item.id}`);
    setError(null);
    try {
      await transitionBookmark(item.id, item.etag, nextStatus, key);
      await load();
      if (editing?.id === item.id) resetEditor();
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) await load();
    } finally {
      setBusy(null);
    }
  }

  function applySearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setQuery(queryDraft.trim());
  }

  return (
    <section className="content-section" aria-labelledby="bookmarks-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">FX21 / KNOWLEDGE</p>
          <h1 id="bookmarks-title">Bookmarks</h1>
          <p className="lead">Lưu URL và metadata thủ công trong PersonalSpace hiện tại. URL chỉ là dữ liệu bất hoạt; Nexora không tự fetch, nhúng hoặc mở provider bên ngoài.</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void load()} disabled={loading || busy !== null}>{loading ? 'Đang tải…' : 'Tải lại'}</button>
      </div>
      {conflict && <Notice kind="error"><span>Bookmark đã thay đổi ở nơi khác. Draft hiện tại vẫn nằm trong memory; hãy tải revision mới rồi lưu lại.</span><button className="inline-button" type="button" onClick={() => void load()} disabled={loading}>Tải revision</button></Notice>}
      {error && !conflict && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      <div className="resource-layout">
        <div className="content-section">
          <form className="form-panel" onSubmit={save} noValidate>
            <div className="section-heading"><h2>{editing ? 'Sửa bookmark' : 'Bookmark mới'}</h2>{editing && <button className="link-button" type="button" onClick={resetEditor}>Hủy sửa</button>}</div>
            <div className="field-group"><label htmlFor="bookmark-url">URL</label><input id="bookmark-url" type="url" value={draft.url} maxLength={2048} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, url: event.target.value }); setError(null); }} placeholder="https://example.test/path" required aria-describedby="bookmark-url-error" /><FieldError id="bookmark-url-error" message={error ? firstFieldError(error, 'url') : undefined} /></div>
            <div className="field-group"><label htmlFor="bookmark-title">Tiêu đề</label><input id="bookmark-title" value={draft.title} maxLength={200} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, title: event.target.value }); setError(null); }} required aria-describedby="bookmark-title-error" /><FieldError id="bookmark-title-error" message={error ? firstFieldError(error, 'title') : undefined} /></div>
            <div className="field-group"><label htmlFor="bookmark-description">Mô tả <span className="optional">(tùy chọn)</span></label><textarea id="bookmark-description" value={draft.description} maxLength={20000} rows={4} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, description: event.target.value }); setError(null); }} /></div>
            <div className="form-actions"><button className="secondary-button" type="button" onClick={resetEditor} disabled={busy !== null}>Làm mới</button><SubmitButton busy={busy === 'save'}>{editing ? 'Lưu bookmark' : 'Tạo bookmark'}</SubmitButton></div>
          </form>
          <div className="security-policy"><strong>Provider boundary</strong><span>Health chỉ là trạng thái metadata hiện tại; refresh, external navigation, sharing, tags và collections chưa nằm trong slice này.</span></div>
        </div>
        <div className="content-section">
          <form className="form-panel" onSubmit={applySearch} noValidate>
            <div className="section-heading"><h2>Thư viện</h2><button className="link-button" type="button" onClick={() => { setQueryDraft(''); setQuery(''); }} disabled={loading}>Xóa tìm kiếm</button></div>
            <div className="field-group"><label htmlFor="bookmark-query">Tìm theo tiêu đề, URL hoặc mô tả</label><input id="bookmark-query" value={queryDraft} onChange={(event) => setQueryDraft(event.target.value)} /></div>
            <label className="check-row"><input type="checkbox" checked={includeArchived} onChange={(event) => setIncludeArchived(event.target.checked)} disabled={loading} /><span>Hiển thị bookmark đã archive</span></label>
            <button className="secondary-button" type="submit" disabled={loading}>Áp dụng</button>
          </form>
          {loading ? <div className="loading-state" role="status">Đang tải bookmarks…</div> : items.length === 0 ? <div className="empty-state"><h2>Chưa có bookmark</h2><p>{query ? 'Không có bookmark phù hợp với tìm kiếm.' : 'Tạo bookmark đầu tiên bằng metadata bạn kiểm soát.'}</p></div> : <div className="resource-list"><div className="section-heading"><span className="muted">{items.length} bản ghi · {includeArchived ? 'active và archived' : 'active'}</span></div><div className="resource-cards">{items.map((item) => <article className="resource-card" key={item.id}><div><h3>{item.title}</h3><p className="bookmark-url" title={item.url}>{item.url}</p>{item.description && <p>{item.description}</p>}<span className="muted">{item.status} · Health: {item.health} · cập nhật {dateTime(item.updatedAt)}</span></div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => beginEdit(item)} disabled={busy !== null}>Sửa</button><button className={item.status === 'Archived' ? 'secondary-button' : 'danger-button'} type="button" onClick={() => void transition(item)} disabled={busy !== null}>{busy === `transition:${item.id}` ? 'Đang lưu…' : item.status === 'Archived' ? 'Unarchive' : 'Archive'}</button></div></article>)}</div></div>}
        </div>
      </div>
    </section>
  );
}

type SnippetDraft = {
  title: string;
  language: string;
  body: string;
  description: string;
};

function SnippetsScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<SnippetRecord[]>([]);
  const [includeArchived, setIncludeArchived] = useState(false);
  const [query, setQuery] = useState('');
  const [queryDraft, setQueryDraft] = useState('');
  const [draft, setDraft] = useState<SnippetDraft>({ title: '', language: 'plaintext', body: '', description: '' });
  const [editing, setEditing] = useState<SnippetRecord | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [conflict, setConflict] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const requestKey = useRef<string | null>(null);

  async function load(nextQuery = query, nextIncludeArchived = includeArchived) {
    setLoading(true);
    setError(null);
    setConflict(false);
    try {
      const page = await listSnippets(nextIncludeArchived, nextQuery, 100);
      setItems(Array.isArray(page.items) ? page.items : []);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [query, includeArchived]);

  function resetEditor() {
    setEditing(null);
    setDraft({ title: '', language: 'plaintext', body: '', description: '' });
    requestKey.current = null;
    setConflict(false);
    setError(null);
  }

  function beginEdit(item: SnippetRecord) {
    setEditing(item);
    setDraft({ title: item.title, language: item.language, body: item.body, description: item.description ?? '' });
    requestKey.current = null;
    setConflict(false);
    setError(null);
  }

  function showError(requestError: unknown) {
    const apiError = asApiError(requestError);
    setError(apiError);
    if (apiError.status === 401) void onAuthLost();
    return apiError;
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setConflict(false);
    const title = draft.title.trim();
    const language = draft.language.trim();
    const body = draft.body;
    const description = draft.description.trim() || null;
    if (!title || title.length > 200) {
      setError(new NexoraApiError('Tiêu đề là bắt buộc và tối đa 200 ký tự.', 422, 'ValidationFailed', null, { title: ['Tiêu đề là bắt buộc và tối đa 200 ký tự.'] }));
      return;
    }
    if (!language || !/^[A-Za-z0-9][A-Za-z0-9+.#_-]{0,49}$/.test(language)) {
      setError(new NexoraApiError('Language phải là nhãn text hợp lệ tối đa 50 ký tự.', 422, 'ValidationFailed', null, { language: ['Language không hợp lệ.'] }));
      return;
    }
    if (!body || new TextEncoder().encode(body).length > 1024 * 1024) {
      setError(new NexoraApiError('Source code là bắt buộc và tối đa 1 MiB UTF-8.', 422, 'ValidationFailed', null, { body: ['Source code không hợp lệ.'] }));
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy('save');
    try {
      if (editing) {
        await saveSnippet(editing.id, editing.etag, title, language, body, description, requestKey.current);
      } else {
        await createSnippet(title, language, body, description, requestKey.current);
      }
      requestKey.current = null;
      resetEditor();
      await load();
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) {
        setConflict(true);
        await load();
      }
    } finally {
      setBusy(null);
    }
  }

  async function transition(item: SnippetRecord) {
    const nextStatus = item.status === 'Archived' ? 'Active' : 'Archived';
    if (!window.confirm(`${nextStatus === 'Archived' ? 'Archive' : 'Unarchive'} snippet “${item.title}”?`)) return;
    setBusy(`transition:${item.id}`);
    setError(null);
    try {
      await transitionSnippet(item.id, item.etag, nextStatus, createIdempotencyKey());
      await load();
      if (editing?.id === item.id) resetEditor();
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) await load();
    } finally {
      setBusy(null);
    }
  }

  async function copySnippet(item: SnippetRecord) {
    setBusy(`copy:${item.id}`);
    setError(null);
    try {
      if (!navigator.clipboard) throw new Error('ClipboardUnavailable');
      await navigator.clipboard.writeText(item.body);
      setCopiedId(item.id);
      window.setTimeout(() => setCopiedId((current) => current === item.id ? null : current), 1800);
    } catch {
      setError(new NexoraApiError('Clipboard không khả dụng; source vẫn chỉ được hiển thị dạng text.', 409, 'ClipboardUnavailable'));
    } finally {
      setBusy(null);
    }
  }

  function applySearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setQuery(queryDraft.trim());
  }

  return (
    <section className="content-section" aria-labelledby="snippets-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">FX22 / KNOWLEDGE</p>
          <h1 id="snippets-title">Code snippets</h1>
          <p className="lead">Lưu code/text cá nhân dưới dạng dữ liệu escaped. Nexora không chạy, compile, gửi AI/lint service hoặc publish source.</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void load()} disabled={loading || busy !== null}>{loading ? 'Đang tải…' : 'Tải lại'}</button>
      </div>
      {conflict && <Notice kind="error"><span>Snippet đã thay đổi ở nơi khác. Draft hiện tại vẫn nằm trong memory; tải revision mới trước khi áp dụng lại.</span><button className="inline-button" type="button" onClick={() => void load()} disabled={loading}>Tải revision</button></Notice>}
      {error && !conflict && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      <div className="resource-layout">
        <div className="content-section">
          <form className="form-panel" onSubmit={save} noValidate>
            <div className="section-heading"><h2>{editing ? 'Sửa snippet' : 'Snippet mới'}</h2>{editing && <button className="link-button" type="button" onClick={resetEditor}>Hủy sửa</button>}</div>
            <div className="field-group"><label htmlFor="snippet-title">Tiêu đề</label><input id="snippet-title" value={draft.title} maxLength={200} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, title: event.target.value }); setError(null); }} required aria-describedby="snippet-title-error" /><FieldError id="snippet-title-error" message={error ? firstFieldError(error, 'title') : undefined} /></div>
            <div className="field-group"><label htmlFor="snippet-language">Language</label><input id="snippet-language" value={draft.language} maxLength={50} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, language: event.target.value }); setError(null); }} placeholder="plaintext" required aria-describedby="snippet-language-error" /><FieldError id="snippet-language-error" message={error ? firstFieldError(error, 'language') : undefined} /></div>
            <div className="field-group"><label htmlFor="snippet-body">Source code / text</label><textarea id="snippet-body" value={draft.body} maxLength={1024 * 1024} rows={13} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, body: event.target.value }); setError(null); }} spellCheck={false} required aria-describedby="snippet-body-help snippet-body-error" /><p className="field-help" id="snippet-body-help">Tối đa 1 MiB UTF-8; shell/HTML/script chỉ được lưu và hiển thị như text.</p><FieldError id="snippet-body-error" message={error ? firstFieldError(error, 'body') : undefined} /></div>
            <div className="field-group"><label htmlFor="snippet-description">Mô tả <span className="optional">(tùy chọn)</span></label><textarea id="snippet-description" value={draft.description} maxLength={20000} rows={3} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, description: event.target.value }); setError(null); }} /></div>
            <div className="form-actions"><button className="secondary-button" type="button" onClick={resetEditor} disabled={busy !== null}>Làm mới</button><SubmitButton busy={busy === 'save'}>{editing ? 'Lưu version' : 'Tạo snippet'}</SubmitButton></div>
          </form>
          <div className="security-policy"><strong>Safety boundary</strong><span>Version cũ được giữ append-only trong SQL; Archive là readonly. Copy chỉ xảy ra khi người dùng bấm rõ ràng và không ghi source vào audit.</span></div>
        </div>
        <div className="content-section">
          <form className="form-panel" onSubmit={applySearch} noValidate>
            <div className="section-heading"><h2>Thư viện</h2><button className="link-button" type="button" onClick={() => { setQueryDraft(''); setQuery(''); }} disabled={loading}>Xóa tìm kiếm</button></div>
            <div className="field-group"><label htmlFor="snippet-query">Tìm theo tiêu đề, language, mô tả hoặc source</label><input id="snippet-query" value={queryDraft} onChange={(event) => setQueryDraft(event.target.value)} /></div>
            <label className="check-row"><input type="checkbox" checked={includeArchived} onChange={(event) => setIncludeArchived(event.target.checked)} disabled={loading} /><span>Hiển thị snippet đã archive</span></label>
            <button className="secondary-button" type="submit" disabled={loading}>Áp dụng</button>
          </form>
          {loading ? <div className="loading-state" role="status">Đang tải snippets…</div> : items.length === 0 ? <div className="empty-state"><h2>Chưa có snippet</h2><p>{query ? 'Không có snippet phù hợp với tìm kiếm.' : 'Tạo snippet đầu tiên; source sẽ không được thực thi.'}</p></div> : <div className="resource-list"><div className="section-heading"><span className="muted">{items.length} bản ghi · {includeArchived ? 'active và archived' : 'active'}</span></div><div className="resource-cards">{items.map((item) => <article className="resource-card snippet-card" key={item.id}><div className="snippet-content"><div className="section-heading"><div><h3>{item.title}</h3><span className="muted">{item.language} · version {item.versionNumber} · {item.status} · cập nhật {dateTime(item.updatedAt)}</span></div></div>{item.description && <p>{item.description}</p>}<pre className="snippet-code"><code>{item.body}</code></pre></div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => void copySnippet(item)} disabled={busy !== null}>{copiedId === item.id ? 'Đã copy' : 'Copy'}</button><button className="secondary-button" type="button" onClick={() => beginEdit(item)} disabled={busy !== null || item.status === 'Archived'}>Sửa</button><button className={item.status === 'Archived' ? 'secondary-button' : 'danger-button'} type="button" onClick={() => void transition(item)} disabled={busy !== null}>{busy === `transition:${item.id}` ? 'Đang lưu…' : item.status === 'Archived' ? 'Unarchive' : 'Archive'}</button></div></article>)}</div></div>}
        </div>
      </div>
    </section>
  );
}

type ReadingState = 'Unread' | 'Reading' | 'Read' | 'Archived';

function ReadLaterScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<ReadingItemRecord[]>([]);
  const [bookmarks, setBookmarks] = useState<BookmarkRecord[]>([]);
  const [stateFilter, setStateFilter] = useState('');
  const [selectedBookmarkId, setSelectedBookmarkId] = useState('');
  const [positionDraft, setPositionDraft] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [sourceError, setSourceError] = useState<NexoraApiError | null>(null);
  const [conflict, setConflict] = useState(false);
  const requestKey = useRef<string | null>(null);

  async function load(nextState = stateFilter) {
    setLoading(true);
    setError(null);
    setSourceError(null);
    setConflict(false);
    try {
      const page = await listReadingQueue(nextState, 100);
      const nextItems = Array.isArray(page.items) ? page.items : [];
      setItems(nextItems);
      setPositionDraft((current) => {
        const next = { ...current };
        nextItems.forEach((item) => { next[item.id] ??= String(item.progress); });
        return next;
      });
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    }
    try {
      const page = await listBookmarks(true, '', 100);
      setBookmarks(Array.isArray(page.items) ? page.items : []);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setSourceError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [stateFilter]);

  function showError(requestError: unknown) {
    const apiError = asApiError(requestError);
    setError(apiError);
    if (apiError.status === 401) void onAuthLost();
    return apiError;
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setConflict(false);
    if (!selectedBookmarkId) {
      setError(new NexoraApiError('Chọn một bookmark trước khi lưu vào Read Later.', 422, 'ValidationFailed'));
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy('save');
    try {
      await saveReadingItem('Bookmark', selectedBookmarkId, requestKey.current);
      requestKey.current = null;
      setSelectedBookmarkId('');
      await load();
    } catch (requestError) {
      showError(requestError);
    } finally {
      setBusy(null);
    }
  }

  async function changeState(item: ReadingItemRecord, state: Exclude<ReadingState, 'Archived'>, progress?: number) {
    if (!item.sourceAvailable) return;
    const key = createIdempotencyKey();
    setBusy(`state:${item.id}`);
    setError(null);
    setConflict(false);
    try {
      await updateReadingItem(item.id, item.etag, state, progress, key);
      await load();
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) {
        setConflict(true);
        await load();
      }
    } finally {
      setBusy(null);
    }
  }

  async function savePosition(item: ReadingItemRecord) {
    const raw = positionDraft[item.id] ?? String(item.progress);
    const progress = Number(raw);
    if (!Number.isFinite(progress) || progress < 0 || progress > 1) {
      setError(new NexoraApiError('Position phải là số từ 0 đến 1.', 422, 'ValidationFailed'));
      return;
    }
    await changeState(item, 'Reading', progress);
  }

  async function remove(item: ReadingItemRecord) {
    if (!window.confirm(`Gỡ “${item.safeTitleSnapshot}” khỏi Read Later? Bookmark nguồn sẽ không bị xóa.`)) return;
    setBusy(`remove:${item.id}`);
    setError(null);
    setConflict(false);
    try {
      await removeReadingItem(item.id, item.etag, createIdempotencyKey());
      await load();
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) {
        setConflict(true);
        await load();
      }
    } finally {
      setBusy(null);
    }
  }

  const selectableBookmarks = bookmarks.filter((bookmark) => bookmark.status === 'Active' || bookmark.status === 'Archived');
  return (
    <section className="content-section" aria-labelledby="read-later-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">FX23 / KNOWLEDGE</p>
          <h1 id="read-later-title">Read Later</h1>
          <p className="lead">Queue chỉ lưu tham chiếu Bookmark và snapshot title/URL an toàn. Không copy body, không tự fetch hoặc mở provider bên ngoài.</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void load()} disabled={loading || busy !== null}>{loading ? 'Đang tải…' : 'Tải lại'}</button>
      </div>
      {conflict && <Notice kind="error"><span>Reading item đã thay đổi ở nơi khác. Hãy tải revision mới rồi thực hiện lại thao tác.</span><button className="inline-button" type="button" onClick={() => void load()} disabled={loading}>Tải revision</button></Notice>}
      {error && !conflict && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {sourceError && <Notice kind="info">Không tải được danh sách Bookmark nguồn; queue hiện có vẫn được hiển thị, nhưng không thể lưu nguồn mới. {sourceError.message}</Notice>}
      <div className="resource-layout">
        <div className="content-section">
          <form className="form-panel" onSubmit={save} noValidate>
            <div className="section-heading"><h2>Lưu source</h2></div>
            <div className="field-group">
              <label htmlFor="read-later-source">Bookmark nguồn</label>
              <select id="read-later-source" value={selectedBookmarkId} onChange={(event) => { requestKey.current = null; setSelectedBookmarkId(event.target.value); setError(null); }} disabled={loading || busy !== null || selectableBookmarks.length === 0}>
                <option value="">{selectableBookmarks.length === 0 ? 'Không có bookmark khả dụng' : 'Chọn bookmark'}</option>
                {selectableBookmarks.map((bookmark) => <option key={bookmark.id} value={bookmark.id}>{bookmark.title} · {bookmark.url}</option>)}
              </select>
              <p className="field-help">Save retry cùng source chỉ giữ một queue entry theo owner; source Archived vẫn chỉ là metadata được phép đọc.</p>
            </div>
            <div className="form-actions"><SubmitButton busy={busy === 'save'}>Lưu vào Read Later</SubmitButton></div>
          </form>
          <div className="security-policy"><strong>Source boundary</strong><span>Xóa queue không xóa Bookmark. Khi source bị xóa, disabled hoặc không còn quyền đọc, UI chỉ giữ snapshot an toàn và không hiển thị body cache.</span></div>
        </div>
        <div className="content-section">
          <div className="form-panel">
            <div className="section-heading"><h2>Queue của bạn</h2><span className="muted">{items.length} mục</span></div>
            <div className="field-group"><label htmlFor="read-later-state">Lọc trạng thái</label><select id="read-later-state" value={stateFilter} onChange={(event) => setStateFilter(event.target.value)} disabled={loading || busy !== null}><option value="">Tất cả</option><option value="Unread">Unread</option><option value="Reading">Reading</option><option value="Read">Read</option></select></div>
          </div>
          {loading ? <div className="loading-state" role="status">Đang tải Read Later…</div> : items.length === 0 ? <div className="empty-state"><h2>Queue đang trống</h2><p>{stateFilter ? 'Không có mục phù hợp với bộ lọc.' : 'Chọn một Bookmark để lưu source vào queue.'}</p></div> : <div className="resource-list"><div className="resource-cards">{items.map((item) => <article className={`resource-card reading-card${item.sourceAvailable ? '' : ' source-unavailable'}`} key={item.id}><div><h3>{item.safeTitleSnapshot}</h3><p className="reading-url" title="URL snapshot bất hoạt"><code>{item.safeUrlSnapshot}</code></p><span className="muted">{item.sourceType} · {item.state} · lưu {dateTime(item.savedAt)}{item.readAt ? ` · đọc ${dateTime(item.readAt)}` : ''}</span>{item.sourceAvailable ? <div className="reading-position"><label htmlFor={`reading-position-${item.id}`}>Position metadata (0–1)</label><input id={`reading-position-${item.id}`} type="number" min="0" max="1" step="0.01" value={positionDraft[item.id] ?? String(item.progress)} onChange={(event) => setPositionDraft({ ...positionDraft, [item.id]: event.target.value })} disabled={busy !== null} /><button className="secondary-button" type="button" onClick={() => void savePosition(item)} disabled={busy !== null}>{busy === `state:${item.id}` ? 'Đang lưu…' : 'Lưu vị trí'}</button></div> : <p className="source-unavailable-message">Source unavailable. Không có cached body hoặc % đọc giả.</p>}</div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => void changeState(item, item.state === 'Read' ? 'Unread' : 'Read')} disabled={!item.sourceAvailable || busy !== null || item.state === 'Archived'}>{item.state === 'Read' ? 'Đánh dấu chưa đọc' : 'Đánh dấu đã đọc'}</button>{item.state !== 'Archived' && item.state !== 'Read' && <button className="secondary-button" type="button" onClick={() => void changeState(item, 'Reading')} disabled={!item.sourceAvailable || busy !== null}>Đang đọc</button>}<button className="danger-button" type="button" onClick={() => void remove(item)} disabled={busy !== null}>{busy === `remove:${item.id}` ? 'Đang gỡ…' : 'Gỡ khỏi queue'}</button></div></article>)}</div></div>}
        </div>
      </div>
    </section>
  );
}

const LOCAL_TAG_NAMESPACES = ['projects', 'documents', 'bookmarks', 'snippets'] as const;

function OrganizationTagsScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<TagRecord[]>([]);
  const [tagNamespace, setTagNamespace] = useState<(typeof LOCAL_TAG_NAMESPACES)[number]>('projects');
  const [queryDraft, setQueryDraft] = useState('');
  const [query, setQuery] = useState('');
  const [name, setName] = useState('');
  const [color, setColor] = useState('');
  const [editing, setEditing] = useState<TagRecord | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [conflict, setConflict] = useState(false);
  const requestKey = useRef<string | null>(null);

  async function load(nextNamespace = tagNamespace, nextQuery = query) {
    setLoading(true);
    setError(null);
    setConflict(false);
    try {
      const page = await listOrganizationTags(nextNamespace, nextQuery, 100);
      setItems(Array.isArray(page.items) ? page.items : []);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [tagNamespace, query]);

  function resetEditor() {
    setEditing(null);
    setName('');
    setColor('');
    requestKey.current = null;
    setError(null);
    setConflict(false);
  }

  function beginEdit(tag: TagRecord) {
    setEditing(tag);
    setTagNamespace(tag.namespace as (typeof LOCAL_TAG_NAMESPACES)[number]);
    setName(tag.name);
    setColor(tag.color ?? '');
    requestKey.current = null;
    setError(null);
    setConflict(false);
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmedName = name.trim();
    const trimmedColor = color.trim();
    if (!trimmedName || trimmedName.length > 50) {
      setError(new NexoraApiError('Tên tag phải từ 1 đến 50 ký tự.', 422, 'ValidationFailed'));
      return;
    }
    if (trimmedColor && !/^#[0-9a-f]{6}$/i.test(trimmedColor)) {
      setError(new NexoraApiError('Màu tag phải có dạng #RRGGBB.', 422, 'ValidationFailed'));
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy('save');
    setError(null);
    setConflict(false);
    try {
      if (editing) {
        await renameOrganizationTag(editing.id, editing.etag, trimmedName, trimmedColor || null, requestKey.current);
      } else {
        await createOrganizationTag(tagNamespace, trimmedName, trimmedColor || null, requestKey.current);
      }
      requestKey.current = null;
      resetEditor();
      await load();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 412) {
        setConflict(true);
        await load();
      }
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  async function remove(tag: TagRecord) {
    if (tag.usageCount > 0 || !window.confirm(`Xóa tag “${tag.name}” khỏi namespace ${tag.namespace}?`)) return;
    setBusy(`remove:${tag.id}`);
    setError(null);
    setConflict(false);
    try {
      await removeOrganizationTag(tag.id, tag.etag);
      if (editing?.id === tag.id) resetEditor();
      await load();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 412) {
        setConflict(true);
        await load();
      }
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(null);
    }
  }

  function applySearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setQuery(queryDraft.trim());
  }

  return (
    <section className="content-section" aria-labelledby="organization-tags-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">FX24 / ORGANIZATION</p>
          <h1 id="organization-tags-title">Tag management</h1>
          <p className="lead">Tag là nhãn cá nhân theo namespace. Slice này chỉ quản lý catalog; chưa gắn tag vào resource và tag không cấp quyền.</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void load()} disabled={loading || busy !== null}>{loading ? 'Đang tải…' : 'Tải lại'}</button>
      </div>
      {conflict && <Notice kind="error"><span>Tag đã thay đổi ở nơi khác. Draft vẫn được giữ trong memory; tải revision mới trước khi lưu lại.</span><button className="inline-button" type="button" onClick={() => void load()} disabled={loading}>Tải revision</button></Notice>}
      {error && !conflict && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      <div className="resource-layout">
        <form className="form-panel resource-form" onSubmit={save} noValidate>
          <div className="section-heading"><h2>{editing ? 'Đổi tên tag' : 'Tag mới'}</h2>{editing && <button className="link-button" type="button" onClick={resetEditor}>Hủy sửa</button>}</div>
          <div className="field-group"><label htmlFor="organization-tag-namespace">Namespace</label><select id="organization-tag-namespace" value={tagNamespace} onChange={(event) => { requestKey.current = null; setTagNamespace(event.target.value as (typeof LOCAL_TAG_NAMESPACES)[number]); setError(null); }} disabled={editing !== null || busy !== null}>{LOCAL_TAG_NAMESPACES.map((value) => <option key={value} value={value}>{value}</option>)}</select><p className="field-help">Projects và Tasks dùng chung namespace; các provider khác giữ namespace riêng.</p></div>
          <div className="field-group"><label htmlFor="organization-tag-name">Tên tag</label><input id="organization-tag-name" value={name} maxLength={50} onChange={(event) => { requestKey.current = null; setName(event.target.value); setError(null); }} required /></div>
          <div className="field-group"><label htmlFor="organization-tag-color">Màu <span className="optional">(#RRGGBB, tùy chọn)</span></label><input id="organization-tag-color" value={color} maxLength={7} placeholder="#2F67D8" onChange={(event) => { requestKey.current = null; setColor(event.target.value); setError(null); }} /></div>
          <div className="form-actions"><button className="secondary-button" type="button" onClick={resetEditor} disabled={busy !== null}>Làm mới</button><SubmitButton busy={busy === 'save'}>{editing ? 'Lưu tag' : 'Tạo tag'}</SubmitButton></div>
        </form>
        <div className="content-section">
          <form className="form-panel" onSubmit={applySearch} noValidate>
            <div className="section-heading"><h2>Tags của bạn</h2><button className="link-button" type="button" onClick={() => { setQueryDraft(''); setQuery(''); }} disabled={loading}>Xóa tìm kiếm</button></div>
            <div className="field-group"><label htmlFor="organization-tag-search">Tìm theo tên</label><input id="organization-tag-search" value={queryDraft} onChange={(event) => setQueryDraft(event.target.value)} /></div>
            <button className="secondary-button" type="submit" disabled={loading}>Áp dụng</button>
          </form>
          {loading ? <div className="loading-state" role="status">Đang tải tags…</div> : items.length === 0 ? <div className="empty-state"><h2>Chưa có tag</h2><p>{query ? 'Không có tag phù hợp với tìm kiếm.' : `Tạo tag đầu tiên trong namespace ${tagNamespace}.`}</p></div> : <div className="resource-list"><div className="section-heading"><span className="muted">Namespace: {tagNamespace} · {items.length} tag</span></div><div className="resource-cards">{items.map((tag) => <article className="resource-card" key={tag.id}><div><h3>{tag.color && <span aria-hidden="true" style={{ color: tag.color }}>● </span>}{tag.name}</h3><span className="muted">{tag.namespace} · {tag.usageCount} resource · cập nhật {dateTime(tag.updatedAt)}</span></div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => beginEdit(tag)} disabled={busy !== null}>Đổi tên</button><button className="danger-button" type="button" onClick={() => void remove(tag)} disabled={busy !== null || tag.usageCount > 0}>{tag.usageCount > 0 ? 'Đang dùng' : 'Xóa'}</button></div></article>)}</div></div>}
        </div>
      </div>
      <div className="security-policy"><strong>Boundary</strong><span>Assignment vào Project/Task/Document/Bookmark/Snippet, Collections, Templates và sharing chưa nằm trong slice; server vẫn giữ action gate riêng cho các capability đó.</span></div>
    </section>
  );
}

type ToolboxOptions = Record<string, string>;

function DeveloperToolsScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [tools, setTools] = useState<ToolboxTool[]>([]);
  const [selectedCode, setSelectedCode] = useState('base64');
  const [input, setInput] = useState('');
  const [operation, setOperation] = useState('encode');
  const [algorithm, setAlgorithm] = useState('SHA-256');
  const [count, setCount] = useState('1');
  const [length, setLength] = useState('24');
  const [indent, setIndent] = useState(true);
  const [pattern, setPattern] = useState('');
  const [ignoreCase, setIgnoreCase] = useState(false);
  const [output, setOutput] = useState<ToolboxRunResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);

  async function loadCatalog() {
    setLoading(true);
    setError(null);
    try {
      const catalog = await listDeveloperTools();
      setTools(Array.isArray(catalog.items) ? catalog.items : []);
      if (catalog.items?.length > 0 && !catalog.items.some((tool) => tool.code === selectedCode)) {
        setSelectedCode(catalog.items[0].code);
      }
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void loadCatalog(); }, []);

  const selectedTool = tools.find((tool) => tool.code === selectedCode);

  function optionsForTool(): ToolboxOptions {
    switch (selectedCode) {
      case 'base64':
      case 'url-codec':
      case 'html-codec':
        return { operation };
      case 'hash':
        return { algorithm };
      case 'uuid':
        return { count };
      case 'password':
        return { length };
      case 'json':
        return { indent: String(indent) };
      case 'regex':
        return { pattern, ignoreCase: String(ignoreCase) };
      default:
        return {};
    }
  }

  async function run(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setCopied(false);
    if (new TextEncoder().encode(input).length > 1024 * 1024) {
      setError(new NexoraApiError('Input tối đa 1 MiB UTF-8.', 422, 'InputTooLarge'));
      return;
    }
    if (selectedCode === 'regex' && !pattern.trim()) {
      setError(new NexoraApiError('Regex pattern là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    setBusy(true);
    try {
      setOutput(await runDeveloperTool(selectedCode, input, optionsForTool()));
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setBusy(false);
    }
  }

  async function copyOutput() {
    if (!output) return;
    try {
      await navigator.clipboard.writeText(output.output);
      setCopied(true);
    } catch {
      setError(new NexoraApiError('Không thể truy cập clipboard; hãy chọn và copy thủ công.', 0, 'ClipboardUnavailable'));
    }
  }

  function clearWorkbench() {
    setInput('');
    setOutput(null);
    setError(null);
    setCopied(false);
  }

  return (
    <section className="content-section" aria-labelledby="developer-tools-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">FX32 / DEVELOPER TOOLBOX</p>
          <h1 id="developer-tools-title">Developer tools</h1>
          <p className="lead">Pure utilities chạy local trong memory. Không chạy code, không gọi network và không tự lưu input/output.</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void loadCatalog()} disabled={loading || busy}>{loading ? 'Đang tải…' : 'Tải lại'}</button>
      </div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {loading ? <div className="loading-state" role="status">Đang tải danh mục tool…</div> : tools.length === 0 ? <div className="empty-state"><h2>Chưa có tool khả dụng</h2><p>Module hoặc action grant hiện không khả dụng trong PersonalSpace này.</p></div> : <div className="resource-layout">
        <form className="form-panel resource-form" onSubmit={run} noValidate>
          <div className="section-heading"><h2>Workbench</h2><span className="state-pill state-active">Local only</span></div>
          <div className="field-group"><label htmlFor="tool-code">Tool</label><select id="tool-code" value={selectedCode} onChange={(event) => { setSelectedCode(event.target.value); setOutput(null); setError(null); }} disabled={busy}>{tools.map((tool) => <option key={tool.code} value={tool.code}>{tool.name} · {tool.category}</option>)}</select></div>
          {selectedTool && <div className="security-policy"><strong>{selectedTool.description}</strong><span>Action: {selectedTool.actionKey} · {selectedTool.executionMode}</span></div>}
          {(selectedCode === 'base64' || selectedCode === 'url-codec' || selectedCode === 'html-codec') && <div className="field-group"><label htmlFor="tool-operation">Operation</label><select id="tool-operation" value={operation} onChange={(event) => setOperation(event.target.value)} disabled={busy}><option value="encode">Encode</option><option value="decode">Decode</option></select></div>}
          {selectedCode === 'hash' && <div className="field-group"><label htmlFor="tool-algorithm">Algorithm</label><select id="tool-algorithm" value={algorithm} onChange={(event) => setAlgorithm(event.target.value)} disabled={busy}><option>SHA-256</option><option>SHA-384</option><option>SHA-512</option><option>MD5</option><option>SHA-1</option></select></div>}
          {selectedCode === 'uuid' && <div className="field-group"><label htmlFor="tool-count">Số UUID (1–20)</label><input id="tool-count" inputMode="numeric" value={count} onChange={(event) => setCount(event.target.value)} min={1} max={20} disabled={busy} /></div>}
          {selectedCode === 'password' && <div className="field-group"><label htmlFor="tool-length">Độ dài password (15–128)</label><input id="tool-length" inputMode="numeric" value={length} onChange={(event) => setLength(event.target.value)} min={15} max={128} disabled={busy} /></div>}
          {selectedCode === 'json' && <label className="check-row"><input type="checkbox" checked={indent} onChange={(event) => setIndent(event.target.checked)} disabled={busy} /><span>Indent output</span></label>}
          {selectedCode === 'regex' && <><div className="field-group"><label htmlFor="tool-pattern">Regex pattern</label><input id="tool-pattern" value={pattern} onChange={(event) => setPattern(event.target.value)} maxLength={10000} disabled={busy} required /></div><label className="check-row"><input type="checkbox" checked={ignoreCase} onChange={(event) => setIgnoreCase(event.target.checked)} disabled={busy} /><span>Ignore case</span></label></>}
          <div className="field-group"><label htmlFor="tool-input">Input <span className="optional">(tối đa 1 MiB)</span></label><textarea id="tool-input" value={input} onChange={(event) => { setInput(event.target.value); setOutput(null); setError(null); }} rows={12} maxLength={1024 * 1024} disabled={busy} spellCheck={false} /></div>
          <div className="form-actions"><button className="secondary-button" type="button" onClick={clearWorkbench} disabled={busy}>Xóa</button><SubmitButton busy={busy}>Chạy tool</SubmitButton></div>
        </form>
        <div className="resource-list">
          <div className="section-heading"><h2>Output</h2>{output && <button className="secondary-button" type="button" onClick={() => void copyOutput()}>{copied ? 'Đã copy' : 'Copy output'}</button>}</div>
          {!output ? <div className="empty-state"><h3>Chưa có output</h3><p>Nhập dữ liệu và bấm “Chạy tool”. Output chỉ tồn tại trong memory của phiên trình duyệt.</p></div> : <div className="form-panel"><div className="delivery-summary"><span>{output.durationMilliseconds} ms</span><span>UTF-8 · memory-only</span>{output.warning && <span>{output.warning}</span>}</div>{output.errorPath && <Notice kind="error">Vị trí lỗi: {output.errorPath}</Notice>}<pre className="snippet-code" aria-label="Tool output">{output.output}</pre></div>}
          <div className="security-policy"><strong>Safety boundary</strong><span>Network HTTP/DNS, code formatter execution, QR navigation, history persistence và Save to Snippet chưa nằm trong local slice này.</span></div>
        </div>
      </div>}
    </section>
  );
}

type GoalFormDraft = {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  targetTitle: string;
  initialValue: string;
  currentValue: string;
  targetValue: string;
};

function emptyGoalDraft(): GoalFormDraft {
  return { title: '', description: '', startDate: '', endDate: '', targetTitle: '', initialValue: '0', currentValue: '0', targetValue: '' };
}

function GoalsScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<GoalRecord[]>([]);
  const [selected, setSelected] = useState<GoalDetail | null>(null);
  const [editing, setEditing] = useState<GoalRecord | null>(null);
  const [draft, setDraft] = useState<GoalFormDraft>(emptyGoalDraft);
  const [progressValue, setProgressValue] = useState('');
  const [progressNote, setProgressNote] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [conflict, setConflict] = useState(false);
  const requestKey = useRef<string | null>(null);

  async function load(selectedId?: string) {
    setLoading(true);
    setError(null);
    try {
      const page = await listGoals('', '', 100);
      const nextItems = Array.isArray(page.items) ? page.items : [];
      setItems(nextItems);
      const id = selectedId ?? selected?.goal.id;
      if (id && nextItems.some((item) => item.id === id)) {
        setSelected(await getGoal(id));
      } else if (!id || !nextItems.some((item) => item.id === id)) {
        setSelected(null);
      }
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, []);

  function resetEditor() {
    setEditing(null);
    setDraft(emptyGoalDraft());
    requestKey.current = null;
    setConflict(false);
  }

  function beginEdit(goal: GoalRecord) {
    setEditing(goal);
    setDraft({
      title: goal.title,
      description: goal.description ?? '',
      startDate: goal.startDate ?? '',
      endDate: goal.endDate ?? '',
      targetTitle: '',
      initialValue: '0',
      currentValue: '0',
      targetValue: ''
    });
    requestKey.current = null;
    setError(null);
    setConflict(false);
  }

  function showError(requestError: unknown) {
    const apiError = asApiError(requestError);
    setError(apiError);
    if (apiError.status === 401) void onAuthLost();
    return apiError;
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setConflict(false);
    const title = draft.title.trim();
    if (!title || title.length > 200) {
      setError(new NexoraApiError('Tên goal phải từ 1 đến 200 ký tự.', 422, 'ValidationFailed'));
      return;
    }
    if (draft.startDate && draft.endDate && draft.endDate < draft.startDate) {
      setError(new NexoraApiError('Ngày kết thúc không được trước ngày bắt đầu.', 422, 'ValidationFailed'));
      return;
    }
    let numericTarget: { title: string; initialValue: number; currentValue: number; targetValue: number } | null = null;
    const targetFields = [draft.targetTitle.trim(), draft.initialValue.trim(), draft.currentValue.trim(), draft.targetValue.trim()];
    if (!editing && targetFields.some(Boolean)) {
      const initialValue = Number(draft.initialValue);
      const currentValue = Number(draft.currentValue);
      const targetValue = Number(draft.targetValue);
      if (!draft.targetTitle.trim() || !Number.isFinite(initialValue) || !Number.isFinite(currentValue) || !Number.isFinite(targetValue) || targetValue <= initialValue) {
        setError(new NexoraApiError('Numeric target cần title hợp lệ và target lớn hơn initial.', 422, 'ValidationFailed'));
        return;
      }
      numericTarget = { title: draft.targetTitle.trim(), initialValue, currentValue, targetValue };
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy('save');
    try {
      const result = editing
        ? await updateGoal(editing.id, editing.etag, title, draft.description.trim() || null, draft.startDate || null, draft.endDate || null, requestKey.current)
        : await createGoal(title, draft.description.trim() || null, draft.startDate || null, draft.endDate || null, numericTarget, requestKey.current);
      requestKey.current = null;
      setSelected(result);
      resetEditor();
      await load(result.goal.id);
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) {
        setConflict(true);
        await load(editing?.id);
      }
    } finally {
      setBusy(null);
    }
  }

  async function selectGoal(id: string) {
    setBusy(`load:${id}`);
    setError(null);
    try {
      setSelected(await getGoal(id));
    } catch (requestError) {
      showError(requestError);
    } finally {
      setBusy(null);
    }
  }

  async function transition(status: string) {
    if (!selected) return;
    setBusy(`transition:${status}`);
    setError(null);
    setConflict(false);
    try {
      const result = await transitionGoal(selected.goal.id, selected.goal.etag, status);
      setSelected(result);
      await load(result.goal.id);
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) {
        setConflict(true);
        await load(selected.goal.id);
      }
    } finally {
      setBusy(null);
    }
  }

  async function recordProgress(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const target = selected.targets.find((item) => item.kind === 'Numeric');
    const value = Number(progressValue);
    if (!target || !Number.isFinite(value)) {
      setError(new NexoraApiError('Nhập một giá trị numeric hợp lệ.', 422, 'ValidationFailed'));
      return;
    }
    setBusy('progress');
    setError(null);
    setConflict(false);
    try {
      const result = await recordGoalProgress(selected.goal.id, target.id, selected.goal.etag, value, progressNote.trim() || null);
      setSelected(result);
      setProgressValue('');
      setProgressNote('');
      await load(result.goal.id);
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) {
        setConflict(true);
        await load(selected.goal.id);
      }
    } finally {
      setBusy(null);
    }
  }

  const numericTarget = selected?.targets.find((item) => item.kind === 'Numeric');
  const percent = selected ? Math.round(Math.max(0, Math.min(1, selected.goal.progress)) * 100) : 0;
  return (
    <section className="content-section" aria-labelledby="goals-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">FX16 / GOALS</p>
          <h1 id="goals-title">Goals</h1>
          <p className="lead">Goal và numeric target thuộc PersonalSpace hiện tại. Progress được ghi thành event SQL; không tự liên kết Task hay provider ngoài.</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void load()} disabled={loading || busy !== null}>{loading ? 'Đang tải…' : 'Tải lại'}</button>
      </div>
      {conflict && <Notice kind="error"><span>Goal đã thay đổi ở nơi khác. Draft vẫn giữ trong memory; hãy tải revision mới trước khi lưu.</span><button className="inline-button" type="button" onClick={() => void load(selected?.goal.id)} disabled={loading}>Tải revision</button></Notice>}
      {error && !conflict && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      <div className="resource-layout">
        <form className="form-panel resource-form" onSubmit={save} noValidate>
          <div className="section-heading"><h2>{editing ? 'Sửa goal' : 'Goal mới'}</h2>{editing && <button className="link-button" type="button" onClick={resetEditor}>Hủy sửa</button>}</div>
          <div className="field-group"><label htmlFor="goal-title">Tên goal</label><input id="goal-title" value={draft.title} maxLength={200} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, title: event.target.value }); }} required /></div>
          <div className="field-group"><label htmlFor="goal-description">Mô tả <span className="optional">(tùy chọn)</span></label><textarea id="goal-description" value={draft.description} maxLength={20000} rows={3} onChange={(event) => { requestKey.current = null; setDraft({ ...draft, description: event.target.value }); }} /></div>
          <div className="form-grid"><div className="field-group"><label htmlFor="goal-start">Bắt đầu</label><input id="goal-start" type="date" value={draft.startDate} onChange={(event) => setDraft({ ...draft, startDate: event.target.value })} /></div><div className="field-group"><label htmlFor="goal-end">Kết thúc</label><input id="goal-end" type="date" value={draft.endDate} onChange={(event) => setDraft({ ...draft, endDate: event.target.value })} /></div></div>
          {!editing && <div className="form-panel nested-panel"><div className="section-heading"><h3>Numeric target <span className="optional">(tùy chọn)</span></h3></div><div className="field-group"><label htmlFor="goal-target-title">Tên target</label><input id="goal-target-title" value={draft.targetTitle} maxLength={200} onChange={(event) => setDraft({ ...draft, targetTitle: event.target.value })} /></div><div className="form-grid"><div className="field-group"><label htmlFor="goal-target-initial">Initial</label><input id="goal-target-initial" type="number" step="any" value={draft.initialValue} onChange={(event) => setDraft({ ...draft, initialValue: event.target.value })} /></div><div className="field-group"><label htmlFor="goal-target-current">Current</label><input id="goal-target-current" type="number" step="any" value={draft.currentValue} onChange={(event) => setDraft({ ...draft, currentValue: event.target.value })} /></div><div className="field-group"><label htmlFor="goal-target-value">Target</label><input id="goal-target-value" type="number" step="any" value={draft.targetValue} onChange={(event) => setDraft({ ...draft, targetValue: event.target.value })} /></div></div><p className="field-help">Target phải lớn hơn initial; các giá trị được lưu tối đa 8 chữ số thập phân.</p></div>}
          <div className="form-actions"><button className="secondary-button" type="button" onClick={resetEditor} disabled={busy !== null}>Làm mới</button><SubmitButton busy={busy === 'save'}>{editing ? 'Lưu goal' : 'Tạo goal'}</SubmitButton></div>
        </form>
        <div className="content-section">
          <div className="section-heading"><h2>Goals của bạn</h2><span className="muted">{items.length} goal</span></div>
          {loading ? <div className="loading-state" role="status">Đang tải goals…</div> : items.length === 0 ? <div className="empty-state"><h2>Chưa có goal</h2><p>Tạo goal đầu tiên để theo dõi một kết quả có thể đo lường.</p></div> : <div className="resource-cards">{items.map((goal) => { const itemPercent = Math.round(Math.max(0, Math.min(1, goal.progress)) * 100); return <article className={selected?.goal.id === goal.id ? 'resource-card selected-card' : 'resource-card'} key={goal.id}><button className="resource-card-button" type="button" onClick={() => void selectGoal(goal.id)} disabled={busy !== null}><span><strong>{goal.title}</strong><span className="muted">{goal.status} · {goal.targetCount} target · {itemPercent}%</span></span><span className="goal-progress"><progress max={100} value={itemPercent} aria-label={`Tiến độ ${goal.title}`} /></span></button><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => beginEdit(goal)} disabled={busy !== null || goal.status === 'Completed' || goal.status === 'Abandoned'}>Sửa</button></div></article>; })}</div>}
        </div>
      </div>
      {selected && <div className="form-panel goal-detail"><div className="section-heading"><div><p className="eyebrow">SELECTED GOAL</p><h2>{selected.goal.title}</h2></div><span className="state-pill state-active">{selected.goal.status}</span></div><p>{selected.goal.description || 'Không có mô tả.'}</p><div className="goal-progress-summary"><strong>{percent}%</strong><progress max={100} value={percent} aria-label="Tiến độ goal" /><span className="muted">{selected.goal.targetCount} target · cập nhật {dateTime(selected.goal.updatedAt)}</span></div><div className="form-actions">{selected.goal.status === 'Draft' && <button className="secondary-button" type="button" onClick={() => void transition('Active')} disabled={busy !== null}>Bắt đầu</button>}{selected.goal.status === 'Active' && <><button className="secondary-button" type="button" onClick={() => void transition('Completed')} disabled={busy !== null}>Hoàn thành</button><button className="secondary-button" type="button" onClick={() => void transition('Abandoned')} disabled={busy !== null}>Bỏ goal</button></>}{(selected.goal.status === 'Completed' || selected.goal.status === 'Abandoned') && <button className="secondary-button" type="button" onClick={() => void transition('Active')} disabled={busy !== null}>Mở lại</button>}</div>{numericTarget && <form className="nested-panel" onSubmit={recordProgress} noValidate><div className="section-heading"><h3>{numericTarget.title}</h3><span className="muted">{numericTarget.currentValue} / {numericTarget.targetValue}</span></div><div className="form-grid"><div className="field-group"><label htmlFor="goal-progress-value">Current value</label><input id="goal-progress-value" type="number" step="any" value={progressValue} onChange={(event) => setProgressValue(event.target.value)} required /></div><div className="field-group"><label htmlFor="goal-progress-note">Note <span className="optional">(tùy chọn)</span></label><input id="goal-progress-note" value={progressNote} maxLength={2000} onChange={(event) => setProgressNote(event.target.value)} /></div></div><button className="primary-button" type="submit" disabled={busy !== null}>{busy === 'progress' ? 'Đang ghi…' : 'Ghi progress'}</button></form>}</div>}
      <div className="security-policy"><strong>Boundary</strong><span>Slice này chỉ có Goal CRUD, numeric target và explicit progress/status. Task-linked targets, habits, reminders, planner, history/trash và automation chưa được bật.</span></div>
    </section>
  );
}

function FinanceScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [categories, setCategories] = useState<FinanceCategoryRecord[]>([]);
  const [records, setRecords] = useState<FinanceManualRecord[]>([]);
  const [summaries, setSummaries] = useState<FinanceSummary[]>([]);
  const [filters, setFilters] = useState<FinanceFilters>({});
  const [filterDraft, setFilterDraft] = useState<FinanceFilters>({});
  const [categoryTitle, setCategoryTitle] = useState('');
  const [editingCategory, setEditingCategory] = useState<FinanceCategoryRecord | null>(null);
  const [editingRecord, setEditingRecord] = useState<FinanceManualRecord | null>(null);
  const [recordDraft, setRecordDraft] = useState<FinanceRecordDraft>({
    categoryId: '', amount: '', currencyCode: 'VND', occurredOn: todayDateInput(), note: ''
  });
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const categoryRequestKey = useRef<string | null>(null);
  const recordRequestKey = useRef<string | null>(null);

  async function load(nextFilters: FinanceFilters = filters) {
    setLoading(true);
    setError(null);
    try {
      const [categoryPage, recordPage] = await Promise.all([
        listFinanceCategories('', 100),
        listFinanceRecords({ ...nextFilters, limit: 100 })
      ]);
      const nextCategories = Array.isArray(categoryPage.items) ? categoryPage.items : [];
      setCategories(nextCategories);
      setRecords(Array.isArray(recordPage.items) ? recordPage.items : []);
      setSummaries(Array.isArray(recordPage.summaries) ? recordPage.summaries : []);
      setRecordDraft((current) => current.categoryId || nextCategories.length === 0
        ? current
        : { ...current, categoryId: nextCategories[0].id });
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, []);

  function showError(requestError: unknown) {
    const apiError = asApiError(requestError);
    setError(apiError);
    if (apiError.status === 401) void onAuthLost();
    return apiError;
  }

  function resetCategory() {
    setEditingCategory(null);
    setCategoryTitle('');
    categoryRequestKey.current = null;
    setError(null);
  }

  function beginCategoryEdit(category: FinanceCategoryRecord) {
    setEditingCategory(category);
    setCategoryTitle(category.title);
    categoryRequestKey.current = null;
    setError(null);
  }

  async function saveCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const title = categoryTitle.trim();
    if (!title) {
      setError(new NexoraApiError('Tên category là bắt buộc.', 422, 'ValidationFailed', null, { title: ['Tên category là bắt buộc.'] }));
      return;
    }
    categoryRequestKey.current ??= createIdempotencyKey();
    setBusy('category');
    setError(null);
    try {
      if (editingCategory) {
        await updateFinanceCategory(editingCategory.id, editingCategory.etag, title, categoryRequestKey.current);
      } else {
        await createFinanceCategory(title, categoryRequestKey.current);
      }
      categoryRequestKey.current = null;
      resetCategory();
      await load();
    } catch (requestError) {
      showError(requestError);
    } finally {
      setBusy(null);
    }
  }

  async function removeCategory(category: FinanceCategoryRecord) {
    if (category.usageCount > 0 || !window.confirm(`Xóa category “${category.title}”?`)) return;
    setBusy(`category:${category.id}`);
    setError(null);
    try {
      await removeFinanceCategory(category.id, category.etag);
      if (editingCategory?.id === category.id) resetCategory();
      await load();
    } catch (requestError) {
      showError(requestError);
    } finally {
      setBusy(null);
    }
  }

  function resetRecord() {
    setEditingRecord(null);
    setRecordDraft((current) => ({ categoryId: categories[0]?.id ?? '', amount: '', currencyCode: current.currencyCode || 'VND', occurredOn: todayDateInput(), note: '' }));
    recordRequestKey.current = null;
    setError(null);
  }

  function beginRecordEdit(record: FinanceManualRecord) {
    setEditingRecord(record);
    setRecordDraft({ categoryId: record.categoryId, amount: record.amount, currencyCode: record.currencyCode, occurredOn: record.occurredOn, note: record.note ?? '' });
    recordRequestKey.current = null;
    setError(null);
  }

  async function saveRecord(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!recordDraft.categoryId || !recordDraft.amount.trim() || !recordDraft.currencyCode.trim() || !recordDraft.occurredOn) {
      setError(new NexoraApiError('Category, amount, currency và ngày phát sinh là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    recordRequestKey.current ??= createIdempotencyKey();
    setBusy('record');
    setError(null);
    const input = {
      categoryId: recordDraft.categoryId,
      amount: recordDraft.amount.trim(),
      currencyCode: recordDraft.currencyCode.trim().toUpperCase(),
      occurredOn: recordDraft.occurredOn,
      note: recordDraft.note.trim() || null
    };
    try {
      if (editingRecord) {
        await updateFinanceRecord(editingRecord.id, editingRecord.etag, input, recordRequestKey.current);
      } else {
        await createFinanceRecord(input, recordRequestKey.current);
      }
      recordRequestKey.current = null;
      resetRecord();
      await load();
    } catch (requestError) {
      const apiError = showError(requestError);
      if (apiError.status === 412) await load();
    } finally {
      setBusy(null);
    }
  }

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const next: FinanceFilters = {
      categoryId: filterDraft.categoryId || undefined,
      currencyCode: filterDraft.currencyCode?.trim().toUpperCase() || undefined,
      from: filterDraft.from || undefined,
      to: filterDraft.to || undefined,
      query: filterDraft.query?.trim() || undefined
    };
    setFilters(next);
    void load(next);
  }

  function clearFilters() {
    setFilters({});
    setFilterDraft({});
    void load({});
  }

  return (
    <section className="content-section" aria-labelledby="finance-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">FX27 / FINANCE</p>
          <h1 id="finance-title">Finance records</h1>
          <p className="lead">Ghi nhận thủ công trong PersonalSpace hiện tại. Currency và số tiền được giữ nguyên theo dữ liệu SQL; chưa có ledger, thanh toán hay provider thật.</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void load()} disabled={loading || busy !== null}>Tải lại</button>
      </div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {summaries.length > 0 && <div className="info-grid" aria-label="Tổng theo currency">{summaries.map((summary) => <article className="info-card" key={summary.currencyCode}><p className="card-label">Tổng {summary.currencyCode}</p><strong>{summary.amount}</strong><span>theo bộ lọc hiện tại</span></article>)}</div>}
      {loading ? <div className="loading-state" role="status">Đang tải dữ liệu Finance…</div> : <div className="resource-layout">
        <div className="content-section">
          <form className="form-panel" onSubmit={saveCategory} noValidate>
            <div className="section-heading"><h2>{editingCategory ? 'Sửa category' : 'Category mới'}</h2>{editingCategory && <button className="link-button" type="button" onClick={resetCategory}>Hủy sửa</button>}</div>
            <div className="field-group"><label htmlFor="finance-category-title">Tên category</label><input id="finance-category-title" value={categoryTitle} maxLength={100} onChange={(event) => { setCategoryTitle(event.target.value); setError(null); }} required /><FieldError id="finance-category-title-error" message={error ? firstFieldError(error, 'title') : undefined} /></div>
            <div className="form-actions"><button className="secondary-button" type="button" onClick={resetCategory} disabled={busy === 'category'}>Làm mới</button><SubmitButton busy={busy === 'category'}>{editingCategory ? 'Lưu category' : 'Tạo category'}</SubmitButton></div>
          </form>
          <div className="resource-list"><div className="section-heading"><h2>Categories</h2><span className="muted">{categories.length} bản ghi</span></div>{categories.length === 0 ? <div className="empty-state"><h3>Chưa có category</h3><p>Tạo category trước khi ghi nhận một khoản thủ công.</p></div> : <div className="resource-cards">{categories.map((category) => <article className="resource-card" key={category.id}><div><h3>{category.title}</h3><span className="muted">{category.usageCount} record · cập nhật {dateTime(category.updatedAt)}</span></div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => beginCategoryEdit(category)} disabled={busy !== null}>Sửa</button><button className="danger-button" type="button" onClick={() => void removeCategory(category)} disabled={busy !== null || category.usageCount > 0}>{category.usageCount > 0 ? 'Đang dùng' : 'Xóa'}</button></div></article>)}</div>}</div>
        </div>
        <div className="content-section">
          <form className="form-panel" onSubmit={saveRecord} noValidate>
            <div className="section-heading"><h2>{editingRecord ? 'Sửa record' : 'Record mới'}</h2>{editingRecord && <button className="link-button" type="button" onClick={resetRecord}>Hủy sửa</button>}</div>
            <div className="field-group"><label htmlFor="finance-record-category">Category</label><select id="finance-record-category" value={recordDraft.categoryId} onChange={(event) => { setRecordDraft({ ...recordDraft, categoryId: event.target.value }); setError(null); }} disabled={categories.length === 0} required><option value="">{categories.length === 0 ? 'Tạo category trước' : 'Chọn category'}</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.title}</option>)}</select></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="finance-record-amount">Amount</label><input id="finance-record-amount" inputMode="decimal" value={recordDraft.amount} onChange={(event) => { setRecordDraft({ ...recordDraft, amount: event.target.value }); setError(null); }} placeholder="0.00" maxLength={29} required /></div><div className="field-group"><label htmlFor="finance-record-currency">Currency</label><input id="finance-record-currency" value={recordDraft.currencyCode} onChange={(event) => { setRecordDraft({ ...recordDraft, currencyCode: event.target.value.toUpperCase() }); setError(null); }} maxLength={3} required /></div></div>
            <div className="field-group"><label htmlFor="finance-record-date">Ngày phát sinh</label><input id="finance-record-date" type="date" value={recordDraft.occurredOn} onChange={(event) => setRecordDraft({ ...recordDraft, occurredOn: event.target.value })} required /></div>
            <div className="field-group"><label htmlFor="finance-record-note">Ghi chú <span className="optional">(tùy chọn)</span></label><textarea id="finance-record-note" value={recordDraft.note} maxLength={2000} onChange={(event) => setRecordDraft({ ...recordDraft, note: event.target.value })} rows={3} /></div>
            <div className="form-actions"><button className="secondary-button" type="button" onClick={resetRecord} disabled={busy === 'record'}>Làm mới</button><SubmitButton busy={busy === 'record'}>{editingRecord ? 'Lưu record' : 'Tạo record'}</SubmitButton></div>
          </form>
          <form className="form-panel" onSubmit={applyFilters} noValidate>
            <div className="section-heading"><h2>Lọc records</h2><button className="link-button" type="button" onClick={clearFilters} disabled={loading}>Xóa lọc</button></div>
            <div className="field-group"><label htmlFor="finance-filter-query">Tìm theo category hoặc ghi chú</label><input id="finance-filter-query" value={filterDraft.query ?? ''} onChange={(event) => setFilterDraft({ ...filterDraft, query: event.target.value })} /></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="finance-filter-currency">Currency</label><input id="finance-filter-currency" value={filterDraft.currencyCode ?? ''} maxLength={3} onChange={(event) => setFilterDraft({ ...filterDraft, currencyCode: event.target.value.toUpperCase() })} /></div><div className="field-group"><label htmlFor="finance-filter-category">Category</label><select id="finance-filter-category" value={filterDraft.categoryId ?? ''} onChange={(event) => setFilterDraft({ ...filterDraft, categoryId: event.target.value })}><option value="">Tất cả</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.title}</option>)}</select></div></div>
            <div className="form-grid"><div className="field-group"><label htmlFor="finance-filter-from">Từ ngày</label><input id="finance-filter-from" type="date" value={filterDraft.from ?? ''} onChange={(event) => setFilterDraft({ ...filterDraft, from: event.target.value })} /></div><div className="field-group"><label htmlFor="finance-filter-to">Đến ngày</label><input id="finance-filter-to" type="date" value={filterDraft.to ?? ''} onChange={(event) => setFilterDraft({ ...filterDraft, to: event.target.value })} /></div></div>
            <button className="secondary-button" type="submit" disabled={loading}>Áp dụng bộ lọc</button>
          </form>
          <div className="resource-list"><div className="section-heading"><h2>Records của bạn</h2><span className="muted">{records.length} bản ghi</span></div>{records.length === 0 ? <div className="empty-state"><h3>Chưa có record</h3><p>Không có khoản nào phù hợp với bộ lọc hiện tại.</p></div> : <div className="resource-cards">{records.map((record) => <article className="resource-card" key={record.id}><div><h3>{record.amount} {record.currencyCode}</h3><p>{record.categoryTitle} · {record.occurredOn}</p>{record.note && <p>{record.note}</p>}<span className="muted">Cập nhật {dateTime(record.updatedAt)}</span></div><div className="resource-actions"><button className="secondary-button" type="button" onClick={() => beginRecordEdit(record)} disabled={busy !== null}>Sửa</button></div></article>)}</div>}</div>
        </div>
      </div>}
    </section>
  );
}

function ModuleScreen({
  profile,
  module,
  navigate,
  onAuthLost
}: {
  profile: ProfileResponse;
  module?: ProfileResponse['modules'][number];
  navigate: (screen: Screen, moduleCode?: string) => void;
  onAuthLost: () => Promise<void>;
}) {
  if (!module) {
    return (
      <section className="content-section" aria-labelledby="module-missing-title">
        <p className="eyebrow">MODULE</p>
        <h1 id="module-missing-title">Module không khả dụng</h1>
        <p className="lead">Module này không có trong projection của server cho PersonalSpace hiện tại.</p>
        <button className="secondary-button" type="button" onClick={() => navigate('home')}>Về Home</button>
      </section>
    );
  }

  const normalizedCode = module.code.toUpperCase();
  if (module.enabled && (normalizedCode === 'FX11' || normalizedCode === 'FX12' || normalizedCode === 'FX13')) {
    return <ProductivityScreen profile={profile} moduleCode={normalizedCode} navigate={navigate} onAuthLost={onAuthLost} />;
  }
  if (module.enabled && normalizedCode === 'FX20') {
    return <DocumentsScreen onAuthLost={onAuthLost} />;
  }
  if (module.enabled && normalizedCode === 'FX27') {
    return <FinanceScreen onAuthLost={onAuthLost} />;
  }
  if (module.enabled && normalizedCode === 'FX21') {
    return <BookmarksScreen onAuthLost={onAuthLost} />;
  }
  if (module.enabled && normalizedCode === 'FX22') {
    return <SnippetsScreen onAuthLost={onAuthLost} />;
  }
  if (module.enabled && normalizedCode === 'FX23') {
    return <ReadLaterScreen onAuthLost={onAuthLost} />;
  }
  if (module.enabled && normalizedCode === 'FX24') {
    return <OrganizationTagsScreen onAuthLost={onAuthLost} />;
  }
  if (module.enabled && normalizedCode === 'FX32') {
    return <DeveloperToolsScreen onAuthLost={onAuthLost} />;
  }
  if (module.enabled && normalizedCode === 'FX16') {
    return <GoalsScreen onAuthLost={onAuthLost} />;
  }

  return (
    <section className="content-section" aria-labelledby="module-title">
      <div className="content-heading">
        <div>
          <p className="eyebrow">{module.code}</p>
          <h1 id="module-title">Module entry</h1>
          <p className="lead">Server đã cấp module này cho {profile.displayName}; UI feature CRUD của module này chưa nằm trong slice frontend hiện tại.</p>
        </div>
        <span className="state-pill state-active">{module.enabled ? 'Available' : 'Unavailable'}</span>
      </div>
      <div className="empty-state honest-placeholder">
        <h2>Chưa có dữ liệu hiển thị</h2>
        <p>Không dùng demo data hoặc client-side mock. Module này sẽ được nối vào API owner-scoped sau khi backend contract tương ứng được triển khai.</p>
        <button className="secondary-button" type="button" onClick={() => navigate('home')}>Về Home</button>
      </div>
    </section>
  );
}

function ProfileScreen({
  profile,
  onProfileUpdated,
  onAuthLost
}: {
  profile: ProfileResponse;
  onProfileUpdated: (profile: ProfileResponse) => void;
  onAuthLost: () => Promise<void>;
}) {
  const [draft, setDraft] = useState<ProfilePatch>({ displayName: profile.displayName, timeZoneId: profile.timeZoneId, locale: profile.locale });
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const [conflict, setConflict] = useState(false);
  const requestKey = useRef<string | null>(null);

  useEffect(() => {
    setDraft({ displayName: profile.displayName, timeZoneId: profile.timeZoneId, locale: profile.locale });
    setConflict(false);
  }, [profile]);

  function changeDraft(patch: ProfilePatch) {
    requestKey.current = null;
    setDraft((current) => ({ ...current, ...patch }));
  }

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const latest = normalizeProfile(await getMe());
      onProfileUpdated(latest);
      setDraft({ displayName: latest.displayName, timeZoneId: latest.timeZoneId, locale: latest.locale });
      setConflict(false);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) {
        await onAuthLost();
      }
    } finally {
      setLoading(false);
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setConflict(false);
    const displayName = (draft.displayName ?? '').trim();
    const timeZoneId = (draft.timeZoneId ?? '').trim();
    if (!displayName || displayName.length > 100) {
      setError(new NexoraApiError('Tên hiển thị phải từ 1 đến 100 ký tự.', 422, 'ValidationFailed'));
      return;
    }
    if (!timeZoneId) {
      setError(new NexoraApiError('Timezone IANA là bắt buộc.', 422, 'ValidationFailed'));
      return;
    }
    requestKey.current ??= createIdempotencyKey();
    setBusy(true);
    try {
      const updated = normalizeProfile(await updateMe({ displayName, timeZoneId, locale: draft.locale }, requestKey.current));
      requestKey.current = null;
      onProfileUpdated(updated);
      setDraft({ displayName: updated.displayName, timeZoneId: updated.timeZoneId, locale: updated.locale });
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 412) {
        setConflict(true);
      } else if (apiError.status === 401) {
        await onAuthLost();
      }
    } finally {
      setBusy(false);
    }
  }

  const changed = draft.displayName !== profile.displayName || draft.timeZoneId !== profile.timeZoneId || draft.locale !== profile.locale;
  return (
    <section className="content-section" aria-labelledby="profile-title">
      <div className="content-heading"><div><p className="eyebrow">SETTINGS / PROFILE</p><h1 id="profile-title">Profile</h1><p className="lead">Chỉ các trường profile được phép mới có thể cập nhật; UserId, OwnerId, role và state luôn do server quản lý.</p></div><span className="state-pill state-active">{profile.state}</span></div>
      {conflict && (
        <Notice kind="error">
          <span>Dữ liệu profile đã thay đổi ở tab hoặc session khác. Draft hiện tại vẫn được giữ trong memory.</span>
          <button className="inline-button" type="button" onClick={reload} disabled={loading}>{loading ? 'Đang tải…' : 'Reload revision'}</button>
        </Notice>
      )}
      <form className="profile-form" onSubmit={submit} noValidate>
        <div className="form-panel">
          <div className="field-group"><label htmlFor="profile-email">Email</label><input id="profile-email" type="email" value={profile.email} readOnly aria-describedby="profile-email-help" /><p className="field-help" id="profile-email-help">Đổi email là flow xác minh riêng và không có trong màn hình này.</p></div>
          <div className="field-group"><label htmlFor="profile-display-name">Tên hiển thị</label><input id="profile-display-name" type="text" maxLength={100} value={draft.displayName ?? ''} onChange={(event) => changeDraft({ displayName: event.target.value })} required aria-describedby="profile-display-name-error" /><FieldError id="profile-display-name-error" message={error ? firstFieldError(error, 'displayName') : undefined} /></div>
          <div className="field-group"><label htmlFor="profile-timezone">Timezone IANA</label><input id="profile-timezone" type="text" value={draft.timeZoneId ?? ''} onChange={(event) => changeDraft({ timeZoneId: event.target.value })} required aria-describedby="profile-timezone-error" /><FieldError id="profile-timezone-error" message={error ? firstFieldError(error, 'timeZoneId') : undefined} /></div>
          <div className="field-group"><label htmlFor="profile-locale">Ngôn ngữ giao diện</label><select id="profile-locale" value={draft.locale ?? profile.locale} onChange={(event) => changeDraft({ locale: event.target.value as 'vi' | 'en' })}><option value="vi">Tiếng Việt</option><option value="en">English</option></select><FieldError id="profile-locale-error" message={error ? firstFieldError(error, 'locale') : undefined} /></div>
          {error && !conflict && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
          <div className="form-actions"><button className="secondary-button" type="button" onClick={() => setDraft({ displayName: profile.displayName, timeZoneId: profile.timeZoneId, locale: profile.locale })} disabled={!changed || busy}>Hủy thay đổi</button><SubmitButton busy={busy}>Lưu profile</SubmitButton></div>
        </div>
      </form>
      <PreferencesPanel onAuthLost={onAuthLost} />
    </section>
  );
}

function PreferencesPanel({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [preference, setPreference] = useState<PreferenceRecord | null>(null);
  const [mode, setMode] = useState<'System' | 'Light' | 'Dark'>('System');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const page = await listPreferences();
      const theme = page.items.find((item) => item.preferenceKey === 'theme') ?? null;
      setPreference(theme);
      if (theme) {
        try {
          const value = JSON.parse(theme.valueJson) as { mode?: string };
          if (value.mode === 'System' || value.mode === 'Light' || value.mode === 'Dark') setMode(value.mode);
        } catch {
          setError(new NexoraApiError('Theme preference không hợp lệ.', 422, 'ValidationFailed'));
        }
      }
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, []);

  async function save() {
    setBusy(true);
    setError(null);
    try {
      const saved = await updatePreference('theme', preference?.etag ?? '*', { mode });
      setPreference(saved);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) await onAuthLost();
      if (apiError.status === 412) await load();
    } finally {
      setBusy(false);
    }
  }

  return <section className="settings-panel" aria-labelledby="preferences-title"><div className="section-heading"><div><h2 id="preferences-title">Preferences</h2><p className="muted">Schema-validated, non-secret settings; không có mute/quiet-hours/channel suppression.</p></div><button className="secondary-button" type="button" onClick={load} disabled={loading}>Tải lại</button></div>{error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}<div className="form-grid"><div className="field-group"><label htmlFor="theme-mode">Theme</label><select id="theme-mode" value={mode} onChange={(event) => setMode(event.target.value as 'System' | 'Light' | 'Dark')} disabled={loading}><option value="System">System</option><option value="Light">Light</option><option value="Dark">Dark</option></select></div></div><div className="form-actions"><button className="primary-button" type="button" onClick={() => void save()} disabled={busy || loading}>{busy ? 'Đang lưu…' : 'Lưu preferences'}</button></div></section>;
}

function SecurityScreen({ onAuthLost }: { onAuthLost: () => Promise<void> }) {
  const [sessions, setSessions] = useState<SessionProjection[]>([]);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [confirmAll, setConfirmAll] = useState(false);
  const [error, setError] = useState<NexoraApiError | null>(null);
  const actionKeys = useRef<Record<string, string>>({});

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const page = await listSessions();
      setSessions(Array.isArray(page.items) ? page.items : []);
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) {
        await onAuthLost();
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function revoke(session: SessionProjection) {
    setBusyId(session.id);
    setError(null);
    actionKeys.current[session.id] ??= createIdempotencyKey();
    try {
      await revokeSession(session.id, actionKeys.current[session.id]);
      delete actionKeys.current[session.id];
      setConfirmingId(null);
      if (session.isCurrent) {
        await onAuthLost();
        return;
      }
      await load();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) {
        await onAuthLost();
      }
    } finally {
      setBusyId(null);
    }
  }

  async function revokeEverywhere() {
    setBusyId('all');
    setError(null);
    actionKeys.current.all ??= createIdempotencyKey();
    try {
      await revokeAllSessions(actionKeys.current.all);
      delete actionKeys.current.all;
      await onAuthLost();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      setError(apiError);
      if (apiError.status === 401) {
        await onAuthLost();
      }
    } finally {
      setBusyId(null);
    }
  }

  return (
    <section className="content-section" aria-labelledby="security-title">
      <div className="content-heading"><div><p className="eyebrow">SETTINGS / SECURITY</p><h1 id="security-title">Security & sessions</h1><p className="lead">Session metadata được server projection an toàn; raw cookie/token không được render.</p></div><button className="secondary-button" type="button" onClick={load} disabled={loading}>{loading ? 'Đang tải…' : 'Tải lại'}</button></div>
      <div className="security-policy"><strong>MFA và recovery</strong><span>Chính sách có thể yêu cầu flow riêng; màn hình này không giả lập hoặc bỏ qua step-up.</span></div>
      {error && <Notice kind="error">{error.message}{error.traceId ? ` (trace ${error.traceId})` : ''}</Notice>}
      {loading ? <div className="loading-state" role="status">Đang tải session…</div> : sessions.length === 0 ? <div className="empty-state"><h2>Không có session hiển thị</h2><p>Server không trả session active nào cho account hiện tại.</p></div> : <div className="table-wrap"><table><caption>Danh sách session của account hiện tại</caption><thead><tr><th scope="col">Thiết bị</th><th scope="col">Hoạt động gần nhất</th><th scope="col">Hết hạn</th><th scope="col"><span className="sr-only">Thao tác</span></th></tr></thead><tbody>{sessions.map((session) => <tr key={session.id}><td><strong>{session.deviceLabel}</strong>{session.isCurrent && <span className="current-label">Session hiện tại</span>}<span className="muted">Tạo {dateTime(session.createdAt)}</span></td><td>{dateTime(session.lastSeenAt)}</td><td>{dateTime(session.expiresAt)}</td><td className="table-action-cell">{confirmingId === session.id ? <div className="confirm-actions"><span>Thu hồi session này?</span><button className="danger-button" type="button" onClick={() => revoke(session)} disabled={busyId === session.id}>{busyId === session.id ? 'Đang thu hồi…' : 'Xác nhận'}</button><button className="link-button" type="button" onClick={() => setConfirmingId(null)} disabled={busyId === session.id}>Hủy</button></div> : <button className="secondary-button" type="button" onClick={() => setConfirmingId(session.id)} disabled={busyId !== null}>Thu hồi</button>}</td></tr>)}</tbody></table></div>}
      <div className="danger-zone"><div><h2>Thu hồi tất cả session</h2><p>Thao tác này bao gồm session hiện tại và sẽ đưa bạn về màn hình đăng nhập.</p></div>{confirmAll ? <div className="confirm-actions"><span>Thu hồi tất cả?</span><button className="danger-button" type="button" onClick={revokeEverywhere} disabled={busyId === 'all'}>{busyId === 'all' ? 'Đang thu hồi…' : 'Xác nhận'}</button><button className="link-button" type="button" onClick={() => setConfirmAll(false)} disabled={busyId === 'all'}>Hủy</button></div> : <button className="danger-button" type="button" onClick={() => setConfirmAll(true)} disabled={busyId !== null || loading}>Thu hồi tất cả</button>}</div>
    </section>
  );
}

export function App() {
  const [location, setLocation] = useState<LocationState>(() => routeFromPath(window.location.pathname));
  const [sessionState, setSessionState] = useState<SessionState>('checking');
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [verificationEmail, setVerificationEmail] = useState('');
  const [notice, setNotice] = useState<{ kind: NoticeKind; text: string }>();

  function navigate(screen: Screen, moduleCode?: string, replace = false) {
    const next: LocationState = { screen, moduleCode };
    const nextPath = pathForLocation(next);
    if (window.location.pathname !== nextPath) {
      if (replace) {
        window.history.replaceState({}, '', nextPath);
      } else {
        window.history.pushState({}, '', nextPath);
      }
    }
    setLocation(next);
    setNotice(undefined);
  }

  useEffect(() => {
    const onPopState = () => setLocation(routeFromPath(window.location.pathname));
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
  }, []);

  useEffect(() => {
    let cancelled = false;
    async function bootstrap() {
      try {
        await getCsrf();
        const current = normalizeProfile(await getMe());
        if (!cancelled) {
          setProfile(current);
          setSessionState('authenticated');
        }
      } catch (requestError) {
        if (cancelled) {
          return;
        }
        const apiError = asApiError(requestError);
        if (apiError.status === 401) {
          setProfile(null);
          setSessionState('anonymous');
        } else {
          setSessionState('unavailable');
          setNotice({ kind: 'error', text: apiError.message });
        }
      }
    }
    void bootstrap();
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (sessionState === 'anonymous' && !PUBLIC_SCREENS.has(location.screen)) {
      navigate('login', undefined, true);
    }
    if (sessionState === 'authenticated' && PUBLIC_SCREENS.has(location.screen)) {
      navigate('home', undefined, true);
    }
  }, [sessionState, location.screen]);

  function authenticate(nextProfile: ProfileResponse) {
    setProfile(nextProfile);
    setSessionState('authenticated');
    navigate('home', undefined, true);
  }

  async function clearSession(message?: string) {
    clearProfileRevision();
    setProfile(null);
    setSessionState('anonymous');
    navigate('login', undefined, true);
    if (message) {
      setNotice({ kind: 'info', text: message });
    }
  }

  async function handleLogout() {
    try {
      await logout();
      await clearSession();
    } catch (requestError) {
      const apiError = asApiError(requestError);
      if (apiError.status === 401) {
        await clearSession('Phiên đã hết hạn và đã được xóa khỏi trình duyệt.');
      } else {
        setNotice({ kind: 'error', text: apiError.message });
      }
    }
  }

  if (sessionState === 'checking') {
    return <div className="full-page-state" role="status"><div className="loading-mark" aria-hidden="true">N</div><h1>Đang kiểm tra phiên</h1><p>Không hiển thị dữ liệu private trước khi server xác nhận session.</p></div>;
  }

  if (sessionState === 'unavailable') {
    return <div className="full-page-state"><div className="loading-mark" aria-hidden="true">!</div><h1>Local API chưa sẵn sàng</h1><p>{notice?.text ?? 'Không thể kết nối Nexora API.'}</p><button className="primary-button" type="button" onClick={() => window.location.reload()}>Thử lại</button></div>;
  }

  if (sessionState === 'authenticated' && profile) {
    return <Shell profile={profile} location={location} navigate={navigate} onLogout={handleLogout} onProfileUpdated={setProfile} onAuthLost={() => clearSession('Phiên đã hết hạn. Vui lòng đăng nhập lại.')} notice={notice} onDismissNotice={() => setNotice(undefined)} />;
  }

  const publicProps = { navigate, notice, onDismissNotice: () => setNotice(undefined) };
  switch (location.screen) {
    case 'register':
      return <RegisterScreen {...publicProps} onRegistered={(email) => { setVerificationEmail(email); setNotice({ kind: 'success', text: 'Đã tiếp nhận đăng ký. Nhập mã từ kênh transport đã cấu hình để xác minh.' }); navigate('verify'); }} />;
    case 'verify':
      return <VerifyScreen {...publicProps} email={verificationEmail} setEmail={setVerificationEmail} onVerified={() => setNotice({ kind: 'success', text: 'Email đã được xác minh. Đăng nhập để tiếp tục.' })} />;
    case 'forgot':
      return <ForgotPasswordScreen {...publicProps} />;
    case 'reset':
      return <ResetPasswordScreen {...publicProps} />;
    case 'home':
    case 'profile':
    case 'security':
    case 'finance':
    case 'goals':
    case 'module':
    case 'login':
    default:
      return <LoginScreen {...publicProps} onAuthenticated={authenticate} onPendingVerification={(email) => { setVerificationEmail(email); navigate('verify'); }} />;
  }
}
