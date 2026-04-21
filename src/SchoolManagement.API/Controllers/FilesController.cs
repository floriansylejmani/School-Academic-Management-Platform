using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Files;

namespace SchoolManagement.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public sealed class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger      = logger;
    }

    // ── Upload own profile picture ────────────────────────────────────────────

    /// <summary>
    /// Any authenticated user can upload a profile picture for themselves.
    /// Admins can upload for any user by specifying a userId query parameter.
    /// </summary>
    [HttpPost("profile-picture")]
    [RequestSizeLimit(6 * 1024 * 1024)]  // slightly above the 5 MB file limit to account for multipart overhead
    public async Task<ActionResult<ApiResponse<UploadedFileResponse>>> UploadProfilePicture(
        IFormFile file,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken)
    {
        var callerId = GetCallerId();

        // Admins may upload on behalf of any user; others can only upload for themselves
        var targetUserId = userId.HasValue && IsAdmin()
            ? userId.Value
            : callerId;

        var request = new UploadProfilePictureRequest(file, targetUserId, callerId);
        var result  = await _fileService.UploadProfilePictureAsync(request, cancellationToken);
        return Ok(ApiResponse<UploadedFileResponse>.Ok(result, "Profile picture uploaded successfully."));
    }

    // ── Student documents ─────────────────────────────────────────────────────

    [HttpPost("students/{studentId:guid}/documents")]
    [Authorize(Roles = "Admin,Teacher")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UploadedFileResponse>>> UploadStudentDocument(
        Guid studentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var callerId = GetCallerId();
        var request  = new UploadStudentDocumentRequest(file, studentId, callerId);
        var result   = await _fileService.UploadStudentDocumentAsync(request, cancellationToken);
        return Ok(ApiResponse<UploadedFileResponse>.Ok(result, "Document uploaded successfully."));
    }

    [HttpGet("students/{studentId:guid}/documents")]
    [Authorize(Roles = "Admin,Teacher,Parent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UploadedFileResponse>>>> GetStudentDocuments(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var docs = await _fileService.GetStudentDocumentsAsync(studentId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UploadedFileResponse>>.Ok(docs));
    }

    [HttpDelete("students/{studentId:guid}/documents/{documentId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteStudentDocument(
        Guid studentId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _fileService.DeleteStudentDocumentAsync(documentId, studentId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Document deleted successfully."));
    }

    // ── Authorised file serving ───────────────────────────────────────────────

    [HttpGet("{fileId:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid fileId, CancellationToken cancellationToken)
    {
        var result = await _fileService.DownloadFileAsync(fileId, cancellationToken);
        return File(result.Stream, result.ContentType, result.FileName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetCallerId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found in token.");
        return Guid.Parse(value);
    }

    private bool IsAdmin() =>
        User.IsInRole("Admin");
}
