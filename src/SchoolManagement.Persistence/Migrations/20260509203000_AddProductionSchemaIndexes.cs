using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260509203000_AddProductionSchemaIndexes")]
    public partial class AddProductionSchemaIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_ClassId_Date",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "ClassId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Fees_Status_DueDate",
                table: "Fees",
                columns: new[] { "Status", "DueDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId_ClassId_Date",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Fees_Status_DueDate",
                table: "Fees");
        }
    }
}
