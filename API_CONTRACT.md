# API Contract

Base URL: `/api`

All requests and responses use `application/json`. Protected endpoints require a JWT bearer token in the `Authorization` header.

---

## Standard Response Envelope

All endpoints return the same envelope shape:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { },
  "errors": null,
  "traceId": "00-abc123-def456-00"
}
```

| Field | Type | Description |
|---|---|---|
| `success` | bool | `true` on success, `false` on failure |
| `message` | string | Human-readable result message |
| `data` | object or null | Payload on success, `null` on failure |
| `errors` | object or null | Validation error map on 400, otherwise `null` |
| `traceId` | string or null | ASP.NET Core request trace identifier |

### Error Responses

**400 Validation Failure**
```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": {
    "email": ["'Email' must not be empty.", "'Email' is not a valid email address."],
    "password": ["The length of 'Password' must be at least 8 characters."]
  },
  "traceId": "00-abc123-def456-00"
}
```

**401 Unauthenticated**
```json
{
  "success": false,
  "message": "Authentication is required to access this resource.",
  "data": null,
  "errors": null,
  "traceId": "00-abc123-def456-00"
}
```

**403 Forbidden**
```json
{
  "success": false,
  "message": "You do not have permission to access this resource.",
  "data": null,
  "errors": null,
  "traceId": "00-abc123-def456-00"
}
```

**404 Not Found**
```json
{
  "success": false,
  "message": "Student with id 'a1b2c3d4-...' was not found.",
  "data": null,
  "errors": null,
  "traceId": "00-abc123-def456-00"
}
```

**500 Server Error**
```json
{
  "success": false,
  "message": "An unexpected server error occurred.",
  "data": null,
  "errors": null,
  "traceId": "00-abc123-def456-00"
}
```

---

## Pagination

List endpoints that support pagination accept the following query parameters:

| Parameter | Type | Default | Max |
|---|---|---|---|
| `pageNumber` | int | 1 | — |
| `pageSize` | int | 10 | 100 |

Paginated responses use this shape wrapped in the standard `data` field:

```json
{
  "items": [ ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 52
}
```

---

## Authentication

### POST /api/auth/login

Anonymous. Returns access and refresh tokens.

**Request**
```json
{
  "email": "admin@school.com",
  "password": "Admin@12345"
}
```

**Response 200**
```json
{
  "success": true,
  "message": "Login completed successfully.",
  "data": {
    "accessToken": "<jwt>",
    "refreshToken": "<opaque-token>",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "System Admin",
      "email": "admin@school.com",
      "role": "Admin"
    }
  }
}
```

Access token expires in 60 minutes by default. Refresh token expires in 7 days by default. Both values are configurable via `Jwt:AccessTokenExpiryMinutes` and `Jwt:RefreshTokenExpiryDays`.

---

### POST /api/auth/refresh

Anonymous. Exchanges a valid access + refresh token pair for new tokens. Both tokens are rotated on every successful call. The previous refresh token is invalidated.

**Request**
```json
{
  "accessToken": "<expired-or-valid-jwt>",
  "refreshToken": "<opaque-refresh-token>"
}
```

**Response 200** — same shape as login response.

**Response 401** — if the refresh token is invalid, expired, or already used.

---

### POST /api/auth/register

**Authorization: Admin only**

Creates a new user account. The `role` field must match an existing role name exactly (`Admin`, `Teacher`, `Student`, or `Parent`). Use dedicated CRUD endpoints to create the associated profile record after registration.

**Request**
```json
{
  "fullName": "Jane Smith",
  "email": "jane.smith@school.com",
  "password": "Secure@12345",
  "role": "Teacher"
}
```

**Response 200** — same shape as login response, including tokens for the new account.

---

### POST /api/auth/forgot-password

Anonymous. Initiates a password reset. Always returns the same generic success response regardless of whether the email exists, to prevent account enumeration.

In production, the reset link is delivered via the configured notification channel. In development, the token is written to the application log.

**Request**
```json
{
  "email": "jane.smith@school.com"
}
```

**Response 200**
```json
{
  "success": true,
  "message": "If an account exists for this email, a password reset link has been sent.",
  "data": {
    "message": "If an account exists for this email, a password reset link has been sent."
  }
}
```

---

### POST /api/auth/reset-password

Anonymous. Completes a password reset using the token from the forgot-password flow. Tokens are single-use and expire after 30 minutes by default (configurable via `PasswordReset:TokenExpiryMinutes`).

**Request**
```json
{
  "token": "<reset-token>",
  "newPassword": "NewSecure@12345",
  "confirmPassword": "NewSecure@12345"
}
```

**Response 200**
```json
{
  "success": true,
  "message": "Password has been reset successfully.",
  "data": null
}
```

**Response 400** — if the token is invalid, expired, or the passwords do not match.

---

## Students

### GET /api/students

**Authorization: Admin, Teacher**

Returns a paginated list of all students.

**Query:** `pageNumber`, `pageSize`

**Response 200**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "fullName": "Alice Johnson",
        "email": "alice.johnson@school.com",
        "phone": "+355 69 123 4567",
        "studentCode": "STD-001",
        "dateOfBirth": "2010-06-15",
        "gender": "Female",
        "admissionDate": "2023-09-01",
        "parentId": "a1b2c3d4-0000-0000-0000-000000000001",
        "parentName": "Robert Johnson",
        "classId": "b2c3d4e5-0000-0000-0000-000000000001",
        "className": "10A",
        "createdAt": "2024-01-10T09:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1
  }
}
```

