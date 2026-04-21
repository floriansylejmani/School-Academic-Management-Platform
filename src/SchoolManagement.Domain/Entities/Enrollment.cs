using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class Enrollment : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public DateOnly EnrollmentDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public Student? Student { get; set; }
    public AcademicClass? Class { get; set; }
}
