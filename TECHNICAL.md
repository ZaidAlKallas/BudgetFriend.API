# BudgetFriend API

## 1. Overview

BudgetFriend API is a personal finance API built around features and use cases. The project uses Vertical Slice Architecture, with a focus on cohesion, explicit dependencies, domain correctness, testability, and independent feature evolution.

The infrastructure is intentionally kept purposeful: technologies are introduced when they solve a concrete problem or provide meaningful production value.

---

## 2. Architecture

### Vertical Slice Architecture

BudgetFriend API follows **Vertical Slice Architecture**. Instead of organizing the application primarily around technical layers such as:

```text
Controllers/
Services/
Repositories/
DTOs/
```

the application is organized around features and use cases:

```text
Features/
├── Accounts/
├── Authentication/
├── Categories/
├── Dashboard/
├── Transactions/
├── Transfers/
└── ...
```

Each feature contains the code required to implement its use cases. This keeps related behavior together, reduces unnecessary coupling, makes features easier to understand, and allows individual features to evolve independently.

A typical feature may contain:

```text
Features/
└── Accounts/
    └── CreateAccount/
        ├── CreateAccountEndpoint.cs
        ├── CreateAccountRequest.cs
        ├── CreateAccountResponse.cs
        └── CreateAccountValidator.cs
```

The exact structure can vary according to feature complexity. The important principle is that the feature owns its application behavior.

### Design Principles

#### Cohesion
Related behavior should live together.

#### Explicit Dependencies
Features should depend only on the abstractions and infrastructure they actually require.

#### Domain Correctness
Financial rules should be enforced consistently rather than relying only on client-side validation.

#### Testability
Important application behavior should be testable through realistic integration tests.

#### Evolution
Individual features should be able to evolve without requiring changes throughout the entire application.

---

## 3. Backend and API Style

### ASP.NET Core 10

ASP.NET Core 10 is the foundation of the API. It provides:

- HTTP request processing
- Dependency injection
- Configuration
- Authentication and authorization
- Middleware
- Minimal APIs
- Health checks

### Minimal APIs

The API uses ASP.NET Core Minimal APIs instead of MVC controllers.

Endpoints are defined close to their corresponding features, which fits naturally with Vertical Slice Architecture.

Endpoints are responsible for:

- Receiving HTTP requests
- Binding request data
- Calling the required application/domain logic
- Returning HTTP responses

For example:

```text
POST /api/accounts
```

is implemented by the corresponding `CreateAccount` feature rather than by a large centralized controller.

---

## 4. Domain and Financial Model

### Domain Relationships

The financial model is centered around:

```text
User
│
├── Accounts
│     │
│     └── Transactions
│             │
│             └── Category
│
└── Categories
```

Transfers introduce a higher-level relationship between two accounts:

```text
Account
   │
   └── Transfer
       │
       ├── From Account
       └── To Account
```

A transfer represents movement of money between accounts rather than income or expense.

### Accounts

Users can create and manage multiple financial accounts.

Each account contains:

- Name
- Initial balance
- Currency

Every account is associated with exactly one currency.

### Account Balance

An account balance is calculated from its initial balance and associated transactions:

```text
Balance = Initial Balance + Income - Expenses
```

Transfers affect the balances of their source and destination accounts but are excluded from income and expense reporting.

### Categories

Categories classify financial transactions.

Each category is associated with a transaction type:

- Income
- Expense

Categories are user-specific and cannot be shared between users.

### Transactions

Transactions represent financial activity associated with an account.

A transaction contains information such as:

- Account
- Category
- Amount
- Transaction date
- Note

The transaction currency is determined by its associated account.

BudgetFriend currently supports:

- Income
- Expense

Transfers are represented separately as a higher-level financial operation.

### Multi-Currency Support

Each account has exactly one currency, allowing users to maintain accounts in different currencies independently.

For example:

```text
Cash Account     → USD
Bank Account     → EUR
Savings Account  → GBP
```

A transaction does not independently select a currency; it inherits the currency of its account.

