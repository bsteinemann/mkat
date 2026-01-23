using Mkat.Domain.Enums;

namespace Mkat.Application.DTOs;

public record CreateContactRequest
{
    public string Name { get; init; } = string.Empty;
}

public record UpdateContactRequest
{
    public string Name { get; init; } = string.Empty;
}

public record ContactResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<ContactChannelResponse> Channels { get; init; } = new();
    public int ServiceCount { get; init; }
}

public record ContactChannelResponse
{
    public Guid Id { get; init; }
    public ChannelType Type { get; init; }
    public string Configuration { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AddChannelRequest
{
    public ChannelType Type { get; init; }
    public string Configuration { get; init; } = string.Empty;
}

public record UpdateChannelRequest
{
    public string Configuration { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
}

public record SetServiceContactsRequest
{
    public List<Guid> ContactIds { get; init; } = new();
}
