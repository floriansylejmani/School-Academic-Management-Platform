using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public sealed class Student : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? ClassId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public DateOnly AdmissionDate { get; set; }

    public User? User { get; set; }
    public Parent? Parent { get; set; }
    public AcademicClass? Class { get; set; }
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
    public ICollection<Fee> Fees { get; set; } = new List<Fee>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<UploadedFile> Documents { get; set; } = new List<UploadedFile>();
}