Financial values belonging to different currencies are not implicitly combined. Aggregations such as balance, income, expenses, summaries, dashboard statistics, and category analysis are calculated separately for each currency.

This prevents mathematically invalid operations such as adding USD and EUR without an explicit conversion.

### Transfers

Transfers allow users to move money between accounts.

A transfer contains:

- Source account
- Destination account
- Source amount
- Destination amount
- Transfer date
- Optional note

Transfers can occur between accounts using the same currency or different currencies.

#### Same-Currency Transfer

```text
USD Account A
100 USD
    ↓
USD Account B
100 USD
```

The source balance decreases while the destination balance increases. The operation does not represent income or an expense.

#### Cross-Currency Transfer

```text
USD Account
100 USD
    ↓
EUR Account
91 EUR
```

The two amounts are represented explicitly rather than treating them as the same monetary value.

#### Financial Reporting

Transfers affect account balances but are excluded from income and expense calculations. This prevents internal movement of money from being incorrectly reported as financial income or spending.

---

## 5. Authentication and Authorization

### JWT Authentication

JSON Web Tokens are used to authenticate API requests.

The authentication system includes:

- User registration
- User login
- Access tokens
- Refresh tokens
- Token renewal
- User-specific authorization

Protected endpoints use the authenticated user's identity to scope financial data.

---

## 6. Validation

### FluentValidation

FluentValidation is used to validate API requests before they reach the main endpoint logic.

This keeps input validation explicit and prevents invalid request data from unnecessarily reaching the domain/application logic.

---

## 7. Financial Reporting Features

### Dashboard

The Dashboard provides a high-level overview of a user's financial state.

#### Account Summaries

For each account:

- Account ID
- Account name
- Current balance
- Currency

#### Currency Breakdown

Financial information is grouped by currency and includes:

- Total balance
- Monthly income
- Monthly expenses
- Net monthly income

#### Expense Categories

The Dashboard identifies frequently used expense categories and provides their breakdown by currency.

#### Recent Transactions

The Dashboard returns the most recent financial transactions with:

- Account
- Currency
- Category
- Transaction type
- Amount
- Note
- Transaction date

### Financial Summary

The Summary endpoint provides aggregated financial information over a selected period.

The default period is the current month when no explicit date range is provided.

Summary data is grouped by currency and includes:

- Initial balance
- Income
- Expenses
- Net amount

Currencies remain independent during aggregation.

### Categories Analysis

Categories Analysis provides a more detailed view of financial activity grouped by category.

The analysis includes:

- Category
- Transaction type
- Currency
- Total amount
- Transaction count
- Percentage

Percentages are calculated within the relevant currency rather than combining amounts from different currencies.

---

## 8. Database and Persistence

### PostgreSQL

PostgreSQL is the primary relational database.

It stores:

- Users
- Accounts
- Categories
- Transactions
- Transfers
- Refresh tokens

PostgreSQL is also used by the integration test environment through Testcontainers.

### Entity Framework Core

Entity Framework Core is used as the ORM for database access and persistence.

It provides:

- LINQ-based queries
- Entity mapping
- Change tracking
- Database migrations
- Query translation
- Async database operations
- Persistence

### Production Database (Neon)

In the planned production architecture the API runs on Render and PostgreSQL is hosted by Neon. No Neon-specific code is required: the application connects to PostgreSQL using the `ConnectionStrings:Database` value supplied by ASP.NET Core configuration, and Neon is treated purely as the PostgreSQL hosting provider.

```text
Render environment variable (ConnectionStrings__Database)
    ↓
ASP.NET Core configuration
    ↓
EF Core / Npgsql
    ↓
Neon PostgreSQL
```

The local development setup is unchanged and continues to use the PostgreSQL container from `docker-compose.yml`:

```text
Local PostgreSQL (docker-compose.yml)
    ↓
appsettings.json
```

Configuration precedence is standard ASP.NET Core behavior: environment variables (for example `ConnectionStrings__Database` on Render) override the value in `appsettings.json`. No production connection string is stored in the repository.

