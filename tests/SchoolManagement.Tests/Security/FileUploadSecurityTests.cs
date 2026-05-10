using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Security;

public sealed class FileUploadSecurityTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public FileUploadSecurityTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Profile_picture_upload_without_auth_returns_401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        using var content = CreateMultipartFileContent(
            fileName: "avatar.png",
            contentType: "image/png",
            bytes: CreatePngBytes(128));

        var response = await client.PostAsync("/api/files/profile-picture", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_picture_over_5mb_is_rejected_without_stack_trace()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        using var content = CreateMultipartFileContent(
            fileName: "avatar.png",
            contentType: "image/png",
            bytes: CreatePngBytes((5 * 1024 * 1024) + 1));

        var response = await client.PostAsync("/api/files/profile-picture", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("File exceeds the maximum allowed size", body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FileService.cs", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Student_document_over_20mb_is_rejected_without_stack_trace()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var studentId = Guid.NewGuid();

        using var content = CreateMultipartFileContent(
            fileName: "document.pdf",
            contentType: "application/pdf",
            bytes: CreatePdfBytes((20 * 1024 * 1024) + 1));

        var response = await client.PostAsync($"/api/files/students/{studentId}/documents", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("File exceeds the maximum allowed size", body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FileService.cs", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Profile_picture_with_invalid_magic_bytes_is_rejected()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var fakePng = "this is not a real png"u8.ToArray();

        using var content = CreateMultipartFileContent(
            fileName: "avatar.png",
            contentType: "image/png",
            bytes: fakePng);

        var response = await client.PostAsync("/api/files/profile-picture", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("File content does not match its declared type", body);
    }

    private static MultipartFormDataContent CreateMultipartFileContent(
        string fileName,
        string contentType,
        byte[] bytes)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        content.Add(fileContent, "file", fileName);

        return content;
    }

    private static byte[] CreatePngBytes(int size)
    {
        var bytes = new byte[size];

        // PNG magic bytes: 89 50 4E 47
        bytes[0] = 0x89;
        bytes[1] = 0x50;
        bytes[2] = 0x4E;
        bytes[3] = 0x47;

        return bytes;
    }

    private static byte[] CreatePdfBytes(int size)
    {
        var bytes = new byte[size];

        // PDF magic bytes: %PDF
        bytes[0] = 0x25;
        bytes[1] = 0x50;
        bytes[2] = 0x44;
        bytes[3] = 0x46;

        return bytes;
    }
}
