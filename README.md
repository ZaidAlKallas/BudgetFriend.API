# BudgetFriend

## Overview

BudgetFriend is a personal finance application designed to help users manage their financial accounts, track income and expenses, and understand their financial activity through aggregated dashboards and analysis.

The project is built with a strong focus on backend engineering, domain modeling, API design, and modern software development practices.

The **BudgetFriend API** provides the backend services responsible for authentication, account and transaction management, financial analysis, multi-currency support, and transfers between accounts.

---

## Why BudgetFriend?

BudgetFriend started as a personal finance application, but the API has a broader purpose: to serve as a practical project for applying and exploring modern backend engineering practices in a realistic domain.

Financial applications provide interesting engineering challenges that are not present in a simple CRUD application.

For example:

* Financial data must remain internally consistent.
* Accounts and transactions have relationships that enforce business rules.
* Different currencies cannot be safely aggregated without conversion.
* Transfers between accounts affect balances without representing income or expenses.
* Financial summaries must aggregate data correctly across accounts and currencies.
* Cached financial data must be invalidated when the underlying data changes.
* Authentication and authorization are essential because financial data is user-specific.

These requirements make BudgetFriend a useful domain for exploring how to design and build a maintainable Web API rather than simply implementing CRUD endpoints.

---

## Project Goals

The main goals of BudgetFriend are:

### 1. Personal Finance Management

Provide users with the ability to manage their financial data, including:

* Accounts
* Categories
* Income
* Expenses
* Transfers
* Multiple currencies

### 2. Financial Insights

Provide aggregated information that helps users understand their financial activity through:

* Dashboard data
* Financial summaries
* Category analysis
* Currency-based breakdowns
* Recent transactions

### 3. Strong Domain Modeling

Represent financial concepts and their relationships explicitly while enforcing important business rules within the domain and application logic.

Examples include:

* An account has a single currency.
* Transactions use the currency of their account.
* Income and expenses affect account balances differently.
* Transfers move money between accounts without being treated as income or expenses.
* Different currencies are kept separate unless an explicit conversion is performed.

### 4. Modern API Engineering

Use BudgetFriend as a practical environment for applying modern backend engineering practices, including:

* Vertical Slice Architecture
* Integration testing
* Authentication and authorization
* Distributed caching
* Rate limiting
* Health checks
* Containerization
* Continuous Integration
* API observability and production-oriented practices

---

## Core Concepts

### Accounts

An account represents a source or container of money owned by a user.

Each account has:

* A name
* An initial balance
* A currency

The account balance is derived from its initial balance and its associated financial transactions.

### Transactions

Transactions represent financial activity associated with an account.

They can represent:

* Income
* Expenses

Transactions inherit the currency of their associated account.

### Categories

Categories classify transactions and allow financial activity to be grouped and analyzed.

Categories are associated with a transaction type, allowing income and expense categories to be treated differently when generating financial summaries and analysis.

### Multi-Currency

BudgetFriend supports multiple currencies.

Each account is responsible for a single currency, allowing users to maintain accounts such as:

* USD account
* EUR account
* GBP account

Amounts belonging to different currencies are not implicitly combined.

### Transfers

Transfers represent moving money from one account to another.

A transfer can occur:

* Between accounts using the same currency.
* Between accounts using different currencies.

For cross-currency transfers, the transferred amount on each side is explicitly represented rather than assuming that currencies can be directly added together.

Transfers affect account balances but are not treated as income or expenses in financial reporting.

---

## Design Principles

BudgetFriend follows several principles throughout its development:

### Correctness Over Convenience

Financial calculations should preserve domain correctness even when this requires additional modeling or logic.

### Explicit Domain Concepts

Important financial operations should be represented explicitly instead of being hidden behind generic CRUD operations.

### Separation of Concerns

Features should remain cohesive and changes to one feature should have minimal impact on unrelated features.

### Production-Oriented Engineering

The project aims to apply practices that are relevant to real-world APIs rather than focusing only on implementing functionality.

### Incremental Development

The project is developed incrementally. New infrastructure and architectural decisions are introduced when they solve an actual problem or provide meaningful value to the system.

---

## Technology Stack

- ASP.NET Core 10 Minimal APIs
- C#
- Entity Framework Core
- PostgreSQL
- Redis
- JWT Authentication & Refresh Tokens
- FluentValidation
- Serilog
- Docker
- GitHub Actions
- xUnit
- Testcontainers

---

## Scope

BudgetFriend is primarily focused on personal finance management.

The project is not intended to provide:

* Banking services
* Real-time financial market data
* Investment management
* Tax preparation
* Automatic synchronization with financial institutions

Its primary focus is tracking and analyzing personal financial activity through a well-designed API.

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker Desktop

### Run

Clone the repository and start the required infrastructure:

```bash
docker compose up -d 
dotnet run --project src/BudgetFriend.API
dotnet test
```

---

## TECHNICAL

For more details on the technical design, see the [Technical Design](TECHNICAL.md).