using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Assets;

namespace Leitor.Erp.Services.Dtos.Assets;

public class CreateUpdateAssetCredentialDto
{
    [Required]
    public Guid ConfigurationItemId { get; set; }

    [Required]
    [StringLength(128)]
    public string Label { get; set; } = string.Empty;

    public AssetCredentialType CredentialType { get; set; } = AssetCredentialType.Password;

    [StringLength(128)]
    public string? Username { get; set; }

    // Plain text on the wire (HTTPS-protected, same as any other form field) - encrypted via
    // IStringEncryptionService before it ever reaches the database. See
    // AssetCredentialAppService.MapToEntityAsync.
    [Required]
    [StringLength(2000)]
    public string Value { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notes { get; set; }
}