#### Npgsql connection string format

Npgsql consumes connection strings in the ADO.NET key/value format. The existing local value already uses this format:

```text
Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require;Maximum Pool Size=<n>;No Reset On Close=true
```

When a Neon connection string is pasted into a Render environment variable, use the **Npgsql key/value format** rather than the `postgresql://` URI format shown in parts of the Neon Console. Relevant Npgsql options for Neon:

- `SSL Mode=Require` — Neon requires TLS. Npgsql defaults to `Prefer`; `Require` matches Neon's `sslmode=require`.
- `Maximum Pool Size` — Npgsql pools connections by default (max 100). Neon Free-tier computes have a small `max_connections`, so keep the pool size modest (for example 10–20) unless the workload needs more.
- `No Reset On Close=true` — when Npgsql's own pool sits on top of Neon's PgBouncer (pooled string), this disables Npgsql's `DISCARD ALL` reset behavior, which does not make sense across PgBouncer in transaction mode.
- Uses **Npgsql pooler** connection string (hostname with `-pooler` suffix) for application traffic.

#### Migrations and pooled vs. direct connections

Neon provides a **pooled** (PgBouncer) and a **direct** (unpooled) connection string for each database:

| Activity | Connection type |
| --- | --- |
| Application runtime traffic | Pooled (`-pooler` hostname) |
| EF Core schema migrations, `pg_dump`, `pg_restore`, session-level SQL | Direct (no `-pooler`) |

Run migrations with the direct connection string. Pooled connections use PgBouncer in transaction mode and can fail schema operations in ways that are hard to diagnose.

The API only auto-applies migrations in the `Development` environment (`WebApplicationExtensions.cs`), so the production (Render/`Production`) startup never runs migrations automatically. This is intentional: applying migrations remains an explicit, operator-controlled step so multiple API instances cannot race to migrate the schema. If Render is ever scaled to more than one instance, keeping migrations out of startup avoids concurrent-migration contention.

#### Production database initialization procedure

This procedure is followed once, before first deployment. It does not contain or require any credentials stored in the repository.

1. **Create the Neon project/database.** In Neon, create a project (or use an existing one) and note the branch (default `main`), database name (default `neondb`), and role (default `neondb_owner`). No Neon Auth is used; authentication remains handled by the ASP.NET Core API.
2. **Obtain the connection strings.** In the Neon Console, use **Connect** to copy both connection strings: the **pooled** one (hostname contains `-pooler`) for the application, and the **direct** one (no `-pooler`) for migrations.
3. **Store the connection string securely.** Configure the production connection string as an environment variable on Render, for example:
   - `ConnectionStrings__Database` = the pooled Neon connection string in Npgsql key/value format (with `SSL Mode=Require`).
   - Never place the value in `appsettings.json`, `docker-compose.yml`, or any committed file.
4. **Apply the EF Core migrations.** Apply migrations against the Neon database using the **direct** connection string and the EF Core tooling, from the directory that contains the startup project:
   ```bash
   dotnet ef database update --project src/BudgetFriend.API
   ```
   The required connection string must be supplied via an environment variable, for example `ConnectionStrings__Database="Host=<direct-neon-host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require"`.
5. **Verify the database schema.** Inspect the schema in the Neon Console (Tables view) or connect with the CLI/editor and confirm all tables (`Users`, `Accounts`, `Categories`, `Transactions`, `Transfers`, `RefreshTokens`) and the `__EFMigrationsHistory` table exist.
6. **Verify the API can connect.** Deploy the API to Render with the pooled connection string configured as `ConnectionStrings__Database`, then check the health endpoint (`/_health`), which includes the PostgreSQL (`Npgsql`) health check.

Alternatively, only migrations can be executed during a Render pre-deploy/build step using the direct connection string, while the runtime connection remains pooled. The simplest initial approach is step 4 done manually, once, before enabling traffic.

#### Neon Free Tier reliability notes

