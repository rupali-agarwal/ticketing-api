# Ticketing API

A REST API for managing events, ticket purchases, availability, and sales reporting.

The application is built with ASP.NET Core, Entity Framework Core, and SQL Server. It uses a layered architecture to separate API, application, domain, and infrastructure concerns.

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server / LocalDB
- xUnit
- Scalar / OpenAPI

## Project Structure

```text
src/
├── Ticketing.Api
├── Ticketing.Application
├── Ticketing.Domain
└── Ticketing.Infrastructure

tests/
├── Ticketing.UnitTests
└── Ticketing.IntegrationTests
```

### Ticketing.Api

Contains the HTTP API layer, including controllers, dependency injection configuration, OpenAPI configuration, and global exception handling.

### Ticketing.Application

Contains application contracts, DTOs, service interfaces, and application-specific exceptions.

### Ticketing.Domain

Contains the core domain entities:

- `Event`
- `PricingTier`
- `TicketPurchase`

### Ticketing.Infrastructure

Contains the EF Core `DbContext`, entity configurations, migrations, and service implementations.

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server LocalDB or another SQL Server instance

The default development connection string uses SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=TicketingDb;Trusted_Connection=True;TrustServerCertificate=True
```

If required, update `src/Ticketing.Api/appsettings.json` with an appropriate SQL Server connection string.

### Apply Database Migrations

From the repository root:

```bash
dotnet ef database update \
  --project src/Ticketing.Infrastructure \
  --startup-project src/Ticketing.Api
```

### Run the API

```bash
dotnet run --project src/Ticketing.Api
```

When running in the Development environment, OpenAPI and Scalar are enabled for exploring the API.

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/events` | Create an event |
| `GET` | `/api/events` | Get all events |
| `GET` | `/api/events/{id}` | Get an event by ID |
| `PUT` | `/api/events/{id}` | Update an event |
| `DELETE` | `/api/events/{id}` | Soft-delete an event |
| `GET` | `/api/events/{id}/availability` | Get ticket availability |
| `POST` | `/api/events/{id}/tickets` | Purchase tickets |
| `GET` | `/api/events/{id}/sales-summary` | Get event sales and revenue summary |

## Domain and Assumptions

An event has a single total capacity shared across all pricing tiers.

Pricing tiers determine the price of a ticket but do not maintain separate inventory. This keeps inventory management at the event level and ensures that all ticket purchases compete for the same capacity.

`TicketsSold` is stored on the event to make availability checks and inventory updates efficient.

A `TicketPurchase` stores the `UnitPrice` at the time of purchase rather than relying on the current pricing tier price. This preserves historical pricing and allows sales reports to remain accurate if pricing changes later.

Events are soft-deleted using `IsDeleted` and `DeletedAtUtc`. A global EF Core query filter prevents deleted events from being returned by normal queries.

The implementation does not model assigned seating; purchases reserve a quantity of tickets against the event's total capacity.

## Concurrency and Overselling Prevention

Preventing overselling is treated as a database consistency problem rather than relying on an in-memory availability check.

A read-then-update approach such as:

```text
Read TicketsSold
Check remaining capacity
Update TicketsSold
```

is vulnerable to race conditions because multiple requests can observe the same available inventory before either request updates it.

Instead, ticket inventory is reserved using a conditional atomic update in SQL Server:

```csharp
var rowsAffected = await _dbContext.Events
    .Where(e =>
        e.Id == eventId &&
        e.TicketsSold + request.Quantity <= e.TotalCapacity)
    .ExecuteUpdateAsync(
        setters => setters
            .SetProperty(
                e => e.TicketsSold,
                e => e.TicketsSold + request.Quantity)
            .SetProperty(
                e => e.UpdatedAtUtc,
                now),
        cancellationToken);
```

The capacity condition and inventory update therefore occur as a single database operation.

If one row is updated, the inventory was successfully reserved. If no rows are updated, sufficient inventory is no longer available and the API returns `409 Conflict`.

The inventory update and creation of the corresponding `TicketPurchase` are performed within the same database transaction so they are committed together.

## Sales Reporting

The sales summary endpoint returns:

- total tickets sold
- total revenue
- tickets sold by pricing tier
- revenue by pricing tier

Revenue is calculated from `TicketPurchase.UnitPrice`, preserving the price actually paid at purchase time rather than using the pricing tier's current price.

Pricing tiers with no sales are also included in the summary with zero tickets sold and zero revenue.

## Validation and Error Handling

The API uses a global exception handler and RFC-style `ProblemDetails` responses.

Application errors are mapped to appropriate HTTP status codes:

| Situation | Status |
|---|---|
| Invalid request or business rule violation | `400 Bad Request` |
| Event or pricing tier not found | `404 Not Found` |
| Insufficient ticket inventory | `409 Conflict` |
| Unexpected server error | `500 Internal Server Error` |

Examples of enforced business rules include:

- events must start in the future
- an event must have at least one pricing tier
- pricing tier names within an event must be unique
- purchase quantity must be greater than zero
- tickets cannot be purchased after an event has started
- event capacity cannot be reduced below the number of tickets already sold
- a pricing tier must belong to the event being purchased
- purchases cannot exceed the event's remaining capacity

Database constraints provide an additional layer of protection for important invariants such as capacity, ticket counts, purchase quantity, and prices.

## Testing

The solution contains both unit and integration tests.

Run all tests from the repository root:

```bash
dotnet test
```

### Unit Tests

Unit tests focus on event business rules, including:

- rejecting events in the past
- requiring pricing tiers
- rejecting duplicate pricing tier names
- creating valid events
- preventing capacity from being reduced below tickets already sold
- allowing capacity to equal tickets already sold
- excluding soft-deleted events

EF Core's InMemory provider is used for these service-level tests.

### Integration Tests

Integration tests run the real ASP.NET Core application using `WebApplicationFactory` and use a dedicated SQL Server LocalDB database.

The integration database is isolated from the development database and reset between tests.

Integration tests cover:

- API startup and database connectivity
- event creation and ticket purchasing
- availability updates
- validation error responses
- not-found responses
- insufficient inventory conflicts
- concurrent ticket purchasing

A concurrency integration test creates an event with a capacity of 10 and sends 20 concurrent requests for one ticket each.

The test verifies that:

```text
10 requests succeed
10 requests return 409 Conflict
TicketsSold = 10
TicketsAvailable = 0
10 TicketPurchase records are committed
```

This test runs against SQL Server rather than EF Core's InMemory provider so that the concurrency behaviour being tested reflects the database semantics relied upon by the application.

## Design Trade-offs

This solution intentionally keeps the architecture lightweight for the scope of the exercise.

Service implementations use `TicketingDbContext` directly rather than introducing generic repository or unit-of-work abstractions. EF Core already provides the required persistence and transaction abstractions, and additional layers would add complexity without providing significant value for this application.

Inventory is maintained at the event level rather than per pricing tier. This assumes pricing tiers represent different prices for access to the same event inventory rather than independently allocated ticket pools.

The solution is implemented as a modular monolith rather than separate services. For the current scope, this keeps transactional consistency and deployment straightforward while maintaining clear boundaries between the API, application, domain, and infrastructure layers.