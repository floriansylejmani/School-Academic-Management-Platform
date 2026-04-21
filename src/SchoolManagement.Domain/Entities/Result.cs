using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class Result : BaseEntity
{
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public decimal MarksObtained { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    public Exam? Exam { get; set; }
    public Student? Student { get; set; }
}
