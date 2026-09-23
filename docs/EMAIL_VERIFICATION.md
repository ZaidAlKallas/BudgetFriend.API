# Email Verification — Usage Guide

This guide describes how email verification works in the BudgetFriend API using a
**6-digit verification code**, and how the surrounding security controls behave.

---

## 1. How it works (overview)

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

Verification is a **server-side** operation. It works identically on web,
mobile, and API clients because the user types the code themselves — there is no
link to intercept or deep-link into the app.

---

## 2. Configuration

The only configuration relevant to email verification is the SMTP/provider
settings under the `Email` section (`EmailOptions`). Code-based verification does
**not** use `Email:BaseUrl` or `Email:DeepLinkBaseUrl`.

Those two settings now apply **only** to password-reset emails (`/reset-password`),
which remain link-based:

| Setting | Purpose |
| --- | --- |
| `Email:BaseUrl` | Web frontend origin used for password-reset links. |
| `Email:DeepLinkBaseUrl` | Optional mobile deep-link origin; preferred over `BaseUrl` when set. |

Tunable security values live in
`src/Features/Authentication/EmailVerification/EmailVerificationDefaults.cs`:

| Constant | Value | Meaning |
| --- | --- | --- |
| `CodeLength` | `6` | Number of digits in the verification code. |
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

---

## 4. Security characteristics

- **Cryptographic randomness**: codes come from `RandomNumberGenerator`
  (`SecurityTokens.GenerateNumericCode`), not a seeded PRNG.
- **Hashed at rest**: only the SHA-256 hash of the code is stored; the plaintext
  code is never persisted and is only present in the outbound email.
- **Expiry**: each code is valid for 15 minutes (`EmailVerificationDefaults.Expiry`).
- **Brute-force protection (two layers)**:
  1. Per-account attempt counter — after 5 wrong attempts the code is
     invalidated (`EmailVerificationDefaults.MaxAttempts`, `VerifyEmailEndpoint`).
  2. IP-level fixed-window rate limiting on `/verify-email`
     (`VerifyEmailPolicy`, `ServiceCollectionExtensions`.
- **Resend throttling**: `/resend-verification` is limited by `EmailPolicy`
  (5 requests / 10 minutes by default).
- **Single-use**: a used or replaced code is cleared from the database, so
  replaying it fails.
- **No user enumeration**: resend always returns the same generic message, and
  invalid codes return the same generic error for unknown users and verified
  accounts.

---

## 5. Database

The `User` entity stores (see `src/Database/Entities/User.cs`):

| Column | Purpose |
| --- | --- |
| `EmailVerificationCodeHash` | SHA-256 hash of the active code. |
| `EmailVerificationCodeExpiresAtUtc` | When the code stops being valid. |
| `EmailVerificationAttemptCount` | Failed attempts against the current code. |

The schema change is shipped by the
`ConvertEmailVerificationToSixDigitCode` EF migration (renames the old token
columns and adds the attempt counter).

---

## 6. Testing

- Unit tests:
  - `test/BudgetFriend.API.UnitTests/Authentication/SecurityTokensTests.cs` —
    code format, custom length, randomness.
  - `test/BudgetFriend.API.UnitTests/Authentication/AuthEmailBuilderTests.cs` —
    code message contains the code and no links.
  - `test/BudgetFriend.API.UnitTests/Validators/VerifyEmailValidatorTests.cs` —
    email/code shape validation.
- Integration tests:
  - `test/BudgetFriend.API.IntegrationTests/Auth/EmailVerificationTests.cs` —
    registration email, valid/invalid/expired codes, lockout after max attempts,
    single-use, resend semantics.
