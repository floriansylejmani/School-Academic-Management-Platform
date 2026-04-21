-- =============================================================================
-- School Management System — PostgreSQL Schema
-- Engine  : PostgreSQL 15+
-- ORM     : Entity Framework Core 9 (ASP.NET Core backend)
-- Pattern : UUID PKs · timestamptz · enums as VARCHAR · strategic cascades
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Extensions
-- ---------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()

-- ---------------------------------------------------------------------------
-- Drop in reverse dependency order (safe re-run)
-- ---------------------------------------------------------------------------
DROP TABLE IF EXISTS notifications              CASCADE;
DROP TABLE IF EXISTS enrollments               CASCADE;
DROP TABLE IF EXISTS payments                  CASCADE;
DROP TABLE IF EXISTS fees                      CASCADE;
DROP TABLE IF EXISTS results                   CASCADE;
DROP TABLE IF EXISTS exams                     CASCADE;
DROP TABLE IF EXISTS attendance_records        CASCADE;
DROP TABLE IF EXISTS timetable_entries         CASCADE;
DROP TABLE IF EXISTS teacher_subject_assignments CASCADE;
DROP TABLE IF EXISTS students                  CASCADE;
DROP TABLE IF EXISTS subjects                  CASCADE;
DROP TABLE IF EXISTS academic_classes          CASCADE;
DROP TABLE IF EXISTS teachers                  CASCADE;
DROP TABLE IF EXISTS parents                   CASCADE;
DROP TABLE IF EXISTS users                     CASCADE;
DROP TABLE IF EXISTS roles                     CASCADE;

-- =============================================================================
-- 1. ROLES
-- =============================================================================
CREATE TABLE roles (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(50) NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT uq_roles_name UNIQUE (name)
);

CREATE INDEX idx_roles_name ON roles (name);

-- Seed core roles
INSERT INTO roles (name) VALUES
    ('Admin'),
    ('Teacher'),
    ('Student'),
    ('Parent');

-- =============================================================================
-- 2. USERS
-- =============================================================================
CREATE TABLE users (
    id                       UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id                  UUID         NOT NULL,
    full_name                VARCHAR(150) NOT NULL,
    email                    VARCHAR(150) NOT NULL,
    password_hash            TEXT         NOT NULL,
    phone                    VARCHAR(30),
    address                  VARCHAR(250),
    is_active                BOOLEAN      NOT NULL DEFAULT TRUE,
    refresh_token            VARCHAR(500),
    refresh_token_expires_at TIMESTAMPTZ,
    created_at               TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ,

    CONSTRAINT uq_users_email  UNIQUE (email),
    CONSTRAINT fk_users_role   FOREIGN KEY (role_id)
        REFERENCES roles (id) ON DELETE RESTRICT
);

CREATE INDEX idx_users_email   ON users (email);
CREATE INDEX idx_users_role_id ON users (role_id);