---

### GET /api/students/me

**Authorization: Student only**

Returns the profile of the currently authenticated student.

**Response 200** — single `StudentResponse` object.

---

### GET /api/students/parent/me

**Authorization: Parent only**

Returns a paginated list of children linked to the currently authenticated parent.

**Query:** `pageNumber`, `pageSize`

**Response 200** — paginated `StudentResponse` list.

---

### GET /api/students/{id}

**Authorization: Admin, Teacher, Student (own record only)**

Students may only retrieve their own record. Attempting to retrieve another student's record returns `403`.

**Response 200** — single `StudentResponse` object.

---

### POST /api/students

**Authorization: Admin only**

Creates a new student and their associated user account.

**Request**
```json
{
  "fullName": "Alice Johnson",
  "email": "alice.johnson@school.com",
  "password": "Student@12345",
  "phone": "+355 69 123 4567",
  "address": "123 Main Street",
  "studentCode": "STD-001",
  "dateOfBirth": "2010-06-15",
  "gender": "Female",
  "admissionDate": "2023-09-01",
  "parentId": "a1b2c3d4-0000-0000-0000-000000000001",
  "classId": "b2c3d4e5-0000-0000-0000-000000000001"
}
```

`gender` accepts: `Male`, `Female`, `Other`

**Response 201** — the created `StudentResponse`.

---

### PUT /api/students/{id}

**Authorization: Admin only**

Updates an existing student. Does not update the associated user password.

**Request**
```json
{
  "fullName": "Alice Johnson",
  "email": "alice.johnson@school.com",
  "phone": "+355 69 123 4567",
  "address": "123 Main Street",
  "studentCode": "STD-001",
  "dateOfBirth": "2010-06-15",
  "gender": "Female",
  "admissionDate": "2023-09-01",
  "parentId": "a1b2c3d4-0000-0000-0000-000000000001",
  "classId": "b2c3d4e5-0000-0000-0000-000000000001"
}
```

**Response 200** — the updated `StudentResponse`.

---

### DELETE /api/students/{id}

**Authorization: Admin only**

**Response 200**
```json
{ "success": true, "message": "Student deleted successfully.", "data": null }
```

---

## Teachers

### GET /api/teachers

**Authorization: Admin**

Paginated list of all teachers. **Query:** `pageNumber`, `pageSize`

