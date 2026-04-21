using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.Persistence.Migrations
{
    public partial class AddSubmissionsAndAiReviews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EssayPrompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AnswerText = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    MaximumScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TeacherFinalScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    TeacherFinalGrade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TeacherReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsAiFeedbackReleasedToStudent = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Submissions_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionAIReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderResponseId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    GrammarScore = table.Column<int>(type: "integer", nullable: false),
                    ClarityScore = table.Column<int>(type: "integer", nullable: false),
                    StructureScore = table.Column<int>(type: "integer", nullable: false),
                    ContentScore = table.Column<int>(type: "integer", nullable: false),
                    OverallSuggestedScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SummaryFeedback = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    StrengthsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    WeaknessesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ImprovementsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RubricBreakdownJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    SafetyNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionAIReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionAIReviews_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionAIReviews_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAIReviews_RequestedByUserId",
                table: "SubmissionAIReviews",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAIReviews_SubmissionId",
                table: "SubmissionAIReviews",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ExamId_StudentId",
                table: "Submissions",
                columns: new[] { "ExamId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ReviewedByUserId",
                table: "Submissions",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_SubmittedByUserId",
                table: "Submissions",
                column: "SubmittedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionAIReviews");

            migrationBuilder.DropTable(
                name: "Submissions");
        }
    }
}
