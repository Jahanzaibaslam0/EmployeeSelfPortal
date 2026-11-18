# Account Settings — internal deployment

This module is local to IIS + SQL Server. It does not call email, SMS, or cloud identity providers. TOTP codes are generated on the user’s phone from a shared secret stored in `tblUser`.

## Pages

| URL | Access |
|-----|--------|
| `/AccountSettings.aspx` | Signed-in user, own account only |
| `/ForgotPassword.aspx` | Public |
| `/Verify2FA.aspx` | Public, only after password step |
| `/UserSetup.aspx` | Admin-assisted reset, disable 2FA, clear lockout |

Employee HR data remains on **My Profile** (`/UserProfile.aspx`).

## Database

On application start, `AccountSecurityService.EnsureSchema()` adds columns/tables. If the SQL login cannot `ALTER`/`CREATE`, a DBA must run:

`Database/Production_AccountSecurity_Patch.sql`

New `tblUser` columns: `TwoFactorEnabled`, `TwoFactorSecret`, `TwoFactorConfirmedOn`, `MustChangePassword`, `FailedLoginCount`, `LockoutUntil`, `PasswordChangedOn`, `SecurityStamp`.

Tables: `tblUserRecoveryCode`, `tblUserSecurityQuestion`, `tblAuthThrottle`.

Passwords and recovery answers/codes are stored as PBKDF2 hashes (`PasswordHelper`). TOTP secrets are stored for verification only (required for offline authenticators).

## Configuration

No new connection strings or internet APIs. Optional `web.config`:

- `sessionState` cookie name `HRMS_Session`, `httpCookies httpOnlyCookies="true"`.
- If the site is served over HTTPS internally, add `requireSSL="true"` on `httpCookies`.

Libraries: none. TOTP is implemented in `TotpHelper.cs` (RFC 6238, HMAC-SHA1, 30 seconds, 6 digits).

Keep IIS/SQL and phones reasonably time-synced (domain NTP). A ±1 step window is allowed.

## First-time admin password

`EnsureUserTableAndAdmin` **inserts** `admin` / `Admin@123` only when that username does not exist. It no longer overwrites an existing admin hash on every startup.

Emergency reset remains `ResetAdmin.aspx` (restrict this URL on production).

## Testing (isolated server)

1. Sign in → **Account** in the header → change display name; confirm username is read-only.
2. Change password: wrong current password fails; weak password fails; success then sign in with the new password. Other browsers should drop out after stamp change on next request.
3. Set two recovery questions (current password required).
4. Enable 2FA: enter current password → add setup key in Microsoft/Google Authenticator **offline** → confirm 6-digit code → **save backup codes**.
5. Sign out. Sign in → authenticator page → wrong code fails; correct code enters Home.
6. Sign in with a backup code instead of TOTP (code becomes used).
7. Forgot password: unknown username still shows questions (no account enumeration). Real user: correct answers or unused backup code → set new password. No email is sent.
8. Five bad passwords: further attempts stay generic “Invalid username or password.” Wait 15 minutes or **User Setup → Clear lockout**.
9. **User Setup**: reset password (user must change at next sign-in); **Disable 2FA**.
10. Confirm phones in airplane mode still produce valid TOTP codes.

## Operations

- Lost phone: backup codes, recovery questions, or admin disable 2FA + password reset.
- Do not expose `ResetAdmin.aspx` on the public LAN without extra restriction.
- Audit actions: `PasswordChanged`, `TwoFactorEnabled`, `TwoFactorDisabled`, `PasswordRecovered`, `Login` / `LoginFailed` (Audit Log Report).