**Response 200**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "c3d4e5f6-0000-0000-0000-000000000001",
        "userId": "d4e5f6a7-0000-0000-0000-000000000001",
        "fullName": "David Brown",
        "email": "david.brown@school.com",
        "phone": "+355 69 987 6543",
        "teacherCode": "TCH-001",
        "specialization": "Mathematics",
        "hireDate": "2020-09-01",
        "createdAt": "2024-01-05T08:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1
  }
}
```

---

### GET /api/teachers/{id}

**Authorization: Admin**

---

### POST /api/teachers

**Authorization: Admin only**

```json
{
  "fullName": "David Brown",
  "email": "david.brown@school.com",
  "password": "Teacher@12345",
  "phone": "+355 69 987 6543",
  "address": "456 Oak Avenue",
  "teacherCode": "TCH-001",
  "specialization": "Mathematics",
  "hireDate": "2020-09-01"
}
```

**Response 201** — the created `TeacherResponse`.

---

### PUT /api/teachers/{id}

**Authorization: Admin only**

Same fields as create, without `password`.

**Response 200** — the updated `TeacherResponse`.

---

### DELETE /api/teachers/{id}

**Authorization: Admin only**

**Response 200**
```json
{ "success": true, "message": "Teacher deleted successfully.", "data": null }
```

---

## Parents

### GET /api/parents

**Authorization: Admin only**

Paginated list. **Query:** `pageNumber`, `pageSize`

---

### GET /api/parents/{id}

**Authorization: Admin, Parent (own record only)**

Parents may only retrieve their own record. Attempting another parent's record returns `403`.

---

### POST /api/parents

**Authorization: Admin only**

```json
{
  "fullName": "Robert Johnson",
  "email": "robert.johnson@school.com",
  "password": "Parent@12345",
  "phone": "+355 69 555 0001",
  "address": "789 Pine Road",
  "occupation": "Engineer"
}
```

**Response 201** — the created `ParentResponse`.

---

### PUT /api/parents/{id}

**Authorization: Admin only**

Same fields as create, without `password`.

---

### DELETE /api/parents/{id}

**Authorization: Admin only**

---

## Classes

### GET /api/classes

**Authorization: Admin, Teacher**

Paginated list. **Query:** `pageNumber`, `pageSize`

**Response 200**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "e5f6a7b8-0000-0000-0000-000000000001",
        "name": "Grade 10",
        "section": "A",
        "academicYear": "2025-2026",
        "classTeacherId": "c3d4e5f6-0000-0000-0000-000000000001",
        "classTeacherName": "David Brown",
        "studentCount": 28,
        "createdAt": "2024-08-01T00:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1
  }
}
```

---

### GET /api/classes/{id}

**Authorization: Admin, Teacher**

---

### POST /api/classes

**Authorization: Admin only**

```json
{
  "name": "Grade 10",
  "section": "A",
  "academicYear": "2025-2026",
  "classTeacherId": "c3d4e5f6-0000-0000-0000-000000000001"
}
```

`classTeacherId` is optional.

**Response 201** — the created `ClassResponse`.

---

### PUT /api/classes/{id}

**Authorization: Admin only**

---

### DELETE /api/classes/{id}

**Authorization: Admin only**

---

## Subjects

### GET /api/subjects

**Authorization: Admin, Teacher**

Paginated list. **Query:** `pageNumber`, `pageSize`

---

### GET /api/subjects/{id}

**Authorization: Admin, Teacher**

---

### POST /api/subjects

**Authorization: Admin only**

```json
{
  "name": "Mathematics",
  "code": "MATH-101",
  "description": "Core mathematics curriculum"
}
```

**Response 201** — the created `SubjectResponse`.

---

### PUT /api/subjects/{id}

**Authorization: Admin only**

---

### DELETE /api/subjects/{id}

**Authorization: Admin only**

---

## Timetable

### GET /api/timetable

**Authorization: Admin, Teacher**

Paginated list. **Query:** `pageNumber`, `pageSize`

---

