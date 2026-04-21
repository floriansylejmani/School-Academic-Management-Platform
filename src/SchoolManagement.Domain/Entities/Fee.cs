using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public sealed class Fee : BaseEntity
{
    public Guid StudentId { get; set; }
    public string FeeType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public FeeStatus Status { get; set; } = FeeStatus.Pending;

    public Student? Student { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
