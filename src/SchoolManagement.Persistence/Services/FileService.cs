using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Files;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Persistence.Services;

public sealed class FileService : IFileService
{
    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        // JPEG: FF D8 FF
        { "image/jpeg",  [0xFF, 0xD8, 0xFF] },
        // PNG:  89 50 4E 47
        { "image/png",   [0x89, 0x50, 0x4E, 0x47] },
        // WebP: RIFF????WEBP  — check bytes 0-3 and 8-11
        { "image/webp",  [0x52, 0x49, 0x46, 0x46] },
        // PDF:  %PDF
        { "application/pdf", [0x25, 0x50, 0x44, 0x46] },
        // DOC (OLE2): D0 CF 11 E0
        { "application/msword", [0xD0, 0xCF, 0x11, 0xE0] },
        // DOCX (ZIP PK): 50 4B 03 04
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", [0x50, 0x4B, 0x03, 0x04] }
    };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;

    public FileService(AppDbContext db, IWebHostEnvironment env, ILogger<FileService> logger)
    {
        _db     = db;
        _env    = env;
        _logger = logger;
    }

    // ── Upload profile picture ────────────────────────────────────────────────

    public async Task<UploadedFileResponse> UploadProfilePictureAsync(
        UploadProfilePictureRequest request, CancellationToken ct = default)
    {
        ValidateFile(
            request.File,
            FileConstraints.AllowedProfilePictureExtensions,
            FileConstraints.AllowedProfilePictureMimeTypes,
            FileConstraints.MaxProfilePictureSizeBytes);

        var storedName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName).ToLowerInvariant()}";
        var subDir     = Path.Combine(GetUploadsRoot(), "profile-pictures");
        Directory.CreateDirectory(subDir);
        var physicalPath = Path.Combine(subDir, storedName);

        await using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await request.File.CopyToAsync(fs, ct);
        }

        // Remove the previous profile picture record (and file) for this user
        var existing = await _db.UploadedFiles
            .Where(f => f.EntityType == FileEntityTypes.ProfilePicture && f.EntityId == request.UserId)
            .ToListAsync(ct);

        foreach (var old in existing)
        {
            DeletePhysicalFile("profile-pictures", old.StoredFileName);
            _db.UploadedFiles.Remove(old);
        }

        // Update the user profile picture URL
        var user = await _db.Users.FindAsync([request.UserId], ct);
        if (user is null) throw new InvalidOperationException("User not found.");
        var downloadUrl = BuildDownloadUrl(FileEntityTypes.ProfilePicture, storedName);
        user.ProfilePictureUrl = downloadUrl;

        var entity = new UploadedFile
        {
            StoredFileName    = storedName,
            OriginalFileName  = Path.GetFileName(request.File.FileName),
            ContentType       = request.File.ContentType,
            FileSizeBytes     = request.File.Length,
            EntityType        = FileEntityTypes.ProfilePicture,
            EntityId          = request.UserId,
            UploadedByUserId  = request.UploadedByUserId
        };
        _db.UploadedFiles.Add(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Profile picture uploaded for user {UserId} by {UploadedBy}",
            request.UserId, request.UploadedByUserId);

        return ToResponse(entity);
    }

    // ── Upload student document ───────────────────────────────────────────────

    public async Task<UploadedFileResponse> UploadStudentDocumentAsync(
        UploadStudentDocumentRequest request, CancellationToken ct = default)
    {
        ValidateFile(
            request.File,
            FileConstraints.AllowedDocumentExtensions,
            FileConstraints.AllowedDocumentMimeTypes,
            FileConstraints.MaxDocumentSizeBytes);

        var studentExists = await _db.Students.AnyAsync(s => s.Id == request.StudentId, ct);
        if (!studentExists) throw new InvalidOperationException("Student not found.");

        var storedName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName).ToLowerInvariant()}";
        var subDir     = Path.Combine(GetUploadsRoot(), "student-documents");
        Directory.CreateDirectory(subDir);
        var physicalPath = Path.Combine(subDir, storedName);

        await using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await request.File.CopyToAsync(fs, ct);
        }

        var entity = new UploadedFile
        {
            StoredFileName    = storedName,
            OriginalFileName  = Path.GetFileName(request.File.FileName),
            ContentType       = request.File.ContentType,
            FileSizeBytes     = request.File.Length,
            EntityType        = FileEntityTypes.StudentDocument,
            EntityId          = request.StudentId,
            UploadedByUserId  = request.UploadedByUserId
        };
        _db.UploadedFiles.Add(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Document uploaded for student {StudentId} by {UploadedBy}",
            request.StudentId, request.UploadedByUserId);

        return ToResponse(entity);
    }

    // ── List student documents ────────────────────────────────────────────────

    public async Task<IReadOnlyList<UploadedFileResponse>> GetStudentDocumentsAsync(
        Guid studentId, CancellationToken ct = default)
    {
        var files = await _db.UploadedFiles
            .AsNoTracking()
            .Where(f => f.EntityType == FileEntityTypes.StudentDocument && f.EntityId == studentId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        return files.Select(ToResponse).ToList();
    }

    // ── Delete student document ───────────────────────────────────────────────

    public async Task DeleteStudentDocumentAsync(Guid documentId, Guid studentId, CancellationToken ct = default)
    {
        var file = await _db.UploadedFiles
            .FirstOrDefaultAsync(f => f.Id == documentId
                && f.EntityType == FileEntityTypes.StudentDocument
                && f.EntityId == studentId, ct);

        if (file is null)
            throw new KeyNotFoundException("Document not found or does not belong to this student.");

        DeletePhysicalFile("student-documents", file.StoredFileName);
        _db.UploadedFiles.Remove(file);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted document {DocumentId} for student {StudentId}", documentId, studentId);
    }

    // ── Authorised download ───────────────────────────────────────────────────

    public async Task<FileDownloadResult> DownloadFileAsync(Guid fileId, CancellationToken ct = default)
    {
        var file = await _db.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId, ct)
            ?? throw new KeyNotFoundException("File not found.");

        var subDir = file.EntityType == FileEntityTypes.ProfilePicture
            ? "profile-pictures"
            : "student-documents";

        var physicalPath = Path.Combine(GetUploadsRoot(), subDir, file.StoredFileName);
        if (!File.Exists(physicalPath))
            throw new FileNotFoundException("The requested file no longer exists on disk.");

        var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return new FileDownloadResult(stream, file.ContentType, file.OriginalFileName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetUploadsRoot()
        => Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");

    private static string BuildDownloadUrl(string entityType, string storedName)
    {
        // Relative URL resolved to /api/files/download/{storedName}; full URL is assembled on the client
        var segment = entityType == FileEntityTypes.ProfilePicture ? "profile-pictures" : "student-documents";
        return $"/api/files/serve/{segment}/{storedName}";
    }

    private void DeletePhysicalFile(string subDir, string storedName)
    {
        var path = Path.Combine(GetUploadsRoot(), subDir, storedName);
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete physical file {Path}", path);
        }
    }

    private static void ValidateFile(
        Microsoft.AspNetCore.Http.IFormFile file,
        string[] allowedExtensions,
        string[] allowedMimeTypes,
        long maxBytes)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("No file was provided.");

        if (file.Length > maxBytes)
            throw new ArgumentException($"File exceeds the maximum allowed size of {maxBytes / 1024 / 1024} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            throw new ArgumentException($"File extension '{ext}' is not allowed.");

        var declaredMime = file.ContentType.ToLowerInvariant();
        if (!allowedMimeTypes.Contains(declaredMime))
            throw new ArgumentException($"Content type '{declaredMime}' is not allowed.");

        // Magic-byte validation
        if (!MagicBytes.TryGetValue(declaredMime, out var magic))
            throw new ArgumentException("Cannot verify file signature for this content type.");

        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);

        if (read < magic.Length)
            throw new ArgumentException("File is too small to be valid.");

        if (!header[..magic.Length].SequenceEqual(magic))
            throw new ArgumentException("File content does not match its declared type.");
    }

    private static UploadedFileResponse ToResponse(UploadedFile f) => new(
        f.Id,
        f.OriginalFileName,
        f.ContentType,
        f.FileSizeBytes,
        f.EntityType,
        f.EntityId,
        BuildDownloadUrl(f.EntityType, f.StoredFileName),
        f.CreatedAt);
}
