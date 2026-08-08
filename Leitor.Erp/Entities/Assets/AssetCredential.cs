using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Assets;

// One secret against a ConfigurationItem - a server/router/etc typically has several distinct
// credentials (OS admin login, BIOS password, SNMP community string, ...), so this is a child
// aggregate root (mirrors CustomerContact/VendorContact) rather than a single field on the CI.
// EncryptedValue is ciphertext only - AssetCredentialAppService is the one place that ever calls
// IStringEncryptionService.Encrypt/Decrypt against it; the plain value never round-trips through
// AssetCredentialDto (list/get), only through CreateUpdateAssetCredentialDto (write) and the
// dedicated RevealAsync method (read), each independently permission-gated.
public class AssetCredential : FullAuditedAggregateRoot<Guid>
{
    public Guid ConfigurationItemId { get; set; }
    public string Label { get; set; } = string.Empty;
    public AssetCredentialType CredentialType { get; set; } = AssetCredentialType.Password;
    public string? Username { get; set; }
    public string EncryptedValue { get; set; } = string.Empty;
    public string? Notes { get; set; }

    protected AssetCredential()
    {
    }

    public AssetCredential(Guid id, Guid configurationItemId, string label)
        : base(id)
    {
        ConfigurationItemId = configurationItemId;
        Label = label;
    }
}
