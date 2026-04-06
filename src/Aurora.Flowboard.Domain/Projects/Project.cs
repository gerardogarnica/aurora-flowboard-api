using Aurora.Flowboard.Domain.Projects.Events;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.Projects;

public sealed class Project : BaseEntity
{
    private const int MaxNameLength = 100;

    private static readonly Dictionary<ProjectStatus, ProjectStatus[]> ValidTransitions = new()
    {
        [ProjectStatus.Draft] = [ProjectStatus.Active, ProjectStatus.Archived],
        [ProjectStatus.Active] = [ProjectStatus.OnHold, ProjectStatus.Completed, ProjectStatus.Archived],
        [ProjectStatus.OnHold] = [ProjectStatus.Active, ProjectStatus.Archived],
        [ProjectStatus.Completed] = [ProjectStatus.Archived],
        [ProjectStatus.Archived] = []
    };

    private readonly List<ProjectMember> _members = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateOnly? EstimatedCompletionDate { get; private set; }
    public ProjectStatus Status { get; private set; }
    public bool IsActive => Status == ProjectStatus.Active;
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private bool IsModifiable => Status is ProjectStatus.Draft or ProjectStatus.Active or ProjectStatus.OnHold;

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
        Status = ProjectStatus.Draft;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<Project> Create(
        string name,
        string? description,
        DateOnly? estimatedCompletionDate,
        IReadOnlyCollection<(User User, ProjectRole Role)> members,
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

        foreach ((User user, ProjectRole role) in members)
        {
            Result result = project.AddMember(user, role, createdOnUtc);

            if (!result.IsSuccessful)
            {
                return Result.Fail<Project>(result.Error);
            }
        }

        project.AddDomainEvent(new ProjectCreatedDomainEvent(project.Id));

        return project;
    }

    public Result Update(
        string name,
        string? description,
        DateOnly? estimatedCompletionDate,
        DateTime updatedOnUtc)
    {
        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
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

    public Result ChangeStatus(ProjectStatus newStatus, DateTime updatedOnUtc)
    {
        if (Status == newStatus)
        {
            return Result.Fail(ProjectErrors.InvalidStatusTransition);
        }

        if (!IsValidTransition(Status, newStatus))
        {
            return Result.Fail(ProjectErrors.InvalidStatusTransition);
        }

        ProjectStatus oldStatus = Status;
        Status = newStatus;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new ProjectStatusChangedDomainEvent(Id, oldStatus, newStatus));

        return Result.Ok();
    }

    public Result AddMember(User user, ProjectRole role, DateTime joinedOnUtc)
    {
        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
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
        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
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

    private static bool IsValidTransition(ProjectStatus from, ProjectStatus to) =>
        ValidTransitions.TryGetValue(from, out ProjectStatus[]? targets) && targets.Contains(to);
}
