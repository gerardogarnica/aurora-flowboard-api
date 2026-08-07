using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Projects.Events;
using Aurora.Flowboard.Domain.Users;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Domain.Projects;

public sealed class Project : BaseEntity
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    private static readonly Dictionary<ProjectStatus, ProjectStatus[]> ValidTransitions = new()
    {
        [ProjectStatus.Draft] = [ProjectStatus.Active, ProjectStatus.Archived],
        [ProjectStatus.Active] = [ProjectStatus.OnHold, ProjectStatus.Completed, ProjectStatus.Archived],
        [ProjectStatus.OnHold] = [ProjectStatus.Active, ProjectStatus.Archived],
        [ProjectStatus.Completed] = [ProjectStatus.Archived],
        [ProjectStatus.Archived] = []
    };

    private readonly List<ProjectMember> _members = [];
    private readonly List<ProjectChangeLog> _changeLogs = [];
    private readonly List<Flow> _flows = [];
    private readonly List<WorkItem> _workItems = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Code { get; private set; }
    public Color Color { get; private set; }
    public ProjectKind Kind { get; private set; }
    public ProjectStatus Status { get; private set; }
    public int WorkItemCounter { get; private set; }
    public DateTime LastActivityDate { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public User Creator { get; init; } = null!; // Navigation property

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<ProjectChangeLog> ChangeLogs => _changeLogs.AsReadOnly();
    public IReadOnlyCollection<Flow> Flows => _flows.AsReadOnly();
    public IReadOnlyCollection<WorkItem> WorkItems => _workItems.AsReadOnly();

    private bool IsModifiable => Status is ProjectStatus.Draft or ProjectStatus.Active or ProjectStatus.OnHold;

    private Project() : base(Guid.Empty) { } // EF Core

    private Project(
        Guid id,
        string name,
        string? description,
        string code,
        ProjectKind kind,
        Color color,
        Guid createdBy,
        DateTime createdOnUtc) : base(id)
    {
        Name = name;
        Description = description;
        Code = code;
        Kind = kind;
        Color = color;
        WorkItemCounter = 0;
        LastActivityDate = createdOnUtc;
        Status = ProjectStatus.Draft;
        CreatedBy = createdBy;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<Project> Create(
        string name,
        string? description,
        string code,
        ProjectKind kind,
        Color color,
        User createdBy,
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

        Result<ProjectCode> codeResult = ProjectCode.Create(code);
        if (!codeResult.IsSuccessful)
        {
            return Result.Fail<Project>(codeResult.Error);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail<Project>(ProjectErrors.DescriptionTooLong);
        }

        var project = new Project(
            Guid.NewGuid(),
            name.Trim(),
            description?.Trim(),
            codeResult.Value.Value,
            kind,
            color,
            createdBy.Id,
            createdOnUtc);

        ProjectMember creatorMember = ProjectMember.Create(project.Id, createdBy.Id, ProjectRole.Admin, createdOnUtc);
        project._members.Add(creatorMember);

        project._changeLogs.Add(ProjectChangeLog.Create(project, createdBy, ProjectChangeType.Created, null, createdOnUtc));

        project.AddDomainEvent(new ProjectCreatedDomainEvent(project.Id));

        return project;
    }

    public Result Update(
        string name,
        string? description,
        Color color,
        User changedBy,
        DateTime updatedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanUpdateProject);
        }

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

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail(ProjectErrors.DescriptionTooLong);
        }

        Name = name.Trim();
        Description = description?.Trim();
        Color = color;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.Updated, null, updatedOnUtc));

        AddDomainEvent(new ProjectUpdatedDomainEvent(Id));

        return Result.Ok();
    }

    public Result ChangeKind(ProjectKind newKind, User changedBy, DateTime updatedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanChangeKind);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (newKind == Kind)
        {
            return Result.Fail(ProjectErrors.KindUnchanged);
        }

        ProjectKind oldKind = Kind;
        Kind = newKind;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.KindChanged, null, updatedOnUtc, newKind: newKind));

        AddDomainEvent(new ProjectKindChangedDomainEvent(Id, oldKind, newKind));

        return Result.Ok();
    }

    public Result ChangeStatus(ProjectStatus newStatus, User changedBy, DateTime updatedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanChangeStatus);
        }

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

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.StatusChanged, null, updatedOnUtc, newStatus));

        AddDomainEvent(new ProjectStatusChangedDomainEvent(Id, oldStatus, newStatus));

        return Result.Ok();
    }

    public Result AddMember(User user, ProjectRole role, User changedBy, DateTime joinedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanAddMembers);
        }

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

        ProjectMember member = ProjectMember.Create(Id, user.Id, role, joinedOnUtc);
        _members.Add(member);

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.MemberAdded, user.Id, joinedOnUtc));

        AddDomainEvent(new ProjectMemberAddedDomainEvent(Id, user.Id, role));

        return Result.Ok();
    }

    public Result RemoveMember(Guid userId, User changedBy, DateTime updatedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanRemoveMembers);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        ProjectMember? member = _members.FirstOrDefault(m => m.UserId == userId);

        if (member is null)
        {
            return Result.Fail(ProjectErrors.MemberNotFound);
        }

        if (member.Role == ProjectRole.Admin && _members.Count(m => m.Role == ProjectRole.Admin) == 1)
        {
            return Result.Fail(ProjectErrors.CannotRemoveLastAdmin);
        }

        _members.Remove(member);
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.MemberRemoved, userId, updatedOnUtc));

        AddDomainEvent(new ProjectMemberRemovedDomainEvent(Id, userId));

        return Result.Ok();
    }

    public int IncrementWorkItemCounter()
    {
        WorkItemCounter++;
        return WorkItemCounter;
    }

    public bool CanAddOrUpdateFlow()
    {
        return Status is ProjectStatus.Draft or ProjectStatus.Active;
    }

    public bool CanAddOrUpdateWorkItem()
    {
        return Status is ProjectStatus.Active;
    }

    internal bool IsAdmin(Guid userId) =>
        _members.Any(m => m.UserId == userId && m.Role == ProjectRole.Admin);

    internal bool IsMember(Guid userId) =>
        _members.Any(m => m.UserId == userId);

    internal ProjectRole? GetRole(Guid userId) =>
        _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    private static bool IsValidTransition(ProjectStatus from, ProjectStatus to) =>
        ValidTransitions.TryGetValue(from, out ProjectStatus[]? targets) && targets.Contains(to);
}