### GET /api/timetable/class/{classId}

**Authorization: Admin, Teacher, Student, Parent**

Returns all timetable entries for a specific class. No pagination — returns a flat array.

**Response 200**
```json
{
  "success": true,
  "data": [
    {
      "id": "f6a7b8c9-0000-0000-0000-000000000001",
      "classId": "e5f6a7b8-0000-0000-0000-000000000001",
      "className": "10A",
      "subjectId": "a7b8c9d0-0000-0000-0000-000000000001",
      "subjectName": "Mathematics",
      "teacherId": "c3d4e5f6-0000-0000-0000-000000000001",
      "teacherName": "David Brown",
      "dayOfWeek": "Monday",
      "startTime": "08:00:00",
      "endTime": "09:00:00",
      "roomNumber": "101",
      "createdAt": "2024-08-01T00:00:00Z"
    }
  ]
}
```

---

### GET /api/timetable/{id}

**Authorization: Admin, Teacher**

---

### POST /api/timetable

**Authorization: Admin only**

```json
{
  "classId": "e5f6a7b8-0000-0000-0000-000000000001",
  "subjectId": "a7b8c9d0-0000-0000-0000-000000000001",
  "teacherId": "c3d4e5f6-0000-0000-0000-000000000001",
  "dayOfWeek": "Monday",
  "startTime": "08:00:00",
  "endTime": "09:00:00",
  "roomNumber": "101"
}
```

**Response 201** — the created `TimetableEntryResponse`.

---

### PUT /api/timetable/{id}

**Authorization: Admin only**

---

### DELETE /api/timetable/{id}

**Authorization: Admin only**

---

## Attendance

### GET /api/attendance

**Authorization: Admin, Teacher**

Paginated list of all attendance records. **Query:** `pageNumber`, `pageSize`

---

### GET /api/attendance/{id}

**Authorization: Admin, Teacher**

---

### GET /api/attendance/student/{studentId}

**Authorization: Admin, Teacher, Student (own records only), Parent (own children only)**

Students may only request their own `studentId`. Parents may only request a `studentId` belonging to one of their linked children. Both return `403` on violation.

Returns a flat array (no pagination).

**Response 200**
```json
{
  "success": true,
  "data": [
    {
      "id": "b8c9d0e1-0000-0000-0000-000000000001",
      "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "studentName": "Alice Johnson",
      "classId": "e5f6a7b8-0000-0000-0000-000000000001",
      "className": "10A",
      "subjectId": "a7b8c9d0-0000-0000-0000-000000000001",
      "subjectName": "Mathematics",
      "teacherId": "c3d4e5f6-0000-0000-0000-000000000001",
      "teacherName": "David Brown",
      "date": "2026-04-10",
      "status": "Present",
      "remarks": null,
      "createdAt": "2026-04-10T08:05:00Z"
    }
  ]
}
```

`status` values: `Present`, `Absent`, `Late`, `Excused`

---

### POST /api/attendance

**Authorization: Admin, Teacher**

```json
{
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "classId": "e5f6a7b8-0000-0000-0000-000000000001",
  "subjectId": "a7b8c9d0-0000-0000-0000-000000000001",
  "teacherId": "c3d4e5f6-0000-0000-0000-000000000001",
  "date": "2026-04-10",
  "status": "Present",
  "remarks": null
}
```

`date` must not be in the future.

**Response 201** — the created `AttendanceResponse`.

---

### PUT /api/attendance/{id}

**Authorization: Admin, Teacher**

Same fields as create.

**Response 200** — the updated `AttendanceResponse`.

---

## Exams

### GET /api/exams

**Authorization: Admin, Teacher, Student**

Paginated list. **Query:** `pageNumber`, `pageSize`

---

### GET /api/exams/{id}

**Authorization: Admin, Teacher, Student**

---

### GET /api/exams/class/{classId}

**Authorization: Admin, Teacher, Student, Parent**

Returns all exams for a specific class as a flat array.

---

