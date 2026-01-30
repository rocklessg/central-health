# CentralHealth API

A healthcare management backend API built with .NET 8, following clean architecture principles.

## Overview

CentralHealth API provides backend services for front desk staff at healthcare facilities, supporting:

- **Patient Records Landing Screen** - View and manage patient records with filtering, searching, sorting, and pagination
- **Appointment Scheduling** - Create and manage patient appointments
- **Invoice Creation** - Create detailed invoices with itemized medical services and automatic discount calculations
- **Payment Processing** - Process and collect payments through a digital invoice system
- **Status Workflow** - Move patients to "Awaiting Vitals" status after invoice payment

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

3. Run the application:

```bash
cd CentralHealth.Api
dotnet run
```

Swagger UI is available at `/swagger` in development mode.

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

## API Endpoints

### Facility Setup

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/facilities` | Create a new facility with admin user |
| GET | `/api/facilities/{id}` | Get facility by ID |

### Records (Story 1)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/records/search` | Get patient records with filtering, searching, sorting, and pagination |

### Appointments (Story 2)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/appointments` | Create a new appointment |
| GET | `/api/appointments/{id}?facilityId={facilityId}` | Get appointment by ID |
| POST | `/api/appointments/search` | Get appointments with filters |
| PATCH | `/api/appointments/{id}/cancel` | Cancel an appointment |

### Invoices (Story 3)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/invoices` | Create a new invoice with itemized services |
| GET | `/api/invoices/{id}?facilityId={facilityId}` | Get invoice by ID |
| POST | `/api/invoices/search` | Get invoices with filters |
| PATCH | `/api/invoices/{id}/cancel` | Cancel an invoice |

### Payments (Story 4 & 5)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/payments` | Process a payment (moves patient to AwaitingVitals on full payment) |
| GET | `/api/payments/{id}?facilityId={facilityId}` | Get payment by ID |
| GET | `/api/payments/invoice/{invoiceId}?facilityId={facilityId}` | Get payments by invoice |

## Request Examples

### 1. Create Facility (Initial Setup)

```http
POST /api/facilities
Content-Type: application/json

{
  "name": "Central Health Hospital",
  "code": "CH-001",
  "address": "123 Healthcare Avenue",
  "phone": "+234-800-000-0001",
  "email": "info@centralhealth.com"
}
```

### 2. Search Records

```http
POST /api/records/search
Content-Type: application/json

{
  "facilityId": "your-facility-id",
  "userId": "your-user-id",
  "username": "frontdesk",
  "startDate": "2024-01-15",
  "endDate": "2024-01-15",
  "clinicId": null,
  "searchTerm": "John",
  "sortDescending": false,
  "pageNumber": 1,
  "pageSize": 20
}
```

### 3. Create Appointment

```http
POST /api/appointments
Content-Type: application/json

{
  "facilityId": "your-facility-id",
  "userId": "your-user-id",
  "username": "frontdesk",
  "patientId": "patient-id",
  "clinicId": "clinic-id",
  "appointmentDate": "2024-01-20",
  "appointmentTime": "09:30:00",
  "type": 1,
  "reasonForVisit": "General checkup",
  "notes": ""
}
```

### 4. Create Invoice

```http
POST /api/invoices
Content-Type: application/json

{
  "facilityId": "your-facility-id",
  "userId": "your-user-id",
  "username": "frontdesk",
  "patientId": "patient-id",
  "appointmentId": "appointment-id",
  "discountPercentage": 10,
  "notes": "Regular patient discount",
  "items": [
    {
      "medicalServiceId": null,
      "description": "Consultation",
      "quantity": 1,
      "unitPrice": 5000,
      "discountAmount": 0
    },
    {
      "medicalServiceId": "service-id",
      "description": "",
      "quantity": 1,
      "unitPrice": 2500,
      "discountAmount": 500
    }
  ]
}
```

### 5. Process Payment

```http
POST /api/payments
Content-Type: application/json

{
  "facilityId": "your-facility-id",
  "userId": "your-user-id",
  "username": "frontdesk",
  "invoiceId": "invoice-id",
  "amount": 6750.00,
  "method": 1,
  "transactionId": "TXN-12345",
  "notes": "Cash payment"
}
```

Payment Methods:
- `0` = Cash
- `1` = Card
- `2` = Transfer
- `3` = Wallet

### 6. Cancel Appointment/Invoice

```http
PATCH /api/appointments/{id}/cancel
Content-Type: application/json

{
  "facilityId": "your-facility-id",
  "userId": "your-user-id",
  "username": "frontdesk"
}
```
## Logging

All key events are logged as per acceptance criteria:

| Event | Log Level | Example |
|-------|-----------|---------|
| List loaded | Information | "Records list loaded successfully. TotalCount=50, PageNumber=1" |
| Filter applied | Information | "Filter applied: ClinicId=xxx" |
| Search executed | Information | "Search executed: SearchTerm=John" |
| Create operations | Information | "Appointment created successfully. AppointmentId=xxx" |
| Status changes | Information | "Patient moved to AwaitingVitals after payment" |
| Validation errors | Warning | "Patient not found. PatientId=xxx" |
| Errors | Error | "Error creating invoice" |

### Log Output
- **Console**: Real-time log output during development
- **File**: Logs saved to `logs/centralhealth-{date}.log`

## Assumptions

1. **Authentication/Authorization**: The API expects facility scoping via `facilityId`.
 HTTP headers (`X-Facility-Id`, `X-User-Id`, `X-Username`, `X-User-Role`) can be used in production grade level. These would be extracted from JWT tokens or similar authentication mechanisms.

2. **Multi-tenancy**: The system supports multiple facilities with data isolation based on FacilityId.

3. **Currency**: Default currency is NGN (Nigerian Naira). The system supports currency per transaction.

4. **Appointment Workflow**: 
   - Scheduled -> CheckedIn -> AwaitingPayment (after invoice creation) -> AwaitingVitals (after payment) -> InProgress -> Completed

5. **Invoice Status Flow**:
   - Draft -> Pending -> PartiallyPaid/Paid -> (or Cancelled/Refunded)

6. **Wallet Payments**: Patients can have wallet balances that can be used for payments.

7. **Soft Deletes**: All entities support soft deletion via `IsDeleted` flag.
