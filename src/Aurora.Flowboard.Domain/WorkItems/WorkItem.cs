using Aurora.Flowboard.Domain.Flows;
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

    public string Title { get; private set; }
    public string? Description { get; private set; }
    public WorkItemType Type { get; private set; }
    public Priority Priority { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid FlowStateId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid CreatedById { get; private set; }
    public string Code { get; private set; }
    public int SequenceNumber { get; private set; }
    public int? EstimatedPoints { get; private set; }
    public DateOnly? EstimatedCompletionDate { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public DateTime? CompletedOnUtc { get; private set; }

    public Project Project { get; init; } = null!;
    public FlowState FlowState { get; private set; } = null!;
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    public IReadOnlyCollection<StateTransitionHistory> StateHistory => _stateHistory.AsReadOnly();
    public IReadOnlyCollection<WorkItemChangeLog> ChangeLogs => _changeLogs.AsReadOnly();
    public IReadOnlyCollection<WorkItemTag> Tags => _tags.AsReadOnly();

    private WorkItem() : base(Guid.Empty) { } // EF Core

    private WorkItem(
        Guid id,
        string title,
        string? description,
        WorkItemType type,
        Priority priority,
        Guid projectId,
        Guid flowStateId,
        Guid createdById,
        Guid? assigneeId,
        string code,
        int sequenceNumber,
        int? estimatedPoints,
        DateOnly? estimatedCompletionDate,
        DateTime createdOnUtc) : base(id)
    {
        Title = title;
        Description = description;
        Type = type;
        Priority = priority;
        ProjectId = projectId;
        FlowStateId = flowStateId;
        CreatedById = createdById;
        AssigneeId = assigneeId;
        Code = code;
        SequenceNumber = sequenceNumber;
        EstimatedPoints = estimatedPoints;
        EstimatedCompletionDate = estimatedCompletionDate;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<WorkItem> Create(
        string title,
        string? description,
        WorkItemType type,
        Priority priority,
        Project project,
        Flow flow,
        User createdBy,
        int? estimatedPoints,
        DateOnly? estimatedCompletionDate,
        DateTime createdOnUtc,
        User? assignee = null)
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
            return Result.Fail<WorkItem>(WorkItemErrors.UserNotProjectMember);
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

        FlowState? initialState = flow.GetInitialState();

        if (initialState is null)
        {
            return Result.Fail<WorkItem>(FlowErrors.NoInitialState);
        }

        int sequenceNumber = project.IncrementWorkItemCounter();
        string code = $"{project.Prefix}-{sequenceNumber}";

        var workItem = new WorkItem(
            Guid.NewGuid(),
            title.Trim(),
            description?.Trim(),
            type,
            priority,
            project.Id,
            initialState.Id,
            createdBy.Id,
            assignee?.Id,
            code,
            sequenceNumber,
            estimatedPoints,
            estimatedCompletionDate,
            createdOnUtc)
        {
            Project = project,
            FlowState = initialState
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

    public Result Update(
        string title,
        string? description,
        Priority priority,
        int? estimatedPoints,
        DateOnly? estimatedCompletionDate,
        User changedBy,
        DateTime updatedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Fail(WorkItemErrors.TitleRequired);
        }

        if (title.Length > MaxTitleLength)
        {
            return Result.Fail(WorkItemErrors.TitleTooLong);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail(WorkItemErrors.DescriptionTooLong);
        }

        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        Title = title.Trim();
        Description = description?.Trim();
        Priority = priority;
        EstimatedPoints = estimatedPoints;
        EstimatedCompletionDate = estimatedCompletionDate;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.Updated, null, updatedOnUtc));

        return Result.Ok();
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

        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
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

    public Result Move(FlowState toState, User changedBy, string? reason, DateTime changedOnUtc)
    {
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (toState.FlowId != FlowState.FlowId)
        {
            return Result.Fail(WorkItemErrors.TargetStateNotInFlow);
        }

        FlowTransition? transition = FlowState.Flow.FindTransition(FlowStateId, toState.Id);

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
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
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
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
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
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(author.Id))
        {
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
        }

        if (!author.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
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
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
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
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
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
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(user.Id))
        {
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
        }

        if (!user.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
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
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
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
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
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
}
