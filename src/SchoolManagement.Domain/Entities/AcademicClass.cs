using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class AcademicClass : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public Guid? ClassTeacherId { get; set; }

    public Teacher? ClassTeacher { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<TeacherSubjectAssignment> SubjectAssignments { get; set; } = new List<TeacherSubjectAssignment>();
    public ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