Neon's Free plan is suitable for a small personal application but has limits worth knowing:

- **Storage** — 0.5 GB per project. Once exceeded, writes that increase storage fail until space is freed or the plan is upgraded.
- **Compute** — 100 CU-hours per project per month. An idle compute scales to zero after 5 minutes of inactivity (cannot be disabled on Free) and the first query has a cold-start penalty (hundreds of milliseconds). Hitting the CU-hour cap suspends compute until the next billing period.
- **Connections** — `max_connections` scales with compute size (a 0.25 CU compute allows approximately 104 raw connections). PgBouncer pooling accepts up to 10,000 client connections, which is why the application should use the pooled string. Npgsql's own pool safely multiplexes onto this.
- **Branches** — up to 10 branches per project. Copy-on-write branches are instant and can be used to test migrations on a production-like copy before applying to the primary branch.
- **Backups/recovery** — instant restore history of 6 hours (capped at 1 GB of change history) plus 1 manual snapshot per project. For a financial application holding real data, these free-tier protections are thin: consider periodic `pg_dump` exports as an additional recovery path until a paid plan with a longer history window is justified.
- **Egress** — 5 GB of public network transfer per month.

---

## 9. Caching

### Overview

Redis is the preferred cache for selected read-heavy financial endpoints, currently:

- Dashboard
- Summary

ASP.NET Core's in-memory cache (`IMemoryCache`) acts as a fallback so the API stays functional when Redis is unavailable (for example during a brief Redis outage or while Redis is being deployed).

Caching is accessed through an abstraction:

```text
ICacheService
```

with the current implementation:

```text
HybridCacheService
```

Conceptually:

```text
Endpoint
   │
   ▼
ICacheService
   │
   ▼
HybridCacheService
   │
   ├── Redis available  ────────────►  Redis
   │
   └── Redis unavailable ───────────►  IMemoryCache (in-process fallback)
```

Feature code therefore does not depend directly on Redis APIs, keeping the feature layer independent from the specific cache provider.

### HybridCacheService

`HybridCacheService` selects a backend for each operation:

- **Redis backend** — preferred. Used when Redis is available and reachable.
- **Memory backend** — in-process fallback. Used when Redis is not configured, or when a Redis operation fails.

Behavior:

- When a Redis operation throws a `RedisException`, the service switches to the in-memory backend. The Redis backend is then re-probed at most once every 30 seconds, so the API recovers automatically once Redis is reachable without pinging Redis on every request.
- If no `ConnectionStrings:Redis` value is configured, the service is memory-only from startup and never attempts Redis.
- `Remove` and `RemoveByPrefix` clear both backends best-effort, so stale entries cannot survive a cache invalidation regardless of which backend is active.
- In-memory entries are namespaced with a prefix and tracked in an index so `RemoveByPrefix` (Redis `KEYS`-style prefix removal) also works against the in-memory cache, which does not support prefix queries natively.

Cache entries expire after 5 minutes (the TTL applied when writing dashboard and summary responses) and are invalidated on financial changes.

### Backend Wiring

`AddCaching` (in `ServiceCollectionExtensions`) always registers `IMemoryCache` and `ICacheService`. Redis wiring is config-driven:

- `ConnectionStrings:Redis` present → `AddStackExchangeRedisCache` and a shared `IConnectionMultiplexer` are registered; `HybridCacheService` starts with the Redis backend.
- `ConnectionStrings:Redis` missing or empty → no distributed cache or multiplexer is registered; caching is memory-only.

The Redis connection string is read from configuration at dependency resolution time rather than registration time, so runtime configuration overrides are always honored.

### Deployment Resilience

Because the fallback is in-process, when Redis is down each API instance serves cached responses from its own memory. This means:

- The API continues to serve cached dashboard and summary data during a Redis outage; database queries still happen on cache misses.
- Cached values may differ briefly across instances because in-memory fallback state is not shared. This is bounded by the 5-minute cache TTL, after which entries are re-fetched from the database (the source of truth).
- Corruption of the database is not possible: the cache is read-through to PostgreSQL and writes always go to the database regardless of cache backend.

