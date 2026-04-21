# Frontend Architecture

## Stack

| Technology | Role |
|---|---|
| Next.js 15 (App Router) | Framework, routing, SSR |
| TypeScript | Type safety |
| Tailwind CSS | Styling |
| Axios | HTTP client |
| TanStack Query (React Query) | Server state, caching, mutation |
| Zustand | Client state (auth session, toast, UI) |
| React Hook Form | Form state management |
| Zod | Schema validation for form inputs |

---

## Directory Structure

```
frontend/
  src/
    app/
      (auth)/
        login/              Login page
        forgot-password/    Forgot password page
        reset-password/     Reset password page (reads ?token= from URL)
      (dashboard)/
        admin/              Admin portal pages
          students/
          teachers/
          parents/
          classes/
          subjects/
          timetable/
          attendance/
          exams/
          results/
          fees/
          payments/
          reports/
          dashboard/
        teacher/            Teacher portal pages
        student/            Student portal pages
        parent/             Parent portal pages

    components/
      layout/               Sidebar, Header, DashboardLayout
      ui/                   Shared UI components (Button, Input, Modal, Table, etc.)

    features/               Domain-specific components, hooks, types, and schemas
      auth/
      attendance/
      classes/
      dashboard/
      exams/
      fees/
      notifications/
      parent-portal/
      parents/
      profile/
      reports/
      results/
      student-portal/
      students/
      subjects/
      teachers/
      timetable/

    hooks/
      use-toast.ts

    lib/
      query-client.ts       TanStack Query client instance

    services/               Axios API service modules
      apiClient.ts          Axios instance with auth interceptors
      apiConfig.ts          Base URL configuration
      auth.service.ts
      students.service.ts
      teachers.service.ts
      parents.service.ts
      classes.service.ts
      subjects.service.ts
      timetable.service.ts
      attendance.service.ts
      exams.service.ts
      results.service.ts
      fees.service.ts
      notifications.service.ts
      reports.service.ts
      profile.service.ts
      service-helpers.ts    Shared helpers (e.g. downloadBlob)

    store/
      auth.store.ts         Zustand store: current user, tokens, login/logout actions
      toast.store.ts        Zustand store: toast notifications
      ui.store.ts           Zustand store: sidebar state, other UI flags

    types/
      auth.ts               AuthResponse, AuthenticatedUser types
      common.ts             ApiResponse<T>, PagedResponse<T>, PaginationParams
      navigation.ts         Role-based navigation item types

    utils/
      api.ts                Axios error helpers
      auth.ts               Token decode/access helpers
      cn.ts                 Tailwind class merge utility
      navigation.ts         Role → navigation items mapping
```

---

## Auth Flow

1. User submits credentials on `/login`.
2. `auth.service.ts` calls `POST /api/auth/login`.
3. On success, `auth.store.ts` stores the access token, refresh token, and user object.
4. `apiClient.ts` attaches the access token to all requests via an Axios request interceptor.
5. A response interceptor handles `401` responses by attempting a token refresh via `POST /api/auth/refresh`. On refresh success, the original request is retried. On refresh failure, the user is logged out and redirected to `/login`.
6. After login, the user is redirected to their role-specific dashboard: `/admin/dashboard`, `/teacher/dashboard`, `/student/dashboard`, or `/parent/dashboard`.
7. Logout clears the Zustand store and redirects to `/login`.

---

## Forgot / Reset Password Flow

1. User submits their email on `/forgot-password`.
2. `auth.service.ts` calls `POST /api/auth/forgot-password`.
3. The UI always shows the generic success message regardless of whether the email exists.
4. The backend sends a reset link (in development, logs the token to the console). The link points to `/reset-password?token=<token>`.
5. User opens the reset link, submits a new password and confirmation.
6. `auth.service.ts` calls `POST /api/auth/reset-password` with the token from the query string.
7. On success, the user is redirected to `/login`.

---

## PDF Reports

PDF downloads are handled in `reports.service.ts`:

```ts
reportsService.downloadPdf(type, filters)
// type: "students" | "attendance" | "fees"
// filters: { classId?, studentId?, dateFrom?, dateTo? }
```

The service uses Axios with `responseType: "blob"`. The filename is extracted from the `Content-Disposition` response header. A `downloadBlob` helper in `service-helpers.ts` creates an object URL and triggers the browser download.

The reports feature components live in `features/reports/`. The admin reports page is at `/admin/reports`.

---

## Role-Based Access

Route protection is handled in Next.js middleware or layout components. Each portal prefix is accessible only to users with the corresponding role:

| Path prefix | Required role |
|---|---|
| `/admin/*` | Admin |
| `/teacher/*` | Teacher |
| `/student/*` | Student |
| `/parent/*` | Parent |

The `navigation.ts` utility maps each role to its sidebar navigation items. The sidebar renders only items allowed for the authenticated user's role.

---

## Environment Variables

| Variable | Description |
|---|---|
| `NEXT_PUBLIC_API_URL` | API base URL, e.g. `http://localhost:5000/api` |

This variable is compiled into the Next.js bundle at build time. Rebuild the image when changing it.

Create `frontend/.env.local` for local development:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```
