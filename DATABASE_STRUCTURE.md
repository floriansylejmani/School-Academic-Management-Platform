# Database Structure

PostgreSQL 16. All primary keys are UUIDs (Guid). EF Core manages migrations under `src/SchoolManagement.Persistence/Migrations/`.

---

## Tables

### roles
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar | Unique. Seeded values: `Admin`, `Teacher`, `Student`, `Parent` |
| created_at | timestamptz | |

### users
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| role_id | uuid FK → roles.id | |
| full_name | varchar(150) | |
| email | varchar(150) | Unique, stored lowercase |
| password_hash | varchar | PBKDF2/SHA256 hash with salt |
| phone | varchar(30) | Nullable |
| address | varchar(250) | Nullable |
| is_active | bool | |
| created_at | timestamptz | |
| updated_at | timestamptz | Nullable |

### parents
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| user_id | uuid FK → users.id | Unique |
| occupation | varchar(100) | Nullable |
| created_at | timestamptz | |

### teachers
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| user_id | uuid FK → users.id | Unique |
| teacher_code | varchar(50) | Unique |
| specialization | varchar(100) | |
| hire_date | date | |
| created_at | timestamptz | |

### students
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| user_id | uuid FK → users.id | Unique |
| parent_id | uuid FK → parents.id | Nullable |
| class_id | uuid FK → academic_classes.id | Nullable |
| student_code | varchar(50) | Unique |
| date_of_birth | date | |
| gender | int | Enum: 1=Male, 2=Female, 3=Other |
| admission_date | date | |
| created_at | timestamptz | |

### academic_classes
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(50) | |
| section | varchar(20) | |
| academic_year | varchar(20) | |
| class_teacher_id | uuid FK → teachers.id | Nullable |
| created_at | timestamptz | |

### subjects
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(100) | |
| code | varchar(30) | Unique |
| description | varchar(500) | Nullable |
| created_at | timestamptz | |

### timetable_entries
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| class_id | uuid FK → academic_classes.id | |
| subject_id | uuid FK → subjects.id | |
| teacher_id | uuid FK → teachers.id | |
| day_of_week | varchar | e.g. `Monday` |
| start_time | time | |
| end_time | time | end_time > start_time |
| room_number | varchar(30) | Nullable |
| created_at | timestamptz | |

### attendance_records
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| student_id | uuid FK → students.id | |
| class_id | uuid FK → academic_classes.id | |
| subject_id | uuid FK → subjects.id | |
| teacher_id | uuid FK → teachers.id | |
| date | date | Indexed |
| status | int | Enum: 1=Present, 2=Absent, 3=Late, 4=Excused |
| remarks | varchar(250) | Nullable |
| created_at | timestamptz | |

### exams
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| class_id | uuid FK → academic_classes.id | |
| subject_id | uuid FK → subjects.id | |
| title | varchar(100) | |
| exam_date | date | |
| total_marks | decimal | > 0 |
| created_at | timestamptz | |

### results
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| exam_id | uuid FK → exams.id | |
| student_id | uuid FK → students.id | |
| marks_obtained | decimal | ≥ 0 |
| grade | varchar(10) | |
| remarks | varchar(250) | Nullable |
| created_at | timestamptz | |

### fees
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| student_id | uuid FK → students.id | Indexed |
| fee_type | varchar(100) | |
| amount | decimal | ≥ 0 |
| due_date | date | |
| status | int | Enum: 1=Pending, 2=PartiallyPaid, 3=Paid, 4=Overdue |
| created_at | timestamptz | |

### payments
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| fee_id | uuid FK → fees.id | |
| amount_paid | decimal | > 0 |
| payment_date | timestamptz | |
| payment_method | int | Enum: 1=Cash, 2=Card, 3=BankTransfer, 4=Online |
| transaction_reference | varchar(100) | Nullable |
| created_at | timestamptz | |

### notifications
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| user_id | uuid FK → users.id | Indexed |
| title | varchar | |
| message | text | |
| is_read | bool | |
| created_at | timestamptz | |

### reset_tokens
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| user_id | uuid FK → users.id | |
| token_hash | varchar | SHA-256 hash of the raw token |
| expires_at | timestamptz | |
| used_at | timestamptz | Nullable. Set on successful reset |
| created_at | timestamptz | |

---

## Key Relationships

- `roles` 1 → many `users`
- `users` 1 → 0..1 `parent` / `teacher` / `student`
- `parents` 1 → many `students`
- `academic_classes` 0..1 → 1 teacher (class teacher)
- `students` many → 1 `academic_classes`
- `timetable_entries` links class + subject + teacher per time slot
- `attendance_records` links student + class + subject + teacher per date
- `exams` 1 → many `results`
- `fees` 1 → many `payments`
- `users` 1 → many `notifications`
- `users` 1 → many `reset_tokens`

---

## Indexes

| Table | Column(s) |
|---|---|
| users | email |
| students | student_code |
| teachers | teacher_code |
| attendance_records | date |
| fees | student_id |
| notifications | user_id |

---

## Seed Data

`DataSeeder` runs on startup when `Database:SeedDemoData=true`. It inserts:

- Roles: `Admin`, `Teacher`, `Student`, `Parent`
- One admin user: `admin@school.com` / `Admin@12345`