### POST /api/exams

**Authorization: Admin, Teacher**

```json
{
  "classId": "e5f6a7b8-0000-0000-0000-000000000001",
  "subjectId": "a7b8c9d0-0000-0000-0000-000000000001",
  "title": "Q1 Mathematics Exam",
  "examDate": "2026-05-15",
  "totalMarks": 100
}
```

**Response 201** — the created `ExamResponse`.

---

### PUT /api/exams/{id}

**Authorization: Admin, Teacher**

---

### DELETE /api/exams/{id}

**Authorization: Admin, Teacher**

---

## Results

### GET /api/results

**Authorization: Admin, Teacher**

Paginated list. **Query:** `pageNumber`, `pageSize`

---

### GET /api/results/{id}

**Authorization: Admin, Teacher, Student, Parent**

---

### GET /api/results/student/{studentId}

**Authorization: Admin, Teacher, Student (own records only), Parent (own children only)**

Self-access enforcement mirrors attendance. Returns `403` on violation.

Returns a flat array.

**Response 200**
```json
{
  "success": true,
  "data": [
    {
      "id": "c9d0e1f2-0000-0000-0000-000000000001",
      "examId": "d0e1f2a3-0000-0000-0000-000000000001",
      "examTitle": "Q1 Mathematics Exam",
      "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "studentName": "Alice Johnson",
      "classId": "e5f6a7b8-0000-0000-0000-000000000001",
      "className": "10A",
      "subjectId": "a7b8c9d0-0000-0000-0000-000000000001",
      "subjectName": "Mathematics",
      "marksObtained": 88,
      "totalMarks": 100,
      "grade": "A",
      "remarks": "Excellent work",
      "createdAt": "2026-05-20T10:00:00Z"
    }
  ]
}
```

---

### POST /api/results

**Authorization: Admin, Teacher**

```json
{
  "examId": "d0e1f2a3-0000-0000-0000-000000000001",
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "marksObtained": 88,
  "grade": "A",
  "remarks": "Excellent work"
}
```

**Response 201** — the created `ResultResponse`.

---

### PUT /api/results/{id}

**Authorization: Admin, Teacher**

---

## Fees

### GET /api/fees

**Authorization: Admin only**

Paginated list with optional filters.

**Query:**

| Parameter | Type | Description |
|---|---|---|
| `pageNumber` | int | |
| `pageSize` | int | |
| `studentId` | guid | Filter by student |
| `status` | string | `Pending`, `PartiallyPaid`, `Paid`, `Overdue` |
| `dueDateFrom` | date | `yyyy-MM-dd` |
| `dueDateTo` | date | `yyyy-MM-dd` |

