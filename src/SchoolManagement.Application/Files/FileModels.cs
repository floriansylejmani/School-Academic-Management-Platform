using Microsoft.AspNetCore.Http;

namespace SchoolManagement.Application.Files;

// ─────────────────────────────────────────────────────────────────────────────
// Constants
// ─────────────────────────────────────────────────────────────────────────────

public static class FileEntityTypes
{
    public const string ProfilePicture  = "ProfilePicture";
    public const string StudentDocument = "StudentDocument";
}

public static class FileConstraints
{
    public const long MaxProfilePictureSizeBytes = 5 * 1024 * 1024;   // 5 MB
    public const long MaxDocumentSizeBytes       = 20 * 1024 * 1024;  // 20 MB

    public static readonly string[] AllowedProfilePictureExtensions =
        [".jpg", ".jpeg", ".png", ".webp"];

    public static readonly string[] AllowedDocumentExtensions =
        [".pdf", ".doc", ".docx"];

    public static readonly string[] AllowedProfilePictureMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    public static readonly string[] AllowedDocumentMimeTypes =
        ["application/pdf", "application/msword",
         "application/vnd.openxmlformats-officedocument.wordprocessingml.document"];
}

// ─────────────────────────────────────────────────────────────────────────────
// Request / response records
// ─────────────────────────────────────────────────────────────────────────────

public record UploadProfilePictureRequest(IFormFile File, Guid UserId, Guid UploadedByUserId);

public record UploadStudentDocumentRequest(IFormFile File, Guid StudentId, Guid UploadedByUserId);

public record UploadedFileResponse(
    Guid   Id,
    string OriginalFileName,
    string ContentType,
    long   FileSizeBytes,
    string EntityType,
    Guid   EntityId,
    string DownloadUrl,
    DateTime UploadedAt);

public record FileDownloadResult(
    Stream Stream,
    string ContentType,
    string FileName);

// ─────────────────────────────────────────────────────────────────────────────
// Service interface
// ─────────────────────────────────────────────────────────────────────────────

public interface IFileService
{
    /// <summary>
    /// Validates and stores a profile picture for <paramref name="request"/>.UserId.
    /// Updates User.ProfilePictureUrl and removes any previous picture file for that user.
    /// Returns the public download URL.
    /// </summary>
    Task<UploadedFileResponse> UploadProfilePictureAsync(UploadProfilePictureRequest request, CancellationToken ct = default);

    /// <summary>
    /// Validates and stores a document for the given student.
    /// </summary>
    Task<UploadedFileResponse> UploadStudentDocumentAsync(UploadStudentDocumentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns all uploaded documents for a student.
    /// </summary>
    Task<IReadOnlyList<UploadedFileResponse>> GetStudentDocumentsAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a student document. Throws if the document is not found or does not belong to the student.
    /// </summary>
    Task DeleteStudentDocumentAsync(Guid documentId, Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Streams a file for authorised download.
    /// </summary>
    Task<FileDownloadResult> DownloadFileAsync(Guid fileId, CancellationToken ct = default);
}
