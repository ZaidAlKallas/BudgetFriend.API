# Email Verification & Password Reset — Usage Guide

This guide describes how email verification and password reset work in the
BudgetFriend API using **6-digit codes sent by email**, and how the surrounding
security controls behave.

---

## 1. How it works (overview)

### Email verification

```
 User registers / requests a new code
        │
        ▼
 ┌───────────────────────────────────────┐
 │ POST /api/v1/auth/register            │
 │ POST /api/v1/auth/resend-verification │
 └──────────────┬────────────────────────┘
                │  generates a random 6-digit code
                │  stores its SHA-256 hash + expiry (15 min)
                │  resets the failed-attempt counter
                ▼
        Email with the code
        (no link is used)
                │
                ▼
   POST /api/v1/auth/verify-email
   { "email": "...", "code": "123456" }
                │
                ▼
   Server: validates shape → checks attempts → checks expiry
           → compares hash → marks IsEmailVerified = true
```

### Password reset

```
 POST /api/v1/auth/forgot-password            POST /api/v1/auth/reset-password
 { "email": "..." }                    │      { "email":"...", "code":"123456",
        │                               │        "newPassword":"..." }
        │ generates a random 6-digit    │
        │ code, stores hash + expiry    │
        ▼                               │
        Email with the code             │
        │                               ▼
        └──────────────────────────────> Server: validates shape → checks attempts
                                          → checks expiry → compares hash
                                          → hashes new password, revokes sessions
```

Verification and password reset are **server-side** operations. They work
identically on web, mobile, and API clients because the user types the code
themselves — there is no link to intercept or deep-link into the app.

---

## 2. Configuration

The only configuration relevant to these flows is the SMTP/provider settings
under the `Email` section (`EmailOptions`). Code-based flows do **not** use any
frontend/website URLs. `Email:BaseUrl` and `Email:DeepLinkBaseUrl` no longer
exist.

Tunable security values live in
`src/Features/Authentication/EmailVerification/EmailVerificationDefaults.cs`
and `src/Features/Authentication/PasswordReset/PasswordResetDefaults.cs`:

| Constant | Value | Meaning |
| --- | --- | --- |
| `CodeLength` | `6` | Number of digits in the code. |
| `MaxAttempts` | `5` | Failed attempts allowed before the code is invalidated. |
| `Expiry` | `15 minutes` | Validity window of a code. |

---

## 3. Endpoints

### `POST /api/v1/auth/register`
Creates an account, generates a 6-digit code, stores its hash + expiry, and sends
the code by email.

| Response | Meaning |
| --- | --- |
| `201 Created` | Account created; code emailed. |
| `409 Conflict` | A user with this email already exists. |
| `400 Bad Request` | Invalid payload. |

### `POST /api/v1/auth/verify-email`
Body:
```json
{ "email": "user@example.com", "code": "123456" }
```

Rate-limited by the `VerifyEmailPolicy` limiter (10 requests/minute by default).

| Response | Meaning |
| --- | --- |
| `200 OK` | Email marked verified; code and counter cleared. |
| `400 Bad Request` | Invalid/expired code, or too many failed attempts. |
| `429 Too Many Requests` | Rate limit exceeded. |

Failure counting (per account):

1. Wrong code → `EmailVerificationAttemptCount` is incremented, and the response
   reports how many attempts remain.
2. When the counter reaches `MaxAttempts` (5) the code is **immediately
   invalidated** and the user must request a new one.
3. An expired code is cleared on submission and returns an expiry message.

### `POST /api/v1/auth/resend-verification`
Body:
```json
{ "email": "user@example.com" }
```

Always returns the same generic success message (does not reveal whether an
account exists). Rate-limited by the `EmailPolicy` limiter.

Generates a fresh code: the previous code is replaced, its expiry is reset to
15 minutes, and the failed-attempt counter is reset to zero.

### `POST /api/v1/auth/forgot-password`
Body:
```json
{ "email": "user@example.com" }
```

Always returns the same generic success message (does not reveal whether an
account exists). Rate-limited by the `EmailPolicy` limiter.

