using Aurora.Flowboard.Domain.Components.Events;
using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.Components;

public sealed class Component : BaseEntity
{
    public const int MaxNameLength = 50;

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public ComponentStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public Project Project { get; init; } = null!;

    private Component() : base(Guid.Empty) { } // EF Core

    private Component(
        Guid id,
        Guid projectId,
        string name,
        Guid createdBy,
        DateTime createdOnUtc) : base(id)
    {
        ProjectId = projectId;
        Name = name;
        Status = ComponentStatus.Active;
        CreatedBy = createdBy;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<Component> Create(
        string name,
        Project project,
        User createdBy,
        DateTime createdOnUtc)
    {
        if (!project.IsAdmin(createdBy.Id))
        {
            return Result.Fail<Component>(ComponentErrors.OnlyAdminCanManageComponent);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<Component>(ComponentErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<Component>(ComponentErrors.NameTooLong);
        }

        if (project.Components.Any(c => string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail<Component>(ComponentErrors.DuplicateName);
        }

        var component = new Component(
            Guid.NewGuid(),
            project.Id,
            name.Trim(),
            createdBy.Id,
            createdOnUtc)
        {
            Project = project
        };

        project.RegisterComponent(component);

        component.AddDomainEvent(new ComponentCreatedDomainEvent(component.Id, component.ProjectId, component.Name));

        return component;
    }

    public Result Rename(string newName, User changedBy, DateTime updatedOnUtc)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(ComponentErrors.OnlyAdminCanManageComponent);
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Fail(ComponentErrors.NameRequired);
        }

        if (newName.Length > MaxNameLength)
        {
            return Result.Fail(ComponentErrors.NameTooLong);
        }

        if (Project.Components.Any(c => c.Id != Id && string.Equals(c.Name, newName.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail(ComponentErrors.DuplicateName);
        }

        string oldName = Name;
        Name = newName.Trim();
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new ComponentRenamedDomainEvent(Id, ProjectId, oldName, Name));

        return Result.Ok();
    }

    public Result Retire(User changedBy, DateTime updatedOnUtc)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(ComponentErrors.OnlyAdminCanManageComponent);
        }

        if (Status == ComponentStatus.Retired)
        {
            return Result.Fail(ComponentErrors.AlreadyRetired);
        }

        Status = ComponentStatus.Retired;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new ComponentRetiredDomainEvent(Id, ProjectId));

        return Result.Ok();
    }
}
