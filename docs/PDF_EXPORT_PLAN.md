# PDF Report Export

Server-side PDF generation is implemented using QuestPDF. Reports are generated on demand via the `GET /api/reports/{type}/pdf` endpoint and streamed directly to the client as a binary PDF file.

---

## Endpoint

```
GET /api/reports/{type}/pdf
```

**Authorization:** Admin only

**Supported types:**

| `{type}` | Description |
|---|---|
| `students` | Student roster with academic summary |
| `attendance` | Attendance records with status breakdown |
| `fees` | Fee records with payment summary |

**Query filters (all optional):**

| Parameter | Type | Description |
|---|---|---|
| `classId` | guid | Limit results to one class |
| `studentId` | guid | Limit results to one student |
| `dateFrom` | date (`yyyy-MM-dd`) | Inclusive start date |
| `dateTo` | date (`yyyy-MM-dd`) | Inclusive end date |

`dateFrom` must not be later than `dateTo`. Filters can be combined freely. Omitting all filters returns data across the entire school.

---

## Response

**200 OK** — binary PDF stream

```
Content-Type: application/pdf
Content-Disposition: attachment; filename="school-management-students-report-20260411-143000.pdf"
```

Filename pattern: `school-management-{type}-report-{yyyyMMdd-HHmmss}.pdf`

**400 Bad Request** — unsupported `type` value, or `dateFrom > dateTo`

```json
{
  "success": false,
  "message": "Unsupported report type.",
  "data": null,
  "errors": null,
  "traceId": "..."
}
```

---

## Backend Implementation

The report pipeline has three steps:

1. `IReportService` (implemented in the Persistence layer) queries the database and returns a typed report data object:
   - `StudentsPdfReportData` — metadata + collection of `StudentReportRow`
   - `AttendancePdfReportData` — metadata + status counts + collection of `AttendanceReportRow`
   - `FeesPdfReportData` — metadata + totals + collection of `FeeReportRow`

2. `IReportPdfGenerator` (implemented in the Infrastructure layer as `QuestPdfReportGenerator`) receives the data object and returns a `byte[]` PDF.

3. `ReportsController.DownloadPdf` calls both in sequence and returns the result as `File(bytes, "application/pdf", filename)`.

Report metadata includes the report title, applied filters (class name, student name, date range), and the UTC generation timestamp.

---

## Frontend Integration

PDF downloads are handled in `frontend/src/services/reports.service.ts`:

```ts
const { blob, fileName } = await reportsService.downloadPdf("students", {
  classId: "e5f6a7b8-...",
  dateFrom: "2026-01-01",
  dateTo: "2026-04-11"
});
```

The service uses Axios with `responseType: "blob"`. The filename is extracted from the `Content-Disposition` header. A shared helper triggers the browser download by creating and clicking a temporary object URL.

The admin reports page is at `/admin/reports`. It renders filter inputs and download buttons for each report type.

---

## Report Content

### Students Report

Each row includes:
- Student name, student code, email, class name, parent name
- Admission date
- Attendance percentage
- Average score percentage
- Total billed, total paid, outstanding balance
- Result count

### Attendance Report

Summary section includes counts of `Present`, `Absent`, `Late`, and `Excused` records.

Each row includes:
- Student name, class name, subject name, teacher name
- Date, status, remarks

### Fees Report

Summary section includes total billed, total paid, and total outstanding.

Each row includes:
- Student name, class name, fee type
- Amount, paid amount, outstanding balance, due date, status
