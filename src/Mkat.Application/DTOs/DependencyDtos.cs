namespace Mkat.Application.DTOs;

public record AddDependencyRequest
{
    public Guid DependencyServiceId { get; init; }
}

public record DependencyResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record DependencyGraphResponse
{
    public List<DependencyGraphNode> Nodes { get; init; } = new();
    public List<DependencyGraphEdge> Edges { get; init; } = new();
}

public record DependencyGraphNode
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public bool IsSuppressed { get; init; }
    public string? SuppressionReason { get; init; }
}

public record DependencyGraphEdge
{
    public Guid DependentId { get; init; }
    public Guid DependencyId { get; init; }
}
