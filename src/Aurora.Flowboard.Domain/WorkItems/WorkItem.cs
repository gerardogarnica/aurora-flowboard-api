using Aurora.Flowboard.Domain.Components;
using Aurora.Flowboard.Domain.Milestones;
using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Users;
using Aurora.Flowboard.Domain.WorkItems.Events;

namespace Aurora.Flowboard.Domain.WorkItems;

public sealed class WorkItem : BaseEntity
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 4000;
    public const int MaxCodeLength = 20;

    private readonly List<Comment> _comments = [];
    private readonly List<TimeEntry> _timeEntries = [];
    private readonly List<StateTransitionHistory> _stateHistory = [];
    private readonly List<WorkItemChangeLog> _changeLogs = [];
    private readonly List<WorkItemTag> _tags = [];

    public string Code { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public WorkItemType Type { get; private set; }
    public Priority Priority { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid FlowStateId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid CreatedById { get; private set; }
    public int SequenceNumber { get; private set; }
    public int? EstimatedPoints { get; private set; }
    public DateOnly? EstimatedCompletionDate { get; private set; }
    public Guid? ComponentId { get; private set; }
    public Guid? MilestoneId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public DateTime? CompletedOnUtc { get; private set; }

    public Project Project { get; init; } = null!;
    public FlowState FlowState { get; private set; } = null!;
    public Component? Component { get; private set; }
    public Milestone? Milestone { get; private set; }
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    public IReadOnlyCollection<StateTransitionHistory> StateHistory => _stateHistory.AsReadOnly();
    public IReadOnlyCollection<WorkItemChangeLog> ChangeLogs => _changeLogs.AsReadOnly();
    public IReadOnlyCollection<WorkItemTag> Tags => _tags.AsReadOnly();

    private WorkItem() : base(Guid.Empty) { } // EF Core

    private WorkItem(
        Guid id,
        string code,
        string title,
        string? description,
        WorkItemType type,
        Priority priority,
        Guid projectId,
        Guid flowStateId,
        Guid createdById,
        Guid? assigneeId,
        int sequenceNumber,
        int? estimatedPoints,
        DateOnly? estimatedCompletionDate,
        Guid? milestoneId,
        Guid? componentId,
        DateTime createdOnUtc) : base(id)
    {
        Code = code;
        Title = title;
        Description = description;
        Type = type;
        Priority = priority;
        ProjectId = projectId;
        FlowStateId = flowStateId;
        CreatedById = createdById;
        AssigneeId = assigneeId;
        SequenceNumber = sequenceNumber;
        EstimatedPoints = estimatedPoints;
        EstimatedCompletionDate = estimatedCompletionDate;
        MilestoneId = milestoneId;
        ComponentId = componentId;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<WorkItem> Create(
        string title,
        string? description,
        WorkItemType type,
        Priority priority,
        Project project,
        User createdBy,
        int? estimatedPoints,
        DateOnly? estimatedCompletionDate,
        DateTime createdOnUtc,
        User? assignee = null,
        Milestone? milestone = null,
        Component? component = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Fail<WorkItem>(WorkItemErrors.TitleRequired);
        }

        if (title.Length > MaxTitleLength)
        {
            return Result.Fail<WorkItem>(WorkItemErrors.TitleTooLong);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail<WorkItem>(WorkItemErrors.DescriptionTooLong);
        }

        if (!project.IsMember(createdBy.Id))
        {
            return Result.Fail<WorkItem>(WorkItemErrors.NotFound);
        }

        if (!project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!createdBy.IsActive)
        {
            return Result.Fail<WorkItem>(UserErrors.Inactive);
        }

        if (assignee is not null)
        {
            if (!project.IsMember(assignee.Id))
            {
                return Result.Fail<WorkItem>(WorkItemErrors.AssigneeNotProjectMember);
            }

            if (!assignee.IsActive)
            {
                return Result.Fail<WorkItem>(WorkItemErrors.AssigneeInactive);
            }
        }

        if (milestone is not null)
        {
            if (milestone.ProjectId != project.Id)
            {
                return Result.Fail<WorkItem>(WorkItemErrors.MilestoneNotInProject);
            }

            if (milestone.Status is MilestoneStatus.Completed or MilestoneStatus.Archived)
            {
                return Result.Fail<WorkItem>(WorkItemErrors.MilestoneNotAcceptingAssignments);
            }
        }

        if (component is not null)
        {
            if (component.ProjectId != project.Id)
            {
                return Result.Fail<WorkItem>(WorkItemErrors.ComponentNotInProject);
            }

            if (component.Status == ComponentStatus.Retired)
            {
                return Result.Fail<WorkItem>(WorkItemErrors.ComponentRetired);
            }
        }

        FlowState? initialState = project.GetInitialFlowState();

        if (initialState is null)
        {
            return Result.Fail<WorkItem>(ProjectErrors.NoInitialFlowState);
        }

        int sequenceNumber = project.IncrementWorkItemCounter();
        string code = $"{project.Prefix}-{sequenceNumber}";

        var workItem = new WorkItem(
            Guid.NewGuid(),
            code,
            title.Trim(),
            description?.Trim(),
            type,
            priority,
            project.Id,
            initialState.Id,
            createdBy.Id,
            assignee?.Id,
            sequenceNumber,
            estimatedPoints,
            estimatedCompletionDate,
            milestone?.Id,
            component?.Id,
            createdOnUtc)
        {
            Project = project,
            FlowState = initialState,
            Milestone = milestone,
            Component = component
        };

        workItem._changeLogs.Add(WorkItemChangeLog.Create(workItem, createdBy, WorkItemChangeType.Created, null, createdOnUtc));
        workItem.AddDomainEvent(new WorkItemCreatedDomainEvent(workItem.Id));

        if (assignee is not null)
        {
            workItem._changeLogs.Add(WorkItemChangeLog.Create(workItem, createdBy, WorkItemChangeType.Assigned, assignee.Id, createdOnUtc));
            workItem.AddDomainEvent(new WorkItemAssignedDomainEvent(workItem.Id, assignee.Id));
        }

        return workItem;
    }

    public Result UpdateTitle(string title, User changedBy, DateTime updatedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Fail(WorkItemErrors.TitleRequired);
        }

        if (title.Length > MaxTitleLength)
        {
            return Result.Fail(WorkItemErrors.TitleTooLong);
        }

        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        string trimmedTitle = title.Trim();

        if (trimmedTitle == Title)
        {
            return Result.Ok();
        }

        Title = trimmedTitle;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.TitleUpdated, null, updatedOnUtc));

        AddDomainEvent(new WorkItemTitleUpdatedDomainEvent(Id, trimmedTitle));

        return Result.Ok();
    }

    public Result UpdateDescription(string? description, User changedBy, DateTime updatedOnUtc)
    {
        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail(WorkItemErrors.DescriptionTooLong);
        }

        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        string? trimmedDescription = description?.Trim();

        if (trimmedDescription == Description)
        {
            return Result.Ok();
        }

        Description = trimmedDescription;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.DescriptionUpdated, null, updatedOnUtc));

        return Result.Ok();
    }

    public Result Move(FlowState toState, User changedBy, string? reason, DateTime changedOnUtc)
    {
        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        if (toState.ProjectId != ProjectId)
        {
            return Result.Fail(WorkItemErrors.TargetStateNotInProject);
        }

        FlowTransition? transition = Project.FindFlowTransition(FlowStateId, toState.Id);

        if (transition is null)
        {
            return Result.Fail(WorkItemErrors.TransitionNotAllowed);
        }

        ProjectRole? role = Project.GetRole(changedBy.Id);

        if (role is null || !transition.AllowedRoles.Contains(role.Value))
        {
            return Result.Fail(WorkItemErrors.TransitionRoleNotAllowed);
        }

        Guid fromStateId = FlowStateId;

        _stateHistory.Add(StateTransitionHistory.Create(
            this,
            FlowState,
            toState,
            changedBy,
            reason,
            changedOnUtc));

        FlowStateId = toState.Id;
        FlowState = toState;
        UpdatedOnUtc = changedOnUtc;

        if (toState.Category is FlowStateCategory.Completed or FlowStateCategory.Cancelled)
        {
            CompletedOnUtc = changedOnUtc;
        }

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.Moved, toState.Id, changedOnUtc));

        AddDomainEvent(new WorkItemMovedDomainEvent(Id, fromStateId, toState.Id));

        return Result.Ok();
    }

    public Result Assign(User assignee, User changedBy, DateTime updatedOnUtc)
    {
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        if (!Project.IsMember(assignee.Id))
        {
            return Result.Fail(WorkItemErrors.AssigneeNotProjectMember);
        }

        if (FlowState.Category == FlowStateCategory.Cancelled)
        {
            return Result.Fail(WorkItemErrors.CancelledWorkItemCannotBeModified);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (!assignee.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        AssigneeId = assignee.Id;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.Assigned, assignee.Id, updatedOnUtc));

        AddDomainEvent(new WorkItemAssignedDomainEvent(Id, assignee.Id));

        return Result.Ok();
    }

    public Result Unassign(User changedBy, DateTime updatedOnUtc)
    {
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (AssigneeId is null)
        {
            return Result.Fail(WorkItemErrors.NotAssigned);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        if (FlowState.Category == FlowStateCategory.Cancelled)
        {
            return Result.Fail(WorkItemErrors.CancelledWorkItemCannotBeModified);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        AssigneeId = null;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.Unassigned, null, updatedOnUtc));

        AddDomainEvent(new WorkItemUnassignedDomainEvent(Id));

        return Result.Ok();
    }

    public Result AddComment(User author, string content, DateTime createdOnUtc)
    {
        Result guardResult = EnsureCanBeModifiedBy(author);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        Result<Comment> commentResult = Comment.Create(this, author, content, createdOnUtc);

        if (!commentResult.IsSuccessful)
        {
            return Result.Fail(commentResult.Error);
        }

        _comments.Add(commentResult.Value);
        _changeLogs.Add(WorkItemChangeLog.Create(this, author, WorkItemChangeType.CommentAdded, commentResult.Value.Id, createdOnUtc));

        AddDomainEvent(new WorkItemCommentAddedDomainEvent(Id, commentResult.Value.Id));

        return Result.Ok();
    }

    public Result UpdateComment(Guid commentId, User changedBy, string content, DateTime updatedOnUtc)
    {
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        Comment? comment = _comments.FirstOrDefault(c => c.Id == commentId && !c.IsDeleted);

        if (comment is null)
        {
            return Result.Fail(WorkItemErrors.CommentNotFound);
        }

        if (comment.AuthorId != changedBy.Id)
        {
            return Result.Fail(WorkItemErrors.CommentNotOwnedByUser);
        }

        Result result = comment.UpdateContent(content, updatedOnUtc);

        if (!result.IsSuccessful)
        {
            return result;
        }

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.CommentUpdated, commentId, updatedOnUtc));

        return Result.Ok();
    }

    public Result RemoveComment(Guid commentId, User changedBy, DateTime updatedOnUtc)
    {
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        Comment? comment = _comments.FirstOrDefault(c => c.Id == commentId && !c.IsDeleted);

        if (comment is null)
        {
            return Result.Fail(WorkItemErrors.CommentNotFound);
        }

        if (comment.AuthorId != changedBy.Id)
        {
            return Result.Fail(WorkItemErrors.CommentNotOwnedByUser);
        }

        Result result = comment.Remove(updatedOnUtc);

        if (!result.IsSuccessful)
        {
            return result;
        }

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.CommentRemoved, commentId, updatedOnUtc));

        return Result.Ok();
    }

    public Result LogTime(User user, decimal hours, string? description, DateTime loggedOnUtc, DateTime createdOnUtc)
    {
        Result guardResult = EnsureCanBeModifiedBy(user);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        Result<TimeEntry> entryResult = TimeEntry.Create(this, user, hours, description, loggedOnUtc, createdOnUtc);

        if (!entryResult.IsSuccessful)
        {
            return Result.Fail(entryResult.Error);
        }

        _timeEntries.Add(entryResult.Value);
        _changeLogs.Add(WorkItemChangeLog.Create(this, user, WorkItemChangeType.TimeLogged, entryResult.Value.Id, createdOnUtc));

        AddDomainEvent(new WorkItemTimeLoggedDomainEvent(Id, entryResult.Value.Id, hours));

        return Result.Ok();
    }

    public Result AddTag(string name, User changedBy, DateTime updatedOnUtc)
    {
        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(WorkItemErrors.TagNameRequired);
        }

        if (name.Length > WorkItemTag.MaxNameLength)
        {
            return Result.Fail(WorkItemErrors.TagNameTooLong);
        }

        string normalizedName = name.Trim().ToLowerInvariant();

        if (_tags.Any(t => t.Name == normalizedName))
        {
            return Result.Fail(WorkItemErrors.DuplicateTagName);
        }

        WorkItemTag tag = WorkItemTag.Create(this, normalizedName);
        _tags.Add(tag);
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.TagAdded, tag.Id, updatedOnUtc));

        AddDomainEvent(new WorkItemTagAddedDomainEvent(Id, tag.Id));

        return Result.Ok();
    }

    public Result RemoveTag(Guid tagId, User changedBy, DateTime updatedOnUtc)
    {
        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        WorkItemTag? tag = _tags.FirstOrDefault(t => t.Id == tagId);

        if (tag is null)
        {
            return Result.Fail(WorkItemErrors.TagNotFound);
        }

        _tags.Remove(tag);
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.TagRemoved, tagId, updatedOnUtc));
        AddDomainEvent(new WorkItemTagRemovedDomainEvent(Id, tagId));

        return Result.Ok();
    }

    private Result EnsureCanBeModifiedBy(User changedBy)
    {
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        return Result.Ok();
    }
}
