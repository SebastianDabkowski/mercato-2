# Encryption and Key Management Security Guide

This document describes the encryption practices implemented in the Mercato platform for data protection at rest and in transit.

## Overview

The platform implements multiple layers of encryption to protect sensitive data:

1. **Data in Transit**: All traffic is encrypted using HTTPS/TLS
2. **Data at Rest**: Sensitive fields use application-level encryption
3. **Key Management**: Keys are managed using ASP.NET Core Data Protection API with support for managed KMS services

## Data in Transit Encryption

### HTTPS/TLS Configuration

All client connections must use HTTPS. The application enforces this through:

- **HTTP Strict Transport Security (HSTS)**: Enabled in production with the following settings:
  - `MaxAge`: 365 days (1 year)
  - `IncludeSubDomains`: true
  - `Preload`: true (suitable for HSTS preload lists)

- **HTTPS Redirection**: All HTTP requests are automatically redirected to HTTPS

### Configuration (appsettings.json)

No explicit TLS configuration is needed in application settings. TLS is configured at the hosting level (Kestrel, IIS, reverse proxy, or cloud platform).

### Production Deployment Requirements

1. **Certificate Requirements**:
   - Use certificates from a trusted Certificate Authority (CA)
   - Minimum RSA key size: 2048 bits
   - Recommended: ECDSA P-256 or higher
   - Certificate validity: 1 year maximum (per industry standards)

2. **TLS Version Requirements**:
   - Minimum: TLS 1.2
   - Recommended: TLS 1.3
   - Disable: TLS 1.0, TLS 1.1, SSLv3

3. **Cipher Suite Requirements**:
   - Enable only AEAD ciphers (AES-GCM, ChaCha20-Poly1305)
   - Disable: RC4, 3DES, CBC-mode ciphers without MAC-then-Encrypt

## Data at Rest Encryption

### Field-Level Encryption

The platform uses the `IDataEncryptionService` to encrypt sensitive fields before storage. This provides an additional layer of protection beyond storage-level encryption.

#### Sensitive Fields Protected

The following data categories require field-level encryption:

| Entity | Fields | Sensitivity Level |
|--------|--------|-------------------|
| User | TaxId, TwoFactorSecretKey, TwoFactorRecoveryCodes | High |
| PayoutSettings | BankAccountNumber, SepaIban, SepaBic | High |
| DeliveryAddress | PhoneNumber | Medium |

#### Encryption Implementation

- **Algorithm**: AES-256-CBC with HMAC-SHA256 (via ASP.NET Core Data Protection)
- **Key Derivation**: PBKDF2 with purpose-specific keys
- **Service**: `DataEncryptionService` in Infrastructure layer

### Database-Level Encryption

In production, enable storage-level encryption:

#### Azure SQL Database
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.database.windows.net;Database=mercato;Encrypt=True;TrustServerCertificate=False;"
  }
}
```
Enable Transparent Data Encryption (TDE) in Azure Portal.

#### SQL Server
Enable TDE at the database level:
```sql
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE YourCertificateName;

ALTER DATABASE MercatoDB
SET ENCRYPTION ON;
```

#### PostgreSQL
Enable `pgcrypto` extension and use encrypted storage volumes.

## Key Management

### ASP.NET Core Data Protection

The platform uses ASP.NET Core Data Protection API for key management, which provides:

- Automatic key generation and rotation
- Key encryption using master keys
- Support for distributed deployments

### Configuration Options

#### Development Environment
Keys are stored in-memory and regenerated on restart. No persistent storage is required.

#### Production Environment

Configure persistent key storage in `appsettings.Production.json`:

```json
{
  "DataEncryption": {
    "Enabled": true,
    "Purpose": "SD.Project.SensitiveData.v1",
    "KeysPath": "/path/to/secure/keys"
  }
}
```

### Cloud Provider Key Management

#### Azure Key Vault (Recommended for Azure deployments)

1. Create an Azure Key Vault
2. Configure the application:
   ```csharp
   builder.Services.AddDataProtection()
       .PersistKeysToAzureBlobStorage(connectionString, containerName, blobName)
       .ProtectKeysWithAzureKeyVault(keyVaultUri, credential);
   ```
3. Grant the application identity access to Key Vault

#### AWS KMS

1. Create a KMS key in AWS
2. Store encrypted keys in S3
3. Use `IXmlRepository` implementation for S3 storage

#### Google Cloud KMS

1. Create a keyring and key in Cloud KMS
2. Use Cloud Storage for key persistence
3. Configure key encryption using Cloud KMS

### Key Rotation

#### Automatic Rotation
Data Protection API automatically rotates keys every 90 days by default. Old keys are retained for decryption.

#### Manual Rotation Procedure

1. **Planned Rotation**:
   ```bash
   # Generate new key (handled automatically by Data Protection)
   # Application will start using new key immediately
   # Old keys remain valid for decryption
   ```

2. **Emergency Rotation** (Key Compromise):
   ```bash
   # 1. Revoke compromised key in KMS
   # 2. Generate new master key
   # 3. Re-encrypt all data protection keys
   # 4. Restart application instances
   ```

## Compliance Checklist

### Pre-Deployment Audit

- [ ] HTTPS enforced with valid TLS certificate
- [ ] HSTS enabled with appropriate max-age
- [ ] TLS 1.2+ required, older versions disabled
- [ ] Strong cipher suites configured
- [ ] Field-level encryption enabled for sensitive data
- [ ] Storage-level encryption enabled (TDE or equivalent)
- [ ] Key management service configured (not file system in production)
- [ ] Key rotation policy documented and tested
- [ ] Encryption keys not stored in source code or config files
- [ ] Certificate renewal process documented

### Security Standards Compliance

| Standard | Requirement | Implementation |
|----------|-------------|----------------|
| PCI-DSS | Encrypt cardholder data | Field-level encryption for payment tokens |
| GDPR | Protect personal data | Encryption at rest and in transit |
| SOC 2 | Encryption controls | Full implementation as described above |

## Incident Response

### Suspected Key Compromise

1. **Immediate Actions**:
   - Rotate affected keys immediately
   - Review audit logs for unauthorized access
   - Assess scope of potential data exposure

2. **Communication**:
   - Notify security team
   - Document incident timeline
   - Prepare breach notification if required

3. **Recovery**:
   - Deploy new keys
   - Re-encrypt affected data if necessary
   - Update access policies

### Contact

For security incidents, contact the security team at: security@example.com

## References

- [ASP.NET Core Data Protection](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/)
- [Azure Key Vault Best Practices](https://docs.microsoft.com/en-us/azure/key-vault/general/best-practices)
- [OWASP Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)
