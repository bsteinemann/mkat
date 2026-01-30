# Plan: Add Encryption for Secrets

## Overview

Implement transparent encryption for all sensitive data stored in the database using Microsoft's DataProtection API with EF Core value converters.

## Secrets Inventory (Fields to Encrypt)

| Entity | Field | Current State | Priority |
|--------|-------|---------------|----------|
| ContactChannel | Configuration | Plaintext JSON (Telegram tokens) | HIGH |
| Peer | HeartbeatToken | Plaintext | HIGH |
| Peer | WebhookToken | Plaintext | HIGH |
| PushSubscription | P256dhKey | Plaintext base64 | MEDIUM |
| PushSubscription | AuthKey | Plaintext base64 | MEDIUM |
| Monitor | Token | Plaintext | MEDIUM |
| Monitor | ConfigJson | Plaintext JSON | MEDIUM |
| NotificationChannel | ConfigJson | Plaintext JSON | MEDIUM |

## Implementation Approach

Use **Microsoft.AspNetCore.DataProtection** with EF Core value converters:
- Encryption is transparent to Application/Domain layers
- Key management handled automatically (rotation, etc.)
- Keys stored in a configurable location (file or env var)

## Implementation Steps

### Step 1: Create Encryption Service

**New files:**
- `src/Mkat.Infrastructure/Encryption/IEncryptionService.cs` - Interface
- `src/Mkat.Infrastructure/Encryption/DataProtectionEncryptionService.cs` - Implementation

```csharp
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    bool TryDecrypt(string ciphertext, out string plaintext); // For migration
}
```

### Step 2: Configure DataProtection in Program.cs

**File:** `src/Mkat.Api/Program.cs`

- Add DataProtection service with file-based key storage
- New env var: `MKAT_ENCRYPTION_KEY_PATH` (default: `./keys`)
- Register `IEncryptionService` as singleton

### Step 3: Create EF Core Value Converter

**New file:** `src/Mkat.Infrastructure/Data/EncryptedStringConverter.cs`

Value converter that:
1. Encrypts on write (property → database)
2. Decrypts on read (database → property)
3. Handles null/empty strings gracefully
4. Supports migration: tries decrypt, falls back to plaintext if fails

### Step 4: Apply Converters in DbContext

**File:** `src/Mkat.Infrastructure/Data/MkatDbContext.cs`

Apply the converter to all sensitive fields:
- `ContactChannel.Configuration`
- `Peer.HeartbeatToken`, `Peer.WebhookToken`
- `PushSubscription.P256dhKey`, `PushSubscription.AuthKey`
- `Monitor.Token`, `Monitor.ConfigJson`
- `NotificationChannel.ConfigJson`

### Step 5: Add Migration for Existing Data

**New file:** `src/Mkat.Infrastructure/Migrations/[timestamp]_EncryptExistingSecrets.cs`

Create a data migration that:
1. Reads all rows with plaintext secrets
2. Encrypts them using the new service
3. Updates the rows

Note: Value converters auto-encrypt new data; this handles existing data.

### Step 6: Add Unit Tests

**New file:** `tests/Mkat.Infrastructure.Tests/Encryption/EncryptionServiceTests.cs`

Test cases:
- Encrypt/decrypt round-trip
- Null handling
- Empty string handling
- Special characters
- Migration fallback (plaintext input)

### Step 7: Update Documentation

**Files to update:**
- `CLAUDE.md` - Add encryption rules
- `docs/architecture.md` - Document encryption layer
- `.env.template` - Add `MKAT_ENCRYPTION_KEY_PATH`

Add to CLAUDE.md:
```markdown
## Secrets Handling

- NEVER store secrets (tokens, API keys, passwords) in plaintext in the database
- All sensitive fields must use the `EncryptedStringConverter` in DbContext
- When adding new fields that contain secrets, apply the converter in MkatDbContext.OnModelCreating()
```

## File Changes Summary

| File | Action |
|------|--------|
| `src/Mkat.Infrastructure/Encryption/IEncryptionService.cs` | Create |
| `src/Mkat.Infrastructure/Encryption/DataProtectionEncryptionService.cs` | Create |
| `src/Mkat.Infrastructure/Data/EncryptedStringConverter.cs` | Create |
| `src/Mkat.Infrastructure/Data/MkatDbContext.cs` | Modify (add converters) |
| `src/Mkat.Api/Program.cs` | Modify (register services) |
| `tests/Mkat.Infrastructure.Tests/Encryption/EncryptionServiceTests.cs` | Create |
| `CLAUDE.md` | Modify (add encryption rules) |
| `.env.template` | Modify (add key path var) |

## Migration Strategy

The value converter will have a **dual-read capability**:
1. Try to decrypt the value
2. If decryption fails (not valid ciphertext), assume plaintext and return as-is
3. On next save, value will be encrypted

This means:
- Existing plaintext data continues to work
- New writes are always encrypted
- Re-saving any entity encrypts its secrets
- Optional: add startup migration to encrypt all existing data

## Verification

1. **Unit tests pass:** `dotnet test tests/Mkat.Infrastructure.Tests`
2. **Integration test:** Create a contact with Telegram channel, verify Configuration is encrypted in DB
3. **Manual verification:**
   ```bash
   sqlite3 mkat.db "SELECT Configuration FROM ContactChannels LIMIT 1"
   # Should show encrypted blob, not readable JSON
   ```
4. **Migration test:** Start with existing plaintext data, verify app can read it, then verify re-save encrypts

## Environment Variables

| Variable | Required | Default | Purpose |
|----------|----------|---------|---------|
| MKAT_ENCRYPTION_KEY_PATH | No | `./keys` | Directory for DataProtection keys |

## Security Considerations

- Keys directory should have restricted permissions (0700)
- Keys should be backed up separately from database
- In Docker, mount keys as a volume that persists across restarts
- Consider using Docker secrets in production
