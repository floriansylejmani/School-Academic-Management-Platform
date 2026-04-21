using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class Teacher : BaseEntity
{
    public Guid UserId { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }

    public User? User { get; set; }
    public ICollection<AcademicClass> HomeroomClasses { get; set; } = new List<AcademicClass>();
    public ICollection<TeacherSubjectAssignment> SubjectAssignments { get; set; } = new List<TeacherSubjectAssignment>();
    public ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
