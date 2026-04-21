using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public sealed class AttendanceRecord : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Remarks { get; set; }

    public Student? Student { get; set; }
    public AcademicClass? Class { get; set; }
    public Subject? Subject { get; set; }
    public Teacher? Teacher { get; set; }
}
