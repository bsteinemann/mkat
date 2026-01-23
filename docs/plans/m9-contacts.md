# Milestone 9: Contacts & Notification Routing

**Goal:** Per-service notification routing through named contacts with multiple channels

**Dependencies:** M4 (Notifications), M5 (Frontend)

---

## Overview

Contacts represent people who should be notified when a service changes state. Each contact has a name and one or more contact channels (Telegram, Email in future, etc.). Services are linked to one or more contacts — when a service transitions state, all enabled channels of all assigned contacts are notified simultaneously.

A "Default" contact provides backward compatibility with the existing global Telegram configuration.

---

## Domain Model

### New Entity: `Contact`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `Name` | string | Display name (e.g. "John", "On-call") |
| `IsDefault` | bool | Whether this is the default contact |
| `CreatedAt` | DateTime (UTC) | When created |

### New Entity: `ContactChannel`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `ContactId` | Guid | FK to Contact |
| `Type` | ChannelType (enum) | Telegram, Email, etc. |
| `Configuration` | string (JSON) | Channel-specific config |
| `IsEnabled` | bool | Can be toggled without deleting |
| `CreatedAt` | DateTime (UTC) | When created |

Channel configuration stored as JSON per type:
```json
// Telegram
{"botToken": "123:ABC", "chatId": "456"}

// Email (future)
{"smtpHost": "smtp.example.com", "smtpPort": 587, "address": "john@example.com"}
```

### New Join Entity: `ServiceContact`

| Field | Type | Description |
|-------|------|-------------|
| `ServiceId` | Guid | FK to Service |
| `ContactId` | Guid | FK to Contact |

Composite primary key on (ServiceId, ContactId).

### Enum: `ChannelType`

```csharp
public enum ChannelType
{
    Telegram,
    Email  // future
}
```

---

## API Endpoints

### Contact CRUD (authenticated)

```
POST   /api/v1/contacts                            Create contact
GET    /api/v1/contacts                            List contacts
GET    /api/v1/contacts/{id}                       Get contact (with channels)
PUT    /api/v1/contacts/{id}                       Update contact
DELETE /api/v1/contacts/{id}                       Delete contact
```

### Contact Channel Management (authenticated)

```
POST   /api/v1/contacts/{id}/channels              Add channel to contact
PUT    /api/v1/contacts/{id}/channels/{chId}       Update channel config
DELETE /api/v1/contacts/{id}/channels/{chId}       Remove channel
POST   /api/v1/contacts/{id}/channels/{chId}/test  Send test notification
```

### Service-Contact Assignment (authenticated)

```
PUT /api/v1/services/{id}/contacts                 Set contacts for a service (list of contact IDs)
GET /api/v1/services/{id}/contacts                 Get contacts for a service
```

---

## Validation Rules (FluentValidation)

- Contact name: required, non-empty
- A service must always have at least one contact (PUT /services/{id}/contacts fails if list is empty)
- Cannot delete a contact if it's the only contact on any service
- Cannot delete the default contact
- Channel configuration validated per type:
  - Telegram: `botToken` and `chatId` required, non-empty
  - Email (future): `smtpHost`, `smtpPort`, `address` required
- Channel type must be a valid enum value

---

## Alert Dispatch Logic

Updated `AlertDispatchWorker` flow:

1. Pick up pending alert for a service
2. Resolve contacts for that service via `ServiceContact` join
3. If no contacts assigned, fall back to the default contact
4. Collect all enabled `ContactChannel`s across resolved contacts
5. Send alert to each channel (existing dispatch logic per channel type)
6. Record delivery status per channel

---

## Migration from M4

The migration must:

1. Create the `Contact` table
2. Create the `ContactChannel` table
3. Create the `ServiceContact` join table
4. Create a "Default" contact with `IsDefault = true`
5. Migrate the existing global Telegram channel config (`MKAT_TELEGRAM_BOT_TOKEN`, `MKAT_TELEGRAM_CHAT_ID`) into a `ContactChannel` on the default contact
6. Link all existing services to the default contact via `ServiceContact`
7. Remove or deprecate the old global `NotificationChannel` table

---

## Default Contact Behavior

- Created automatically on first startup if no contacts exist (seeded from env vars)
- Services without explicit contact assignments fall back to the default contact
- Cannot be deleted via API
- Can be renamed and have channels added/removed like any other contact
- The `IsDefault` flag is not settable via API

---

## Frontend

- **Contacts page** (`/contacts`): List contacts with channel count and service count
- **Contact create/edit**: Name field, channel list with add/remove/toggle
- **Channel form**: Type selector, type-specific config fields (Telegram: bot token + chat ID)
- **Test button**: Send test notification per channel
- **Service edit form**: Multi-select for contacts (at least one required, pre-selected with default)
- **Service detail page**: Shows assigned contacts with their channel types

---

## Deliverables

- [ ] `Contact` entity and repository
- [ ] `ContactChannel` entity and repository
- [ ] `ServiceContact` join entity
- [ ] `ChannelType` enum
- [ ] EF Core migration (new tables + data migration from global channel)
- [ ] Contact CRUD endpoints and controller
- [ ] Contact channel management endpoints (add, update, remove, test)
- [ ] Service-contact assignment endpoints
- [ ] Updated `AlertDispatchWorker` to route via contacts
- [ ] Default contact creation and fallback logic
- [ ] FluentValidation for contacts, channels, assignments
- [ ] Frontend: contacts page, channel management, service contact picker
- [ ] Unit tests for dispatch routing logic
- [ ] Integration tests for all endpoints

---

## Definition of Done

- Contacts with multiple channels can be created and managed
- Services can be assigned one or more contacts
- Alerts route to all enabled channels of all assigned contacts
- Default contact provides backward compatibility
- Test notification works per channel
- Cannot leave a service with zero contacts
- Cannot delete the default contact
- Migration preserves existing Telegram configuration
- All tests pass

---

**Status:** Draft
**Created:** 2026-01-23
