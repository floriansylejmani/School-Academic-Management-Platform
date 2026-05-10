using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagement.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260427170000_RepairFileAndSubmissionSchema")]
    public partial class RepairFileAndSubmissionSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Users"
                    ADD COLUMN IF NOT EXISTS "ProfilePictureUrl" character varying(500);
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "UploadedFiles" (
                    "Id" uuid NOT NULL,
                    "StoredFileName" character varying(200) NOT NULL,
                    "OriginalFileName" character varying(255) NOT NULL,
                    "ContentType" character varying(100) NOT NULL,
                    "FileSizeBytes" bigint NOT NULL,
                    "EntityType" character varying(50) NOT NULL,
                    "EntityId" uuid NOT NULL,
                    "UploadedByUserId" uuid NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_UploadedFiles" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_UploadedFiles_Users_UploadedByUserId'
                    ) THEN
                        ALTER TABLE "UploadedFiles"
                            ADD CONSTRAINT "FK_UploadedFiles_Users_UploadedByUserId"
                            FOREIGN KEY ("UploadedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Submissions" (
                    "Id" uuid NOT NULL,
                    "ExamId" uuid NOT NULL,
                    "StudentId" uuid NOT NULL,
                    "SubmittedByUserId" uuid NOT NULL,
                    "EssayPrompt" character varying(2000) NULL,
                    "AnswerText" character varying(20000) NOT NULL,
                    "MaximumScore" numeric(10,2) NOT NULL,
                    "TeacherFinalScore" numeric(10,2) NULL,
                    "TeacherFinalGrade" character varying(20) NULL,
                    "TeacherReviewNotes" character varying(2000) NULL,
                    "IsAiFeedbackReleasedToStudent" boolean NOT NULL,
                    "ReviewedByUserId" uuid NULL,
                    "ReviewedAt" timestamp with time zone NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_Submissions" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Submissions_Exams_ExamId'
                    ) THEN
                        ALTER TABLE "Submissions"
                            ADD CONSTRAINT "FK_Submissions_Exams_ExamId"
                            FOREIGN KEY ("ExamId") REFERENCES "Exams" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Submissions_Students_StudentId'
                    ) THEN
                        ALTER TABLE "Submissions"
                            ADD CONSTRAINT "FK_Submissions_Students_StudentId"
                            FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Submissions_Users_ReviewedByUserId'
                    ) THEN
                        ALTER TABLE "Submissions"
                            ADD CONSTRAINT "FK_Submissions_Users_ReviewedByUserId"
                            FOREIGN KEY ("ReviewedByUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Submissions_Users_SubmittedByUserId'
                    ) THEN
                        ALTER TABLE "Submissions"
                            ADD CONSTRAINT "FK_Submissions_Users_SubmittedByUserId"
                            FOREIGN KEY ("SubmittedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "SubmissionAIReviews" (
                    "Id" uuid NOT NULL,
                    "SubmissionId" uuid NOT NULL,
                    "RequestedByUserId" uuid NOT NULL,
                    "Mode" character varying(30) NOT NULL,
                    "Model" character varying(100) NOT NULL,
                    "ProviderResponseId" character varying(150) NULL,
                    "GrammarScore" integer NOT NULL,
                    "ClarityScore" integer NOT NULL,
                    "StructureScore" integer NOT NULL,
                    "ContentScore" integer NOT NULL,
                    "OverallSuggestedScore" numeric(10,2) NOT NULL,
                    "SummaryFeedback" character varying(4000) NOT NULL,
                    "StrengthsJson" character varying(4000) NOT NULL,
                    "WeaknessesJson" character varying(4000) NOT NULL,
                    "ImprovementsJson" character varying(4000) NOT NULL,
                    "RubricBreakdownJson" character varying(8000) NOT NULL,
                    "SafetyNotes" character varying(2000) NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_SubmissionAIReviews" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_SubmissionAIReviews_Submissions_SubmissionId'
                    ) THEN
                        ALTER TABLE "SubmissionAIReviews"
                            ADD CONSTRAINT "FK_SubmissionAIReviews_Submissions_SubmissionId"
                            FOREIGN KEY ("SubmissionId") REFERENCES "Submissions" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_SubmissionAIReviews_Users_RequestedByUserId'
                    ) THEN
                        ALTER TABLE "SubmissionAIReviews"
                            ADD CONSTRAINT "FK_SubmissionAIReviews_Users_RequestedByUserId"
                            FOREIGN KEY ("RequestedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_UploadedFiles_EntityType_EntityId"
                    ON "UploadedFiles" ("EntityType", "EntityId");
                CREATE INDEX IF NOT EXISTS "IX_UploadedFiles_UploadedByUserId"
                    ON "UploadedFiles" ("UploadedByUserId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Submissions_ExamId_StudentId"
                    ON "Submissions" ("ExamId", "StudentId");
                CREATE INDEX IF NOT EXISTS "IX_Submissions_ReviewedByUserId"
                    ON "Submissions" ("ReviewedByUserId");
                CREATE INDEX IF NOT EXISTS "IX_Submissions_StudentId"
                    ON "Submissions" ("StudentId");
                CREATE INDEX IF NOT EXISTS "IX_Submissions_SubmittedByUserId"
                    ON "Submissions" ("SubmittedByUserId");
                CREATE INDEX IF NOT EXISTS "IX_SubmissionAIReviews_RequestedByUserId"
                    ON "SubmissionAIReviews" ("RequestedByUserId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SubmissionAIReviews_SubmissionId"
                    ON "SubmissionAIReviews" ("SubmissionId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SubmissionAIReviews");
            migrationBuilder.DropTable(name: "Submissions");
            migrationBuilder.DropTable(name: "UploadedFiles");
            migrationBuilder.DropColumn(name: "ProfilePictureUrl", table: "Users");
        }
    }
}
