using FluentValidation;

namespace SchoolManagement.Application.Reports;

public sealed class ReportPdfFilterRequestValidator : AbstractValidator<ReportPdfFilterRequest>
{
    public ReportPdfFilterRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom cannot be later than DateTo.");
    }
}
