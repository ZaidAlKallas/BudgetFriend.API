# Email Verification & Deep Links — Usage Guide

This guide describes how email-based verification works in the BudgetFriend API, how the links are built, and what is required to make the flow work for both web browsers and mobile apps.

---

## 1. How it works (overview)

```
 User registers / requests re-verification
        │
        ▼
 ┌────────────────────────────────────┐
 │ /api/v1/auth/register              │
 │ /api/v1/auth/resend-verification   │
 └──────────────┬─────────────────────┘
                │  generates a single-use token
                │  stores only its SHA-256 hash + expiry
                ▼
        Email with a link
        (built by AuthEmailBuilder)
                │
        ┌───────┴────────┐
        ▼                ▼
   Web frontend      Mobile app
   (browser)         (deep link)
        │                │
        └───────┬────────┘
                ▼
   POST /api/v1/auth/verify-email { "token": "..." }
                │
                ▼
   Server verifies hash + expiry, marks IsEmailVerified = true
```

Verification is a **server-side** operation. It works identically regardless of
whether the link is opened in a browser or delivered to the native mobile app —
the endpoint is stateless with respect to the device and does **not** require an
authenticated session.

---

## 2. Configuration

All settings live in the `Email` section (`EmailOptions`, `src/Shared/Email/EmailOptions.cs`).

| Setting | Example | Purpose |
| --- | --- | --- |
| `Email:BaseUrl` | `https://budgetfriend.example.com` | Web frontend origin. Used when `DeepLinkBaseUrl` is empty. |
| `Email:DeepLinkBaseUrl` | `https://budgetfriend.app` | Dedicated deep-link origin (mobile). **Preferred** when set. |

Link-building precedence (`AuthEmailBuilder.BuildLink`):

1. If `Email:DeepLinkBaseUrl` is set, all email links use it.
2. Otherwise `Email:BaseUrl` is used.

This means the API can point the email at a mobile-friendly URL (Universal/App
Links) without changing the web frontend URL.

Example:

```json
"Email": {
  "BaseUrl": "https://budgetfriend.example.com",
  "DeepLinkBaseUrl": "https://budgetfriend.app",
  "Authorization": "re_xxxxxxxxx"
}
```

---

## 3. Link format

```
{BaseUrl | DeepLinkBaseUrl}/verify-email?token=<url-encoded-token>
{BaseUrl | DeepLinkBaseUrl}/reset-password?token=<url-encoded-token>
```

The token is always URL-encoded (`Uri.EscapeDataString`) before being placed in
the query string.

Generated links:

- Email verification -> `/verify-email?token=...`
- Password reset    -> `/reset-password?token=...`

---

## 4. Endpoints

### `POST /api/v1/auth/register`
Creates an account, generates a verification token, stores its hash, and sends
the verification email.

| Response | Meaning |
| --- | --- |
| `201 Created` | Account created; verification email sent. |
| `409 Conflict` | A user with this email already exists. |

### `POST /api/v1/auth/verify-email`
Body:
```json
{ "token": "RWd_2eMq_p87p4qhy7904uOXJ8E5CW1UV5OvMgasXfw" }
```

| Response | Meaning |
| --- | --- |
| `200 OK` | Email marked verified; token cleared. |
| `400 Bad Request` | Token invalid, already used, or expired. |

The token is matched by its SHA-256 hash, so plaintext tokens are never stored.

### `POST /api/v1/auth/resend-verification`
Body:
```json
{ "email": "user@example.com" }
```

Always returns the same generic success message (does not reveal whether an
account exists). Rate-limited by the `EmailPolicy` limiter.

### `POST /api/v1/auth/forgot-password` / `POST /api/v1/auth/reset-password`
Password reset uses the same link-builder and token mechanics (`/reset-password`),
with a 1-hour expiry and session revocation after a successful reset.

---

## 5. Browser behavior

1. User clicks the link in the email.
2. The browser opens `https://<frontend>/verify-email?token=...`.
3. The web page reads the `token` query parameter and calls
   `POST /api/v1/auth/verify-email`.
4. The server verifies the hash + expiry and marks the account verified.
5. The account passes verification on all subsequent requests — including the
   native app, which picks up `IsEmailVerified` on its next profile/session refresh.

> Requirements: the web frontend must expose a `/verify-email` route (and
> `/reset-password`) that consumes the `token` query parameter and calls the API.
> Verification does not require the visitor to be logged in.

---

## 6. Mobile behavior (deep links)

For the link to open the **native app** instead of the browser, configure the
`Email:DeepLinkBaseUrl` origin for platform deep linking:

| Platform | Mechanism | Requirement on the app |
| --- | --- | --- |
| iOS | Universal Links | `apple-app-site-association` served at `https://budgetfriend.app/.well-known/` and the domain associated in the app's entitlements. |
| Android | App Links | `assetlinks.json` served at `https://budgetfriend.app/.well-known/` and the intent filter declared in the app's manifest. |

Required by both: the two endpoints of the deep link (`/verify-email`,
`/reset-password`) must be registered in the app.

**Fallback behavior:** if the app is not installed, the OS opens the browser at
the same URL (the deep-link origin should also serve the web flow). This is why
the deep-link base URL should be a real, served origin.

In the app, read the `token` query parameter from the incoming URL and call
`POST /api/v1/auth/verify-email` with it. No authentication token is needed.

---

## 7. Security characteristics

- **High-entropy tokens**: 256-bit random (`SecurityTokens.Generate`), not guessable.
- **Hashed at rest**: only SHA-256 hashes are stored; plaintext is never stored
  and cannot be recovered from the database.
- **Single-use**: consumed tokens are cleared (`EmailVerificationTokenHash = null`),
  so replaying the same link fails.
- **Expiry**: verification links expire after 24 hours; a new one is generated by
  `/resend-verification` and invalidates the previous token.
- **Rate limiting**: `/resend-verification` and `/forgot-password` use the
  `EmailPolicy` limiter.

---

## 8. Testing

- Unit tests: `test/BudgetFriend.API.UnitTests/Authentication/AuthEmailBuilderTests.cs`
  cover deep-link precedence, fallback, trailing slashes, and URL encoding.
- Integration tests: `test/BudgetFriend.API.IntegrationTests/Auth/EmailVerificationTests.cs`
  cover registration email content, verify flow, invalid/expired/reused tokens,
  and resend semantics.

The integration test factory runs with:
```json
"Email:BaseUrl": "https://test.local",
"Email:DeepLinkBaseUrl": "https://app.test.local"
```
