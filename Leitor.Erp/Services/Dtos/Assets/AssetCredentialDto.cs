using System;
using Leitor.Erp.Entities.Assets;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Assets;

// Deliberately no EncryptedValue/decrypted value here - this DTO is what List/Get return, and
// those aren't permission-gated behind Assets.RevealCredentials the way RevealAsync is. See
// Entities/Assets/AssetCredential.cs's own comment.
public class AssetCredentialDto : FullAuditedEntityDto<Guid>
{
    public Guid ConfigurationItemId { get; set; }
    public string Label { get; set; } = string.Empty;
    public AssetCredentialType CredentialType { get; set; }
    public string? Username { get; set; }
    public string? Notes { get; set; }
}