### Cache Invalidation

Financial changes can affect multiple cached views.

For example:

```text
Create Transaction
       │
       ├── Account Balance changes
       ├── Dashboard changes
       └── Summary changes
```

Therefore, affected cached data is invalidated when relevant financial changes occur.

---

## 10. Cross-Cutting Concerns

The application contains infrastructure for concerns that affect multiple features, including:

- Authentication
- Authorization
- Validation
- Error handling
- Logging
- Observability
- Caching
- Rate limiting
- Health checks

### Observability — OpenTelemetry

OpenTelemetry provides distributed traces and metrics:

- ASP.NET Core instrumentation for incoming HTTP requests
- HttpClient instrumentation for outgoing HTTP calls
- ASP.NET Core HTTP metrics and .NET runtime metrics
- A small set of BudgetFriend business metrics (`budgetfriend.*`)
- OTLP export, configured through the standard `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_PROTOCOL` settings

Serilog remains the logging system. Log events are enriched with `TraceId` and `SpanId` so application logs can be correlated with OpenTelemetry traces. No second logging framework is used.

These concerns are kept separate from individual feature implementations where appropriate.

### Logging — Serilog

Serilog provides structured application logging.

Structured logs make it possible to attach useful contextual information to application events rather than relying only on plain text messages.

### Rate Limiting

ASP.NET Core rate limiting is applied to sensitive endpoints, particularly authentication operations.

Its goal is to reduce abuse and excessive or automated request traffic against sensitive API operations.

### Health Checks

ASP.NET Core Health Checks expose application health information.

PostgreSQL connectivity is monitored as an external dependency. Health checks can be consumed by deployment and container orchestration infrastructure to determine whether the application is ready to serve traffic.

---

## 11. Docker and Infrastructure

### Docker

Docker provides reproducible application and infrastructure environments.

The development environment includes supporting services such as:

- PostgreSQL
- Redis
- Aspire Dashboard (standalone, for local telemetry viewing)

This allows the API to be developed without requiring these services to be installed directly on the host machine.

### Infrastructure Philosophy

Infrastructure is added when it solves a concrete problem or provides meaningful production value. The project intentionally avoids adding technologies only for the sake of increasing the technology stack.

Each infrastructure component should have a clear responsibility within the system.

### Configuration

Application configuration is environment-based.

Sensitive or environment-specific values such as:

- Database connection strings
- JWT secrets
- Redis configuration

should not be hard-coded into the application source code.

Development and deployment environments can provide their own configuration values.

---

## 12. Integration Testing

Integration tests cover API behavior and complete workflows.

The project uses:

- xUnit
- ASP.NET Core `WebApplicationFactory`
- Testcontainers
- PostgreSQL
- Redis

Tests run through the HTTP interface against a real PostgreSQL instance running in an isolated temporary container rather than relying exclusively on mocked database behavior.

The testing flow is:

```text
Test
 │
 ▼
WebApplicationFactory
 │
 ▼
ASP.NET Core Application
 │
 ▼
EF Core
 │
 ▼
PostgreSQL Test Container
```

This allows the tests to validate interactions between:

- HTTP endpoints
- Application logic
- EF Core
- PostgreSQL
- Redis-backed caching (and in-memory fallback when Redis is unavailable)
- Authentication
- Validation

and provides stronger confidence for database-dependent behavior.

---

## 13. Continuous Integration

### GitHub Actions

GitHub Actions is used for Continuous Integration.

The CI pipeline automatically validates changes through operations such as:

```text
Restore
   ↓
Build
   ↓
Integration Tests
   ↓
Coverage
```

The repository uses branch protection to prevent changes from being merged into the protected `main` branch when required checks fail.

This helps prevent regressions from reaching the main branch.

---

## 14. API Documentation

The API exposes OpenAPI documentation and uses Scalar as the API documentation interface.

This allows developers to inspect available endpoints and interact with the API during development.
