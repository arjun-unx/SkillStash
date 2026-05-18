namespace PromptStash.Api.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"Entity '{entity}' with key '{key}' was not found.") { }
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("Access to the requested resource is forbidden.") { }
    public ForbiddenAccessException(string message) : base(message) { }
}
