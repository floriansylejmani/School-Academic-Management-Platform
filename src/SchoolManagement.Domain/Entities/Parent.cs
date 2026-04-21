using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class Parent : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Occupation { get; set; }

    public User? User { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