**Response 200**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "e1f2a3b4-0000-0000-0000-000000000001",
        "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "studentName": "Alice Johnson",
        "feeType": "Monthly Tuition",
        "amount": 150.00,
        "dueDate": "2026-05-01",
        "status": "Pending",
        "payments": [],
        "createdAt": "2026-04-01T00:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1
  }
}
```

---

### GET /api/fees/{id}

**Authorization: Admin, Student (own fees only), Parent (own children only)**

---

### GET /api/fees/student/{studentId}

**Authorization: Admin, Student (own fees only), Parent (own children only)**

Returns a flat array.

---

### POST /api/fees

**Authorization: Admin only**

```json
{
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "feeType": "Monthly Tuition",
  "amount": 150.00,
  "dueDate": "2026-05-01",
  "status": "Pending"
}
```

`status` accepts: `Pending`, `PartiallyPaid`, `Paid`, `Overdue`

**Response 201** — the created `FeeResponse`.

---

### PUT /api/fees/{id}

**Authorization: Admin only**

---

### DELETE /api/fees/{id}

**Authorization: Admin only**

---

## Payments

### GET /api/payments

**Authorization: Admin only**

Paginated list with optional filters.

**Query:**

| Parameter | Type | Description |
|---|---|---|
| `pageNumber` | int | |
| `pageSize` | int | |
| `studentId` | guid | Filter by student |
| `feeId` | guid | Filter by fee |
| `dateFrom` | datetime | ISO 8601 |
| `dateTo` | datetime | ISO 8601 |

---

### POST /api/payments

**Authorization: Admin only**

```json
{
  "feeId": "e1f2a3b4-0000-0000-0000-000000000001",
  "amountPaid": 150.00,
  "paymentDate": "2026-04-11T14:30:00Z",
  "paymentMethod": "Card",
  "transactionReference": "TXN-20260411-001"
}
```

`paymentMethod` accepts: `Cash`, `Card`, `BankTransfer`, `Online`

**Response 200**
```json
{
  "success": true,
  "message": "Payment recorded successfully.",
  "data": {
    "id": "f2a3b4c5-0000-0000-0000-000000000001",
    "feeId": "e1f2a3b4-0000-0000-0000-000000000001",
    "feeType": "Monthly Tuition",
    "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "studentName": "Alice Johnson",
    "amountPaid": 150.00,
    "paymentDate": "2026-04-11T14:30:00Z",
    "paymentMethod": "Card",
    "transactionReference": "TXN-20260411-001"
  }
}
```

---

## Notifications

### GET /api/notifications

**Authorization: All roles**

Returns the current user's notifications, paginated. **Query:** `pageNumber`, `pageSize`

**Response 200**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "a3b4c5d6-0000-0000-0000-000000000001",
        "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "title": "Fee Due Reminder",
        "message": "Your monthly tuition fee of 150.00 is due on 2026-05-01.",
        "isRead": false,
        "createdAt": "2026-04-10T09:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1
  }
}
```

---

### PATCH /api/notifications/{id}/read

**Authorization: All roles**

Marks a specific notification as read. Only the owner of the notification can mark it.

**Response 200** — the updated `NotificationResponse`.

---

### PATCH /api/notifications/read-all

**Authorization: All roles**

Marks all of the current user's notifications as read.

**Response 200**
```json
{ "success": true, "message": "Notifications marked as read.", "data": null }
```

---

## Reports (PDF Export)

### GET /api/reports/{type}/pdf

**Authorization: Admin only**

Generates and streams a PDF report. Returns `application/pdf` with a `Content-Disposition: attachment` header.

**Path parameter `type`:** `students`, `attendance`, or `fees`

**Query parameters:**

| Parameter | Type | Description |
|---|---|---|
| `classId` | guid | Filter by class (optional) |
| `studentId` | guid | Filter by student (optional) |
| `dateFrom` | date | `yyyy-MM-dd` — inclusive start date (optional) |
| `dateTo` | date | `yyyy-MM-dd` — inclusive end date (optional) |

`dateFrom` must not be later than `dateTo`. Providing both or neither is valid.

**Response 200**

Binary PDF stream.

```
Content-Type: application/pdf
Content-Disposition: attachment; filename="school-management-students-report-20260411-143000.pdf"
```

Filename pattern: `school-management-{type}-report-{yyyyMMdd-HHmmss}.pdf`

**Response 400** — if `type` is not one of the supported values, or if `dateFrom` is later than `dateTo`.

```json
{
  "success": false,
  "message": "Unsupported report type.",
  "data": null,
  "errors": null,
  "traceId": "00-abc123-def456-00"
}
```

---

## Health Check

### GET /health

Anonymous. Returns `200 OK` with `{ "status": "ok" }` when the API process is running. Does not check database connectivity.

---

## Role Reference

| Role | Description |
|---|---|
| `Admin` | Full access to all endpoints |
| `Teacher` | Read access to students, attendance, exams, results, timetable; can create/update attendance, exams, and results |
| `Student` | Self-access only: own profile, attendance, results, fees, timetable for own class, notifications |
| `Parent` | Child-scoped access: linked children's attendance, results, fees; own notifications |

The `register` endpoint is Admin-only. New user accounts are created exclusively by the admin from within the system.
