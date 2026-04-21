# Backend Architecture

## Stack

| Technology | Role |
|---|---|
| ASP.NET Core 9 Web API | HTTP layer, routing, middleware |
| Entity Framework Core 9 | ORM, migrations |
| PostgreSQL 16 | Primary database |
| FluentValidation | Request validation |
| QuestPDF | Server-side PDF generation |
| Serilog | Structured logging |

---

## Project Structure

```
src/
  SchoolManagement.API/
    Controllers/          One controller per domain (Auth, Students, Teachers, ...)
    Common/               GlobalExceptionMiddleware, StartupConfigurationValidator
    Extensions/           ApplicationBuilderExtensions (database init)
    Program.cs            Service registration and pipeline setup

  SchoolManagement.Application/
    Authentication/       AuthModels, IAuthService, PasswordResetSettings, validators
    Students/             StudentModels, IStudentService, validators
    Teachers/             TeacherModels, ITeacherService, validators
    Parents/              ParentModels, IParentService, validators
    Classes/              ClassModels, IClassService, validators
    Subjects/             SubjectModels, ISubjectService, validators
    Timetable/            TimetableModels, ITimetableService, validators
    Attendance/           AttendanceModels, IAttendanceService, validators
    Exams/                ExamModels, IExamService, validators
    Results/              ResultModels, IResultService, validators
    Fees/                 FeeModels, IFeeService, validators (includes Payment types)
    Notifications/        NotificationModels, INotificationService
    Reports/              ReportModels, IReportService, IReportPdfGenerator, validators
    Common/
      Interfaces/         ISecurityServices (IPasswordHasher, IPasswordResetNotifier)
      Models/             ApiResponse<T>, PagedResponse<T>, PaginationRequest, AppException

  SchoolManagement.Domain/
    Common/               BaseEntity (Id, CreatedAt, UpdatedAt)
    Entities/             All EF Core entity classes
    Enums/                AttendanceStatus, FeeStatus, Gender, PaymentMethod

  SchoolManagement.Infrastructure/
    Authentication/       JwtSettings, JwtTokenService, Pbkdf2PasswordHasher,
                          LoggingPasswordResetNotifier
    Reports/              QuestPdfReportGenerator
    DependencyInjection/  ServiceCollectionExtensions (registers infra services)

  SchoolManagement.Persistence/
    AppDbContext.cs
    EntityConfigurations/
    Migrations/
    Repositories/
    Seed/                 DataSeeder (roles + default admin)
    DependencyInjection/  ServiceCollectionExtensions (registers persistence services)

tests/
  SchoolManagement.Tests/
    Infrastructure/       SchoolManagementApiFactory, helpers
    Auth/                 AuthEndpointsTests
    Authorization/        AuthorizationApiTests
    Students/             StudentApiTests
    Fees/                 FeePaymentApiTests
    Reports/              ReportPdfApiTests
```

---

## Layers

### API Layer

- **Controllers** handle routing, role-based `[Authorize]` attributes, and self-access enforcement (e.g. Students can only read their own record).
- **GlobalExceptionMiddleware** catches `ValidationException`, `AppException`, `UnauthorizedAccessException`, and unhandled exceptions, and maps all of them to `ApiResponse<object>.Fail(...)` with the appropriate HTTP status code and `traceId`.
- **Program.cs** configures FluentValidation auto-validation, CORS, JWT bearer, Swagger, Serilog request logging, and runs database initialization on startup.

### Application Layer

- Contains DTOs (request/response records), service interfaces, and FluentValidation validators.
- No direct EF Core or infrastructure dependencies — depends only on domain types.
- `ApiResponse<T>` and `PagedResponse<T>` are defined here and used by both the API and test layers.

### Domain Layer

- Plain entity classes and enums.
- `BaseEntity` provides `Id` (Guid), `CreatedAt`, and `UpdatedAt`.

### Infrastructure Layer

- **JwtTokenService** — signs access tokens using HMAC-SHA256 and issues opaque refresh tokens stored in the database.
- **Pbkdf2PasswordHasher** — PBKDF2/SHA256, 350,000 iterations with a cryptographically random salt.
- **LoggingPasswordResetNotifier** — writes the reset token to the application log. Replace with an email or SMS implementation for production.
- **QuestPdfReportGenerator** — generates PDF reports for students, attendance, and fees using QuestPDF. All report data is queried by the `IReportService` before being passed to the generator.

### Persistence Layer

- `AppDbContext` — EF Core DbContext with Fluent API entity configurations.
- `DataSeeder` — seeds the four roles and the default admin account on first run when `Database:SeedDemoData=true`.
- Auto-migration runs on startup when `Database:AutoMigrate=true`.

---

## Authentication and Authorization

- JWT bearer tokens use HMAC-SHA256 signing. `ClockSkew = TimeSpan.Zero` — tokens expire exactly at their `exp` claim.
- The role claim is embedded in the access token. Authorization is enforced by `[Authorize(Roles = "...")]` attributes on controllers and actions.
- Refresh token rotation: every successful `/api/auth/refresh` call issues a new access token and a new refresh token. The previous refresh token is invalidated immediately.
- Password reset tokens are stored hashed in the `reset_tokens` table with an expiry timestamp. Tokens are single-use.
- `StartupConfigurationValidator` rejects the default development JWT secret key and an empty `AllowedOrigins` list when running outside the Development environment.

---

## Validation

FluentValidation validators live alongside their request models in the Application layer. `AddFluentValidationAutoValidation()` applies them automatically before controller actions run. Validation failures produce a `400 Bad Request` with the standard `ApiResponse` error map.

---

## Error Handling

| Thrown type | HTTP status | Response message |
|---|---|---|
| `ValidationException` | 400 | `"Validation failed"` + `errors` map |
| `AppException` (default) | 400 | Exception message |
| `AppException` (custom status) | Configured status | Exception message |
| `UnauthorizedAccessException` | 401 | `"Authentication is required..."` |
| Any other exception | 500 | `"An unexpected server error occurred."` |

All responses include the `traceId` field from `HttpContext.TraceIdentifier`.

---

## Configuration Reference

| Config key | Env var override | Default | Description |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | — | — | PostgreSQL connection string |
| `Database:AutoMigrate` | `DATABASE_AUTO_MIGRATE` | `true` | Apply EF migrations on startup |
| `Database:SeedDemoData` | `DATABASE_SEED_DEMO_DATA` | `true` | Seed roles and default admin |
| `AllowedOrigins:0` | `AllowedOrigins__0` | `http://localhost:3000` (dev only) | CORS origin |
| `Jwt:Issuer` | `JWT_ISSUER` | `SchoolManagement` | Token issuer claim |
| `Jwt:Audience` | `JWT_AUDIENCE` | `SchoolManagement.Client` | Token audience claim |
| `Jwt:SecretKey` | `JWT_SECRET_KEY` | *(dev default rejected in prod)* | HMAC signing key |
| `Jwt:AccessTokenExpiryMinutes` | `JWT_ACCESS_TOKEN_EXPIRY_MINUTES` | `60` | Access token lifetime |
| `Jwt:RefreshTokenExpiryDays` | `JWT_REFRESH_TOKEN_EXPIRY_DAYS` | `7` | Refresh token lifetime |
| `PasswordReset:TokenExpiryMinutes` | `PASSWORD_RESET_TOKEN_EXPIRY_MINUTES` | `30` | Reset token lifetime |
| `PasswordReset:FrontendResetUrl` | `PASSWORD_RESET_FRONTEND_URL` | `http://localhost:3000/reset-password` | URL included in reset notifications |
