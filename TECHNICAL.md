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

---

## 9. Distributed Caching

### Redis

Redis is used as the distributed cache for selected read-heavy financial endpoints, currently:

- Dashboard
- Summary

Caching is accessed through an abstraction:

```text
ICacheService
```

with the current implementation:

```text
RedisCacheService
```

Conceptually:

```text
Endpoint
   │
   ▼
ICacheService
   │
   ▼
RedisCacheService
   │
   ▼
Redis
```

Feature code therefore does not depend directly on Redis APIs, keeping the feature layer independent from the specific cache provider.

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
- Caching
- Rate limiting
- Health checks

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
