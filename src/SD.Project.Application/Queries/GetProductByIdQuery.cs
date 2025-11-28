namespace SD.Project.Application.Queries;

/// <summary>
/// Query to retrieve a single product by its ID.
/// </summary>
public sealed record GetProductByIdQuery(Guid ProductId);
