# GUHC Hierarchy Accounts System

A backend service for managing account hierarchies for Grand Unified Holding Corp.
Built with .NET 8, ASP.NET Core Web API, and MS SQL Server.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local install or Docker)
- EF Core CLI tools:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Database Setup

1. Update the connection string in `HierarchyAccounts.Api/appsettings.json`:
   ```json
   "DefaultConnection": "Server=localhost;Database=HierarchyAccountsDb;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

2. Apply migrations:
   ```bash
   dotnet ef database update \
     --project HierarchyAccounts.Infrastructure \
     --startup-project HierarchyAccounts.Api
   ```

## Running the API

```bash
cd HierarchyAccounts.Api
dotnet run
```

Swagger UI is available at: `https://localhost:7001/swagger`

## Running the Console App

```bash
cd HierarchyAccounts.Console

# Print the full account tree
dotnet run

# Print the subtree of a specific account
dotnet run -- <accountId>

# Use a custom API URL
dotnet run -- --api-url https://myhost:7001/

# Combine options
dotnet run -- <accountId> --api-url https://myhost:7001/
```

## Running Tests

```bash
dotnet test
```

## API Reference

| Method   | Endpoint                        | Description                                          |
|----------|---------------------------------|------------------------------------------------------|
| `POST`   | `/api/accounts`                 | Create a new account (root if no ParentId given)     |
| `GET`    | `/api/accounts/{id}`            | Get details of a single account                      |
| `GET`    | `/api/accounts/{id}/subtree`    | Get the subtree rooted at an account (nested JSON)   |
| `GET`    | `/api/accounts/tree`            | Get the full account hierarchy (nested JSON)         |
| `PATCH`  | `/api/accounts/{id}/move`       | Move an account under a new parent                   |
| `DELETE` | `/api/accounts/{id}`            | Delete account; children are reassigned to its parent|

## Business Rules

- Maximum tree depth: **5 levels**
- Cycles are **never allowed** (validated on every create and move)
- The **root account cannot be moved or deleted**
- Deleting an account **reassigns its direct children** to the deleted account's parent
- Depth constraint is enforced on move for the **entire subtree**, not just the moved node

## Example Data Flow

```http
# 1. Create root account (depth 1)
POST /api/accounts
{ "name": "Global Corp" }

# 2. Create regional branch (depth 2)
POST /api/accounts
{ "name": "Europe Region", "parentId": "<globalCorpId>" }

# 3. Create country office (depth 3)
POST /api/accounts
{ "name": "Germany Office", "parentId": "<europeId>" }

# 4. View full hierarchy
GET /api/accounts/tree

# 5. Move Germany under a different region
PATCH /api/accounts/<germanyId>/move
{ "newParentId": "<asiaId>" }

# 6. Delete Europe (Germany, now under Asia, is unaffected)
DELETE /api/accounts/<europeId>
```

## Project Structure

```
HierarchyAccounts.Domain         → Entities, domain exceptions, repository interface
HierarchyAccounts.Application    → DTOs, service interface, business logic
HierarchyAccounts.Infrastructure → EF Core DbContext, repository implementation, migrations
HierarchyAccounts.Api            → ASP.NET Core controllers, middleware, Swagger config
HierarchyAccounts.Console        → CLI viewer using the REST API
HierarchyAccounts.Tests          → xUnit unit tests (domain + application layer)
```