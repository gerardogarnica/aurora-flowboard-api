using Aurora.Flowboard.Domain.Projects.Events;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.Projects;

public sealed class Project : BaseEntity
{
    private const int MaxNameLength = 100;

    private readonly List<ProjectMember> _members = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateOnly? EstimatedCompletionDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private Project() : base(Guid.Empty) { } // EF Core

    private Project(
        Guid id,
        string name,
        string? description,
        DateOnly? estimatedCompletionDate,
        DateTime createdOnUtc) : base(id)
    {
        Name = name;
        Description = description;
        EstimatedCompletionDate = estimatedCompletionDate;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<Project> Create(
        string name,
        string? description,
        DateOnly? estimatedCompletionDate,
        DateTime createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<Project>(ProjectErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<Project>(ProjectErrors.NameTooLong);
        }

        var project = new Project(
            Guid.NewGuid(),
            name.Trim(),
            description?.Trim(),
            estimatedCompletionDate,
            createdOnUtc);

        project.AddDomainEvent(new ProjectCreatedDomainEvent(project.Id));

        return project;
    }

    public Result Update(
        string name,
        string? description,
        DateOnly? estimatedCompletionDate,
        DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(ProjectErrors.Deactivated);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(ProjectErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail(ProjectErrors.NameTooLong);
        }

        Name = name.Trim();
        Description = description?.Trim();
        EstimatedCompletionDate = estimatedCompletionDate;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new ProjectUpdatedDomainEvent(Id));

        return Result.Ok();
    }

    public Result Deactivate(DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(ProjectErrors.AlreadyDeactivated);
        }

        IsActive = false;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new ProjectDeactivatedDomainEvent(Id));

        return Result.Ok();
    }

    public Result AddMember(User user, ProjectRole role, DateTime joinedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(ProjectErrors.Deactivated);
        }

        if (!user.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (_members.Any(m => m.UserId == user.Id))
        {
            return Result.Fail(ProjectErrors.MemberAlreadyExists);
        }

        var member = ProjectMember.Create(Id, user.Id, role, joinedOnUtc);
        _members.Add(member);

        AddDomainEvent(new ProjectMemberAddedDomainEvent(Id, user.Id, role));

        return Result.Ok();
    }

    public Result RemoveMember(Guid userId, DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(ProjectErrors.Deactivated);
        }

        var member = _members.FirstOrDefault(m => m.UserId == userId);

        if (member is null)
        {
            return Result.Fail(ProjectErrors.MemberNotFound);
        }

        _members.Remove(member);
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new ProjectMemberRemovedDomainEvent(Id, userId));

        return Result.Ok();
    }
}
