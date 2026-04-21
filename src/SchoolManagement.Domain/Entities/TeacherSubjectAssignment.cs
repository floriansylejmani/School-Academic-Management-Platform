using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class TeacherSubjectAssignment : BaseEntity
{
    public Guid TeacherId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid ClassId { get; set; }

    public Teacher? Teacher { get; set; }
    public Subject? Subject { get; set; }
    public AcademicClass? Class { get; set; }
}