Generates a 6-digit code, stores its hash + expiry, resets the failed-attempt
counter, and emails the code.

### `POST /api/v1/auth/reset-password`
Body:
```json
{ "email": "user@example.com", "code": "123456", "newPassword": "NewPassword1!" }
```

Rate-limited by the `ResetPasswordPolicy` limiter (10 requests/minute by default).

| Response | Meaning |
| --- | --- |
| `200 OK` | Password changed; reset code and counter cleared; all sessions revoked. |
| `400 Bad Request` | Invalid/expired code, or too many failed attempts. |
| `429 Too Many Requests` | Rate limit exceeded. |

Failure counting (per account) is identical to email verification:

1. Wrong code → `PasswordResetAttemptCount` is incremented, and the response
   reports how many attempts remain.
2. When the counter reaches `MaxAttempts` (5) the code is **immediately
   invalidated** and the user must request a new one.
3. An expired code is cleared on submission and returns an expiry message.

---

## 4. Security characteristics

- **Cryptographic randomness**: codes come from `RandomNumberGenerator`
  (`SecurityTokens.GenerateNumericCode`), not a seeded PRNG.
- **Hashed at rest**: only the SHA-256 hash of a code is stored; the plaintext
  code is never persisted and is only present in the outbound email.
- **Expiry**: each code is valid for 15 minutes.
- **Brute-force protection (two layers)**:
  1. Per-account attempt counter — after 5 wrong attempts the code is
     invalidated (`MaxAttempts`).
  2. IP-level fixed-window rate limiting on `/verify-email`
     (`VerifyEmailPolicy`) and `/reset-password` (`ResetPasswordPolicy`).
- **Resend/Delivery throttling**: `/resend-verification` and
  `/forgot-password` are limited by `EmailPolicy` (5 requests / 10 minutes by
  default).
- **Single-use**: a used or replaced code is cleared from the database, so
  replaying it fails.
- **No user enumeration**: resend and forgot-password always return the same
  generic message, and invalid codes return the same generic error for unknown
  users.
- **Session revoke**: a successful password reset revokes every refresh token
  belonging to the account.

---

## 5. Database

The `User` entity stores (see `src/Database/Entities/User.cs`):

| Column | Purpose |
| --- | --- |
| `EmailVerificationCodeHash` | SHA-256 hash of the active verification code. |
| `EmailVerificationCodeExpiresAtUtc` | When the verification code stops being valid. |
| `EmailVerificationAttemptCount` | Failed attempts against the verification code. |
| `PasswordResetCodeHash` | SHA-256 hash of the active reset code. |
| `PasswordResetCodeExpiresAtUtc` | When the reset code stops being valid. |
| `PasswordResetAttemptCount` | Failed attempts against the reset code. |

The schema changes are shipped by the `ConvertEmailVerificationToSixDigitCode`
and `ConvertPasswordResetToSixDigitCode` EF migrations (they rename the old
token columns and add the attempt counters).

---

## 6. Testing

- Unit tests:
  - `test/BudgetFriend.API.UnitTests/Authentication/SecurityTokensTests.cs` —
    code format, custom length, randomness.
  - `test/BudgetFriend.API.UnitTests/Authentication/AuthEmailBuilderTests.cs` —
    code messages contain the code and no links.
  - `test/BudgetFriend.API.UnitTests/Validators/VerifyEmailValidatorTests.cs` —
    email/code shape validation.
  - `test/BudgetFriend.API.UnitTests/Validators/ResetPasswordValidatorTests.cs` —
    email/code/password shape validation.
- Integration tests:
  - `test/BudgetFriend.API.IntegrationTests/Auth/EmailVerificationTests.cs` —
    registration email, valid/invalid/expired codes, lockout after max attempts,
    single-use, resend semantics.
  - `test/BudgetFriend.API.IntegrationTests/Auth/PasswordResetTests.cs` —
    forgot-password email, valid/invalid/expired codes, lockout, single-use,
    rejected weak passwords, session revocation.
