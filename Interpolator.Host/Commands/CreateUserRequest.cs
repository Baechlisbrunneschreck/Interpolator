namespace Interpolator.Host.Commands;

public record CreateUserRequest(string? FirstName, string? LastName, bool Internal);