-- =============================================================================
-- 3. PARENTS
-- =============================================================================
CREATE TABLE parents (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID         NOT NULL,
    occupation  VARCHAR(100),
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT uq_parents_user_id UNIQUE (user_id),
    CONSTRAINT fk_parents_user    FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX idx_parents_user_id ON parents (user_id);

-- =============================================================================
-- 4. TEACHERS
-- =============================================================================
CREATE TABLE teachers (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        NOT NULL,
    teacher_code    VARCHAR(50) NOT NULL,
    specialization  VARCHAR(100) NOT NULL,
    hire_date       DATE        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ,

    CONSTRAINT uq_teachers_user_id      UNIQUE (user_id),
    CONSTRAINT uq_teachers_teacher_code UNIQUE (teacher_code),
    CONSTRAINT fk_teachers_user         FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX idx_teachers_user_id      ON teachers (user_id);
CREATE INDEX idx_teachers_teacher_code ON teachers (teacher_code);

-- =============================================================================
-- 5. ACADEMIC CLASSES
--    class_teacher_id is nullable — teacher may not be assigned yet.
--    FK to teachers uses SET NULL so deleting a teacher does not
--    cascade-delete the class.
-- =============================================================================
CREATE TABLE academic_classes (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name             VARCHAR(50) NOT NULL,
    section          VARCHAR(20) NOT NULL,
    academic_year    VARCHAR(20) NOT NULL,
    class_teacher_id UUID,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ,

    CONSTRAINT fk_academic_classes_teacher FOREIGN KEY (class_teacher_id)
        REFERENCES teachers (id) ON DELETE SET NULL
);

CREATE INDEX idx_academic_classes_teacher_id ON academic_classes (class_teacher_id);

-- =============================================================================
-- 6. SUBJECTS
-- =============================================================================
CREATE TABLE subjects (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(100) NOT NULL,
    code        VARCHAR(30)  NOT NULL,
    description VARCHAR(500),
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT uq_subjects_code UNIQUE (code)
);

CREATE INDEX idx_subjects_code ON subjects (code);

-- =============================================================================
-- 7. STUDENTS
--    parent_id  → SET NULL (student survives if parent profile is deleted)
--    class_id   → SET NULL (student survives if class is removed)
-- =============================================================================
CREATE TABLE students (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        NOT NULL,
    parent_id       UUID,
    class_id        UUID,
    student_code    VARCHAR(50) NOT NULL,
    date_of_birth   DATE        NOT NULL,
    gender          VARCHAR(20) NOT NULL,   -- 'Male' | 'Female' | 'Other'
    admission_date  DATE        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ,

    CONSTRAINT uq_students_user_id      UNIQUE (user_id),
    CONSTRAINT uq_students_student_code UNIQUE (student_code),
    CONSTRAINT ck_students_gender       CHECK (gender IN ('Male', 'Female', 'Other')),

    CONSTRAINT fk_students_user   FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_students_parent FOREIGN KEY (parent_id)
        REFERENCES parents (id) ON DELETE SET NULL,
    CONSTRAINT fk_students_class  FOREIGN KEY (class_id)
        REFERENCES academic_classes (id) ON DELETE SET NULL
);

CREATE INDEX idx_students_user_id      ON students (user_id);
CREATE INDEX idx_students_parent_id    ON students (parent_id);
CREATE INDEX idx_students_class_id     ON students (class_id);
CREATE INDEX idx_students_student_code ON students (student_code);

-- =============================================================================
-- 8. TEACHER SUBJECT ASSIGNMENTS  (junction: teacher × subject × class)
-- =============================================================================
CREATE TABLE teacher_subject_assignments (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    teacher_id  UUID        NOT NULL,
    subject_id  UUID        NOT NULL,
    class_id    UUID        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT uq_teacher_subject_class UNIQUE (teacher_id, subject_id, class_id),

    CONSTRAINT fk_tsa_teacher FOREIGN KEY (teacher_id)
        REFERENCES teachers (id) ON DELETE CASCADE,
    CONSTRAINT fk_tsa_subject FOREIGN KEY (subject_id)
        REFERENCES subjects (id) ON DELETE CASCADE,
    CONSTRAINT fk_tsa_class   FOREIGN KEY (class_id)
        REFERENCES academic_classes (id) ON DELETE CASCADE
);

CREATE INDEX idx_tsa_teacher_id ON teacher_subject_assignments (teacher_id);
CREATE INDEX idx_tsa_subject_id ON teacher_subject_assignments (subject_id);
CREATE INDEX idx_tsa_class_id   ON teacher_subject_assignments (class_id);

-- =============================================================================
-- 9. TIMETABLE ENTRIES
--    day_of_week: 1 = Monday … 7 = Sunday  (.NET DayOfWeek-compatible)
-- =============================================================================
CREATE TABLE timetable_entries (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    class_id    UUID        NOT NULL,
    subject_id  UUID        NOT NULL,
    teacher_id  UUID        NOT NULL,
    day_of_week SMALLINT    NOT NULL,
    start_time  TIME        NOT NULL,   -- TIME WITHOUT TIME ZONE
    end_time    TIME        NOT NULL,
    room_number VARCHAR(30),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT ck_timetable_day_of_week CHECK (day_of_week BETWEEN 1 AND 7),
    CONSTRAINT ck_timetable_time_order  CHECK (end_time > start_time),

    CONSTRAINT fk_timetable_class   FOREIGN KEY (class_id)
        REFERENCES academic_classes (id) ON DELETE CASCADE,
    CONSTRAINT fk_timetable_subject FOREIGN KEY (subject_id)
        REFERENCES subjects (id) ON DELETE CASCADE,
    CONSTRAINT fk_timetable_teacher FOREIGN KEY (teacher_id)
        REFERENCES teachers (id) ON DELETE CASCADE
);

CREATE INDEX idx_timetable_class_id   ON timetable_entries (class_id);
CREATE INDEX idx_timetable_subject_id ON timetable_entries (subject_id);
CREATE INDEX idx_timetable_teacher_id ON timetable_entries (teacher_id);
CREATE INDEX idx_timetable_day        ON timetable_entries (day_of_week);

-- =============================================================================
-- 10. ATTENDANCE RECORDS
-- =============================================================================
CREATE TABLE attendance_records (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id  UUID         NOT NULL,
    class_id    UUID         NOT NULL,
    subject_id  UUID         NOT NULL,
    teacher_id  UUID         NOT NULL,
    date        DATE         NOT NULL,
    status      VARCHAR(20)  NOT NULL,   -- 'Present' | 'Absent' | 'Late' | 'Excused'
    remarks     VARCHAR(250),
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT ck_attendance_status CHECK (status IN ('Present', 'Absent', 'Late', 'Excused')),

    CONSTRAINT fk_attendance_student FOREIGN KEY (student_id)
        REFERENCES students (id) ON DELETE CASCADE,
    CONSTRAINT fk_attendance_class   FOREIGN KEY (class_id)
        REFERENCES academic_classes (id) ON DELETE CASCADE,
    CONSTRAINT fk_attendance_subject FOREIGN KEY (subject_id)
        REFERENCES subjects (id) ON DELETE CASCADE,
    CONSTRAINT fk_attendance_teacher FOREIGN KEY (teacher_id)
        REFERENCES teachers (id) ON DELETE CASCADE
);

CREATE INDEX idx_attendance_student_id ON attendance_records (student_id);
CREATE INDEX idx_attendance_class_id   ON attendance_records (class_id);
CREATE INDEX idx_attendance_subject_id ON attendance_records (subject_id);
CREATE INDEX idx_attendance_teacher_id ON attendance_records (teacher_id);
CREATE INDEX idx_attendance_date       ON attendance_records (date);
-- Covering index for the common "student attendance by date range" query
CREATE INDEX idx_attendance_student_date ON attendance_records (student_id, date);

-- =============================================================================
-- 11. EXAMS
-- =============================================================================
CREATE TABLE exams (
    id           UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    class_id     UUID          NOT NULL,
    subject_id   UUID          NOT NULL,
    title        VARCHAR(100)  NOT NULL,
    exam_date    DATE          NOT NULL,
    total_marks  NUMERIC(10,2) NOT NULL,
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ,

    CONSTRAINT ck_exams_total_marks CHECK (total_marks > 0),

    CONSTRAINT fk_exams_class   FOREIGN KEY (class_id)
        REFERENCES academic_classes (id) ON DELETE CASCADE,
    CONSTRAINT fk_exams_subject FOREIGN KEY (subject_id)
        REFERENCES subjects (id) ON DELETE CASCADE
);

CREATE INDEX idx_exams_class_id   ON exams (class_id);
CREATE INDEX idx_exams_subject_id ON exams (subject_id);
CREATE INDEX idx_exams_exam_date  ON exams (exam_date);

-- =============================================================================
-- 12. RESULTS
--    One result row per (exam, student) — enforced by unique constraint.
-- =============================================================================
CREATE TABLE results (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    exam_id         UUID          NOT NULL,
    student_id      UUID          NOT NULL,
    marks_obtained  NUMERIC(10,2) NOT NULL,
    grade           VARCHAR(10)   NOT NULL,
    remarks         VARCHAR(250),
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ,

    CONSTRAINT uq_results_exam_student  UNIQUE (exam_id, student_id),
    CONSTRAINT ck_results_marks         CHECK (marks_obtained >= 0),

    CONSTRAINT fk_results_exam    FOREIGN KEY (exam_id)
        REFERENCES exams (id) ON DELETE CASCADE,
    CONSTRAINT fk_results_student FOREIGN KEY (student_id)
        REFERENCES students (id) ON DELETE CASCADE
);

CREATE INDEX idx_results_exam_id    ON results (exam_id);
CREATE INDEX idx_results_student_id ON results (student_id);

-- =============================================================================
-- 13. FEES
-- =============================================================================
CREATE TABLE fees (
    id          UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id  UUID          NOT NULL,
    fee_type    VARCHAR(100)  NOT NULL,
    amount      NUMERIC(10,2) NOT NULL,
    due_date    DATE          NOT NULL,
    status      VARCHAR(20)   NOT NULL DEFAULT 'Pending',
    created_at  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT ck_fees_amount CHECK (amount >= 0),
    CONSTRAINT ck_fees_status  CHECK (status IN ('Pending', 'PartiallyPaid', 'Paid', 'Overdue')),

    CONSTRAINT fk_fees_student FOREIGN KEY (student_id)
        REFERENCES students (id) ON DELETE CASCADE
);

CREATE INDEX idx_fees_student_id ON fees (student_id);
CREATE INDEX idx_fees_status     ON fees (status);
CREATE INDEX idx_fees_due_date   ON fees (due_date);

-- =============================================================================
-- 14. PAYMENTS  (records each partial or full payment against a fee)
-- =============================================================================
CREATE TABLE payments (
    id                    UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    fee_id                UUID          NOT NULL,
    amount_paid           NUMERIC(10,2) NOT NULL,
    payment_date          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    payment_method        VARCHAR(30)   NOT NULL,
    transaction_reference VARCHAR(100),
    created_at            TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at            TIMESTAMPTZ,

    CONSTRAINT ck_payments_amount_paid    CHECK (amount_paid > 0),
    CONSTRAINT ck_payments_method         CHECK (payment_method IN ('Cash', 'Card', 'BankTransfer', 'Online')),

    CONSTRAINT fk_payments_fee FOREIGN KEY (fee_id)
        REFERENCES fees (id) ON DELETE CASCADE
);

CREATE INDEX idx_payments_fee_id       ON payments (fee_id);
CREATE INDEX idx_payments_payment_date ON payments (payment_date);

-- =============================================================================
-- 15. ENROLLMENTS  (historical record of student ↔ class per academic year)
-- =============================================================================
CREATE TABLE enrollments (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id       UUID        NOT NULL,
    class_id         UUID        NOT NULL,
    academic_year    VARCHAR(20) NOT NULL,
    enrollment_date  DATE        NOT NULL,
    status           VARCHAR(30) NOT NULL DEFAULT 'Active',
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ,

    CONSTRAINT ck_enrollments_status CHECK (status IN ('Active', 'Withdrawn', 'Completed', 'Suspended')),

    CONSTRAINT fk_enrollments_student FOREIGN KEY (student_id)
        REFERENCES students (id) ON DELETE CASCADE,
    CONSTRAINT fk_enrollments_class   FOREIGN KEY (class_id)
        REFERENCES academic_classes (id) ON DELETE CASCADE
);

CREATE INDEX idx_enrollments_student_id   ON enrollments (student_id);
CREATE INDEX idx_enrollments_class_id     ON enrollments (class_id);
CREATE INDEX idx_enrollments_acad_year    ON enrollments (academic_year);
-- Ensure one active enrollment per student per academic year
CREATE UNIQUE INDEX uq_enrollments_student_year
    ON enrollments (student_id, academic_year)
    WHERE status = 'Active';

-- =============================================================================
-- 16. NOTIFICATIONS
-- =============================================================================
CREATE TABLE notifications (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID         NOT NULL,
    title       VARCHAR(100) NOT NULL,
    message     VARCHAR(500) NOT NULL,
    is_read     BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT fk_notifications_user FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX idx_notifications_user_id ON notifications (user_id);
CREATE INDEX idx_notifications_is_read ON notifications (user_id, is_read)
    WHERE is_read = FALSE;   -- partial index — fast unread count per user

-- =============================================================================
-- Auto-update updated_at via trigger (replaces EF Core SaveChanges override)
-- =============================================================================
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;

DO $$
DECLARE
    t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'roles', 'users', 'parents', 'teachers', 'academic_classes',
        'subjects', 'students', 'teacher_subject_assignments',
        'timetable_entries', 'attendance_records', 'exams', 'results',
        'fees', 'payments', 'enrollments', 'notifications'
    ]
    LOOP
        EXECUTE format(
            'CREATE TRIGGER trg_%s_updated_at
             BEFORE UPDATE ON %I
             FOR EACH ROW EXECUTE FUNCTION set_updated_at()',
            t, t
        );
    END LOOP;
END;
$$;

-- =============================================================================
-- Useful views
-- =============================================================================

-- Student roster with denormalised user + class + parent info
CREATE OR REPLACE VIEW v_student_roster AS
SELECT
    s.id                AS student_id,
    u.full_name,
    u.email,
    u.phone,
    s.student_code,
    s.gender,
    s.date_of_birth,
    s.admission_date,
    c.name || ' ' || c.section  AS class_name,
    c.academic_year,
    pu.full_name                AS parent_name,
    pu.phone                    AS parent_phone
FROM students         s
JOIN users            u  ON u.id = s.user_id
LEFT JOIN academic_classes c  ON c.id = s.class_id
LEFT JOIN parents     p  ON p.id = s.parent_id
LEFT JOIN users       pu ON pu.id = p.user_id;

-- Attendance summary per student
CREATE OR REPLACE VIEW v_attendance_summary AS
SELECT
    s.id                                            AS student_id,
    u.full_name,
    COUNT(*)                                        AS total_records,
    COUNT(*) FILTER (WHERE ar.status = 'Present')  AS present,
    COUNT(*) FILTER (WHERE ar.status = 'Late')     AS late,
    COUNT(*) FILTER (WHERE ar.status = 'Absent')   AS absent,
    COUNT(*) FILTER (WHERE ar.status = 'Excused')  AS excused,
    ROUND(
        100.0 *
        COUNT(*) FILTER (WHERE ar.status IN ('Present', 'Late')) /
        NULLIF(COUNT(*), 0),
        1
    )                                               AS attendance_pct
FROM attendance_records ar
JOIN students           s  ON s.id = ar.student_id
JOIN users              u  ON u.id = s.user_id
GROUP BY s.id, u.full_name;

-- Fee summary per student
CREATE OR REPLACE VIEW v_fee_summary AS
SELECT
    s.id                                                        AS student_id,
    u.full_name,
    COUNT(f.id)                                                 AS total_fees,
    COALESCE(SUM(f.amount), 0)                                  AS total_amount,
    COALESCE(SUM(f.amount) FILTER (WHERE f.status = 'Paid'), 0) AS paid_amount,
    COALESCE(SUM(f.amount) FILTER (WHERE f.status != 'Paid'), 0) AS outstanding_amount,
    COUNT(f.id) FILTER (WHERE f.status = 'Overdue')             AS overdue_count
FROM students s
JOIN users    u ON u.id = s.user_id
LEFT JOIN fees f ON f.student_id = s.id
GROUP BY s.id, u.full_name;
