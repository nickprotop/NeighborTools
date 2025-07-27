# Enhanced `/api/files/upload` Endpoint Usage Guide

## Overview

The `/api/files/upload` endpoint has been enhanced to accept metadata parameters from the frontend, allowing for flexible file access control while maintaining security by default.

## Endpoint Details

**URL**: `POST /api/files/upload`  
**Authentication**: Required (`[Authorize]`)  
**Content-Type**: `multipart/form-data`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `file` | `IFormFile` | ✅ Yes | - | The file to upload |
| `folder` | `string` | ❌ No | `""` | Folder to organize files |
| `accessLevel` | `string` | ❌ No | `"private"` | Access level: `"public"`, `"private"`, `"restricted"` |
| `fileType` | `string` | ❌ No | `"general"` | File type category for organization |
| `allowedUsers` | `string` | ❌ No | `null` | Comma-separated user IDs for restricted access |
| `relatedId` | `string` | ❌ No | `null` | Related entity ID (rental, dispute, etc.) |

## Security Design

🔐 **Security First**: Defaults to `"private"` access level if not specified  
👤 **Owner Tracking**: Automatically sets current user as file owner  
✅ **Validation**: Validates access levels and parameters  
🏷️ **Context Mapping**: Maps related IDs to appropriate metadata fields  

## Usage Examples

### Example 1: Public File Upload (Frontend Service)

```typescript
// Frontend TypeScript/JavaScript example
async uploadPublicDocument(file: File, documentType: string): Promise<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folder', 'documents');
    formData.append('accessLevel', 'public');
    formData.append('fileType', documentType);

    const response = await fetch('/api/files/upload', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
        },
        body: formData
    });

    return await response.json();
}
```

### Example 2: Private File Upload (C# Blazor Service)

```csharp
// C# Blazor service example
public async Task<ApiResponse<FileUploadResult>> UploadPrivateFileAsync(
    IBrowserFile file, 
    string folder = "", 
    string fileType = "general")
{
    using var content = new MultipartFormDataContent();
    using var fileContent = new StreamContent(file.OpenReadStream());
    
    content.Add(fileContent, "file", file.Name);
    content.Add(new StringContent(folder), "folder");
    content.Add(new StringContent("private"), "accessLevel"); // Explicit private
    content.Add(new StringContent(fileType), "fileType");

    var response = await _httpClient.PostAsync("/api/files/upload", content);
    return await response.Content.ReadFromJsonAsync<ApiResponse<FileUploadResult>>();
}
```

### Example 3: Restricted Access File

```csharp
// Upload file with restricted access to specific users
public async Task<ApiResponse<FileUploadResult>> UploadRestrictedFileAsync(
    IBrowserFile file, 
    string[] allowedUserIds, 
    string relatedRentalId = null)
{
    using var content = new MultipartFormDataContent();
    using var fileContent = new StreamContent(file.OpenReadStream());
    
    content.Add(fileContent, "file", file.Name);
    content.Add(new StringContent("rentals"), "folder");
    content.Add(new StringContent("restricted"), "accessLevel");
    content.Add(new StringContent("rental-document"), "fileType");
    content.Add(new StringContent(string.Join(",", allowedUserIds)), "allowedUsers");
    
    if (!string.IsNullOrEmpty(relatedRentalId))
        content.Add(new StringContent(relatedRentalId), "relatedId");

    var response = await _httpClient.PostAsync("/api/files/upload", content);
    return await response.Content.ReadFromJsonAsync<ApiResponse<FileUploadResult>>();
}
```

### Example 4: Dispute Evidence Upload

```csharp
// Upload dispute evidence with automatic context mapping
public async Task<ApiResponse<FileUploadResult>> UploadDisputeEvidenceAsync(
    IBrowserFile file, 
    string disputeId, 
    string[] allowedParties)
{
    using var content = new MultipartFormDataContent();
    using var fileContent = new StreamContent(file.OpenReadStream());
    
    content.Add(fileContent, "file", file.Name);
    content.Add(new StringContent($"disputes/{disputeId}"), "folder");
    content.Add(new StringContent("private"), "accessLevel");
    content.Add(new StringContent("dispute-evidence"), "fileType");
    content.Add(new StringContent(string.Join(",", allowedParties)), "allowedUsers");
    content.Add(new StringContent(disputeId), "relatedId"); // Auto-mapped to DisputeId

    var response = await _httpClient.PostAsync("/api/files/upload", content);
    return await response.Content.ReadFromJsonAsync<ApiResponse<FileUploadResult>>();
}
```

## Response Format

```json
{
  "message": "File uploaded successfully",
  "storagePath": "documents/12345678-1234-1234-1234-123456789abc.pdf",
  "fileUrl": "documents/12345678-1234-1234-1234-123456789abc.pdf",
  "fileName": "contract.pdf",
  "contentType": "application/pdf",
  "size": 245760,
  "metadata": {
    "accessLevel": "private",
    "fileType": "rental-document",
    "ownerId": "user-123"
  }
}
```

## Access Level Behaviors

### `"public"` 
- ✅ Accessible to everyone (authenticated and unauthenticated)
- 📁 Typically for: tool images, public documents, marketing materials

### `"private"` (Default)
- 🔒 Only accessible to file owner and admins
- 📁 Typically for: personal documents, private files

### `"restricted"`
- 🎯 Accessible to owner, specified users in `allowedUsers`, and admins
- 📁 Typically for: shared project files, multi-party documents

## Context Mapping

The endpoint automatically maps `relatedId` to appropriate metadata fields:

| File Type / Folder | Related ID Maps To |
|-------------------|-------------------|
| `dispute-evidence` or `disputes/*` | `DisputeId` |
| `rental-document` or `rentals/*` | `RentalId` |
| Other contexts | Can be extended as needed |

## Error Responses

```json
// Invalid access level
{
  "message": "Invalid access level. Must be 'public', 'private', or 'restricted'"
}

// No file provided
{
  "message": "No file provided"
}

// Authentication required
{
  "message": "User not found"
}
```

## Migration from Other Endpoints

This enhanced endpoint can potentially replace some specific upload endpoints:

- ✅ Fully compatible with existing metadata system
- ✅ More flexible than specific endpoints
- ✅ Maintains security by default
- ✅ Backward compatible (existing endpoints still work)

## Security Considerations

🛡️ **Always defaults to private** - No accidental public file exposure  
🔍 **Parameter validation** - Prevents invalid configurations  
👥 **User-based access** - Files are always owned by the uploader  
📋 **Audit logging** - All uploads are logged with metadata  