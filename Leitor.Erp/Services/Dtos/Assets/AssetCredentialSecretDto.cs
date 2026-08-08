namespace Leitor.Erp.Services.Dtos.Assets;

// Returned only by AssetCredentialAppService.RevealAsync (Assets.RevealCredentials-gated) - never
// part of the regular List/Get response shape (AssetCredentialDto).
public class AssetCredentialSecretDto
{
    public string Value { get; set; } = string.Empty;
}
