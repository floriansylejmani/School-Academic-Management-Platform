using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class Exam : BaseEntity
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly ExamDate { get; set; }
    public decimal TotalMarks { get; set; }

    public AcademicClass? Class { get; set; }
    public Subject? Subject { get; set; }
    public ICollection<Result> Results { get; set; } = new List<Result>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
