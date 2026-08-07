namespace Aurora.Flowboard.Domain.Projects;

public sealed class Component
{
    public const int MaxNameLength = 50;

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public ComponentStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private Component() { } // EF Core

    private Component(
        Guid id,
        Guid projectId,
        string name,
        DateTime createdOnUtc)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
        Status = ComponentStatus.Active;
        CreatedOnUtc = createdOnUtc;
    }

    internal static Result<Component> Create(Guid projectId, string name, DateTime createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<Component>(ProjectErrors.ComponentNameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<Component>(ProjectErrors.ComponentNameTooLong);
        }

        return new Component(Guid.NewGuid(), projectId, name.Trim(), createdOnUtc);
    }

    internal Result Rename(string newName, DateTime updatedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Fail(ProjectErrors.ComponentNameRequired);
        }

        if (newName.Length > MaxNameLength)
        {
            return Result.Fail(ProjectErrors.ComponentNameTooLong);
        }

        Name = newName.Trim();
        UpdatedOnUtc = updatedOnUtc;

        return Result.Ok();
    }

    internal Result Retire(DateTime updatedOnUtc)
    {
        if (Status == ComponentStatus.Retired)
        {
            return Result.Fail(ProjectErrors.ComponentAlreadyRetired);
        }

        Status = ComponentStatus.Retired;
        UpdatedOnUtc = updatedOnUtc;

        return Result.Ok();
    }
}
