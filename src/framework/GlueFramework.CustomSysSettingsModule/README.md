# Glue Framework Custom System Settings Module

OrchardCore module for managing custom site settings with encryption support.

## Features

- **Custom Site Settings** - Store and manage tenant-specific settings
- **KeyVault Integration** - Secure storage for sensitive configuration
- **AES-GCM Encryption** - Built-in encryption service for data protection
- **Admin UI** - Manage settings through OrchardCore admin panel

## Key Services

- `ISysSettingsService` - Access and modify site settings
- `IKeyVaultService` - Store and retrieve secrets securely
- `AesGcmCryptoService` - Symmetric encryption/decryption

## Usage

```csharp
public class MyService
{
    private readonly ISysSettingsService _settingsService;
    private readonly IKeyVaultService _keyVault;
    
    public MyService(ISysSettingsService settingsService, IKeyVaultService keyVault)
    {
        _settingsService = settingsService;
        _keyVault = keyVault;
    }
    
    public async Task<string> GetApiKeyAsync()
    {
        return await _keyVault.GetSecretAsync("ExternalApiKey");
    }
}
```

## Admin Access

- Settings management: `/Admin/CustomSiteSettings`
- KeyVault management: `/Admin/KeyVault`

Depends on `GlueFramework.OrchardCoreModule` and `GlueFramework.WebCore`.
