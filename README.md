# CentralHealth API

A healthcare management backend API built with .NET 8, following clean architecture principles.

## Overview

CentralHealth API provides backend services for a healthcare facility management system, supporting:

- Patient records landing screen with filtering, searching, sorting, and pagination
- Appointment scheduling and management
- Invoice creation with itemized medical services and automatic discount calculations
- Payment processing with multiple payment methods including wallet payments
- Automatic status transitions (e.g., moving patients to "Awaiting Vitals" after payment)

## Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 8 |
| ORM | Entity Framework Core 8 |
| Database | Microsoft SQL Server |
| Validation | FluentValidation |
| Logging | Serilog (Console + File sinks) |
| API Documentation | Swagger/OpenAPI |
| Architecture | Clean Architecture with Repository Pattern |

## Setup Instructions

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB, Express, or Full)
- Visual Studio 2022 or VS Code

### Configuration

1. Clone the repository

2. Update the connection string in `CentralHealth.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CentralHealthDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

3. Create the initial EF Core migration:

```bash
cd CentralHealth.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../CentralHealth.Api
```

4. Run the application:

```bash
cd CentralHealth.Api
dotnet run
```

Swagger UI is available at `/swagger` in development mode.

### Database Seeding

The application automatically seeds sample data on first run, including:
- 1 Facility
- 2 Clinics
- 1 User (FrontDesk role)
- 2 Patients with wallets
- 3 Medical services
- 2 Sample appointments

## API Endpoints

### Records

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/records` | Get patient records with filtering, searching, sorting, and pagination |

Query Parameters:
- `startDate` - Filter start date (default: today)
- `endDate` - Filter end date (default: today)
- `clinicId` - Filter by clinic
- `searchTerm` - Search by patient name, code, or phone
- `sortDescending` - Sort direction (default: false/ascending)
- `pageNumber` - Page number (default: 1)
- `pageSize` - Page size (default: 20, max: 100)

### Appointments

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/appointments` | Create a new appointment |
| GET | `/api/appointments/{id}` | Get appointment by ID |
| GET | `/api/appointments` | Get appointments with filters |
| PATCH | `/api/appointments/{id}/cancel` | Cancel an appointment |

### Invoices

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/invoices` | Create a new invoice |
| GET | `/api/invoices/{id}` | Get invoice by ID |
| GET | `/api/invoices` | Get invoices with filters |
| PATCH | `/api/invoices/{id}/cancel` | Cancel an invoice |

### Payments

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/payments` | Process a payment |
| GET | `/api/payments/{id}` | Get payment by ID |
| GET | `/api/payments/invoice/{invoiceId}` | Get payments by invoice |


### Architecture and Design Patterns Used

- **Repository Pattern**: Abstracts data access logic
- **Unit of Work**: Manages transactions across multiple repositories
- **Clean Architecture**: Separation of concerns with dependency inversion
- **Unified Response Pattern**: Consistent API response structure

## Important Notes

1. **Logging**: All key events are logged including:
   - Records list loading
   - Filter and search operations
   - Appointment/Invoice/Payment creation
   - Status changes
   - Errors

2. **Error Handling**: Global exception handling middleware provides consistent error responses.

3. **Validation**: All requests are validated using FluentValidation before processing.

4. **Pagination**: Default page size is 20, maximum is 100.

5. **Database Migrations**: Run migrations before first use or when model changes occur.

## Assumptions

1. **Authentication/Authorization**: The API expects facility scoping via HTTP headers (`X-Facility-Id`, `X-User-Id`, `X-Username`, `X-User-Role`). In production, these would be extracted from JWT tokens or similar authentication mechanisms.

2. **Multi-tenancy**: The system supports multiple facilities with data isolation based on FacilityId.

3. **Currency**: Default currency is NGN (Nigerian Naira). The system supports currency per transaction.

4. **Appointment Workflow**: 
   - Scheduled -> CheckedIn -> AwaitingPayment (after invoice creation) -> AwaitingVitals (after payment) -> InProgress -> Completed

5. **Invoice Status Flow**:
   - Draft -> Pending -> PartiallyPaid/Paid -> (or Cancelled/Refunded)

6. **Wallet Payments**: Patients can have wallet balances that can be used for payments.

7. **Soft Deletes**: All entities support soft deletion via `IsDeleted` flag.

