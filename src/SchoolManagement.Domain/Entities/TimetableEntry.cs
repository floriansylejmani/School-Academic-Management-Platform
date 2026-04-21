using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class TimetableEntry : BaseEntity
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? RoomNumber { get; set; }

    public AcademicClass? Class { get; set; }
    public Subject? Subject { get; set; }
    public Teacher? Teacher { get; set; }
}
