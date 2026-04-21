using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

public sealed class UploadedFile : BaseEntity
{
    /// <summary>Generated UUID-based file name stored on disk (no extension guessable attack surface).</summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>The original file name as provided by the uploader.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Validated MIME type (e.g. image/jpeg, application/pdf).</summary>
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    /// <summary>Logical category: "ProfilePicture" or "StudentDocument".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Id of the related entity (UserId for profile pictures, StudentId for documents).</summary>
    public Guid EntityId { get; set; }

    public Guid UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }
}
