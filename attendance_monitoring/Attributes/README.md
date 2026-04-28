# Email Domain Validation

This folder contains custom validation attributes for the attendance monitoring application.

## AllowedEmailDomainsAttribute

A custom validation attribute that validates email addresses against a configurable list of allowed domains.

### Usage

The attribute is applied to email properties in DTOs:

```csharp
[AllowedEmailDomains(ErrorMessage = "Email domain is not allowed. Please use an email address from an allowed domain.")]
public string Email { get; set; } = string.Empty;
```

### Configuration

Allowed domains are configured in `appsettings.json`:

```json
{
  "AllowedEmailDomains": [
    "gmail.com",
    "outlook.com",
    "yahoo.com",
    "hotmail.com",
    "aol.com",
    "icloud.com",
    "protonmail.com",
    "yandex.com",
    "mail.com"
  ]
}
```

### Behavior

- If no domains are configured, all domains are allowed (fallback behavior)
- If domains are configured, only those domains are allowed
- The validation is case-insensitive
- Provides descriptive error messages for invalid domains