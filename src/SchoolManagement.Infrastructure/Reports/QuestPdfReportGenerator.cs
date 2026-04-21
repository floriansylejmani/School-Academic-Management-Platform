using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolManagement.Application.Reports;

namespace SchoolManagement.Infrastructure.Reports;

public sealed class QuestPdfReportGenerator : IReportPdfGenerator
{
    public QuestPdfReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateStudentsReport(StudentsPdfReportData report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, report.Metadata);

                page.Content().Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Text("Student performance and financial summary").FontSize(12).FontColor(Colors.Grey.Darken2);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.8f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                        });

                        table.Header(header =>
                        {
                            AddHeaderCell(header.Cell(), "Student");
                            AddHeaderCell(header.Cell(), "Class");
                            AddHeaderCell(header.Cell(), "Email");
                            AddHeaderCell(header.Cell(), "Parent");
                            AddHeaderCell(header.Cell(), "Attendance");
                            AddHeaderCell(header.Cell(), "Avg score");
                            AddHeaderCell(header.Cell(), "Outstanding");
                        });

                        foreach (var student in report.Students)
                        {
                            AddBodyCell(table, $"{student.StudentName}\n{student.StudentCode}");
                            AddBodyCell(table, student.ClassName);
                            AddBodyCell(table, student.Email);
                            AddBodyCell(table, student.ParentName ?? "Not linked");
                            AddBodyCell(table, $"{student.AttendancePercentage:0.0}%");
                            AddBodyCell(table, $"{student.AverageScorePercentage:0.0}%");
                            AddBodyCell(table, Money(student.OutstandingBalance));
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateAttendanceReport(AttendancePdfReportData report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, report.Metadata);

                page.Content().Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(card => SummaryCard(card, "Present", report.PresentCount.ToString()));
                        row.RelativeItem().Element(card => SummaryCard(card, "Absent", report.AbsentCount.ToString()));
                        row.RelativeItem().Element(card => SummaryCard(card, "Late", report.LateCount.ToString()));
                        row.RelativeItem().Element(card => SummaryCard(card, "Excused", report.ExcusedCount.ToString()));
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                        });

                        table.Header(header =>
                        {
                            AddHeaderCell(header.Cell(), "Student");
                            AddHeaderCell(header.Cell(), "Class");
                            AddHeaderCell(header.Cell(), "Subject");
                            AddHeaderCell(header.Cell(), "Teacher");
                            AddHeaderCell(header.Cell(), "Date");
                            AddHeaderCell(header.Cell(), "Status");
                        });

                        foreach (var record in report.Records)
                        {
                            AddBodyCell(table, record.StudentName);
                            AddBodyCell(table, record.ClassName);
                            AddBodyCell(table, record.SubjectName);
                            AddBodyCell(table, record.TeacherName);
                            AddBodyCell(table, record.Date.ToString("yyyy-MM-dd"));
                            AddBodyCell(table, record.Status);
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateFeesReport(FeesPdfReportData report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, report.Metadata);

                page.Content().Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(card => SummaryCard(card, "Billed", Money(report.TotalBilled)));
                        row.RelativeItem().Element(card => SummaryCard(card, "Paid", Money(report.TotalPaid)));
                        row.RelativeItem().Element(card => SummaryCard(card, "Outstanding", Money(report.TotalOutstanding)));
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                        });

                        table.Header(header =>
                        {
                            AddHeaderCell(header.Cell(), "Student");
                            AddHeaderCell(header.Cell(), "Class");
                            AddHeaderCell(header.Cell(), "Fee");
                            AddHeaderCell(header.Cell(), "Billed");
                            AddHeaderCell(header.Cell(), "Paid");
                            AddHeaderCell(header.Cell(), "Due");
                            AddHeaderCell(header.Cell(), "Status");
                        });

                        foreach (var fee in report.Fees)
                        {
                            AddBodyCell(table, fee.StudentName);
                            AddBodyCell(table, fee.ClassName);
                            AddBodyCell(table, fee.FeeType);
                            AddBodyCell(table, Money(fee.Amount));
                            AddBodyCell(table, Money(fee.PaidAmount));
                            AddBodyCell(table, fee.DueDate.ToString("yyyy-MM-dd"));
                            AddBodyCell(table, fee.Status);
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void ConfigurePage(PageDescriptor page, ReportMetadata metadata)
    {
        page.Size(PageSizes.A4);
        page.Margin(24);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(text => text.FontSize(10).FontColor(Colors.Grey.Darken4));

        page.Header().Column(column =>
        {
            column.Spacing(6);
            column.Item().Text(metadata.Title).SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);
            column.Item().Text(BuildFilterSummary(metadata)).FontSize(10).FontColor(Colors.Grey.Darken2);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });

        page.Footer()
            .AlignRight()
            .Text($"Generated {metadata.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC")
            .FontSize(9)
            .FontColor(Colors.Grey.Darken1);
    }

    private static string BuildFilterSummary(ReportMetadata metadata)
    {
        var filters = new List<string>
        {
            $"Class: {metadata.ClassName ?? "All"}",
            $"Student: {metadata.StudentName ?? "All"}",
            $"From: {metadata.DateFrom?.ToString("yyyy-MM-dd") ?? "Any"}",
            $"To: {metadata.DateTo?.ToString("yyyy-MM-dd") ?? "Any"}"
        };

        return string.Join("   |   ", filters);
    }

    private static void SummaryCard(IContainer container, string label, string value)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(4);
                column.Item().Text(label).FontSize(10).FontColor(Colors.Grey.Darken1);
                column.Item().Text(value).SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);
            });
    }

    private static void AddHeaderCell(IContainer container, string text)
    {
        container.Element(HeaderCellStyle).Text(text).SemiBold();
    }

    private static void AddBodyCell(TableDescriptor table, string text)
    {
        table.Cell().Element(BodyCellStyle).Text(text);
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(8)
            .PaddingHorizontal(6);
    }

    private static IContainer BodyCellStyle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(7)
            .PaddingHorizontal(6);
    }

    private static string Money(decimal amount) => $"${amount:0.00}";
}
