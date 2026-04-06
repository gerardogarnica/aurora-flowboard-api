using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Users;
using Aurora.Flowboard.Domain.WorkItems.Events;

namespace Aurora.Flowboard.Domain.WorkItems;

public sealed class WorkItem : BaseEntity
{
    private const int MaxTitleLength = 200;

    private readonly List<Comment> _comments = [];
    private readonly List<TimeEntry> _timeEntries = [];
    private readonly List<StateTransitionHistory> _stateHistory = [];
    private readonly List<WorkItemChangeLog> _changeLogs = [];

    public string Title { get; private set; }
    public string? Description { get; private set; }
    public WorkItemType Type { get; private set; }
    public Priority Priority { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid FlowStateId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid CreatedById { get; private set; }
    public int? EstimatedPoints { get; private set; }
    public DateOnly? EstimatedCompletionDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public DateTime? CompletedOnUtc { get; private set; }

    public FlowState FlowState { get; init; } = null!;
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    public IReadOnlyCollection<StateTransitionHistory> StateHistory => _stateHistory.AsReadOnly();
    public IReadOnlyCollection<WorkItemChangeLog> ChangeLogs => _changeLogs.AsReadOnly();

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
        EstimatedPoints = estimatedPoints;
        EstimatedCompletionDate = estimatedCompletionDate;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<WorkItem> Create(
        string title,
        string? description,
        WorkItemType type,
        Priority priority,
        Project project,
        FlowState initialState,
        User createdBy,
        int? estimatedPoints,
        DateOnly? estimatedCompletionDate,
        DateTime createdOnUtc)
    {
        if (!createdBy.IsActive)
        {
            return Result.Fail<WorkItem>(UserErrors.Inactive);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Fail<WorkItem>(WorkItemErrors.TitleRequired);
        }

        if (title.Length > MaxTitleLength)
        {
            return Result.Fail<WorkItem>(WorkItemErrors.TitleTooLong);
        }

        var workItem = new WorkItem(
            Guid.NewGuid(),
            title.Trim(),
            description?.Trim(),
            type,
            priority,
            project.Id,
            initialState.Id,
            createdBy.Id,
            estimatedPoints,
            estimatedCompletionDate,
            createdOnUtc);

        workItem._changeLogs.Add(WorkItemChangeLog.Create(workItem, createdBy, WorkItemChangeType.Created, null, createdOnUtc));
        workItem.AddDomainEvent(new WorkItemCreatedDomainEvent(workItem.Id));

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
        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.Deactivated);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Fail(WorkItemErrors.TitleRequired);
        }

        if (title.Length > MaxTitleLength)
        {
            return Result.Fail(WorkItemErrors.TitleTooLong);
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

    public Result Deactivate(User changedBy, DateTime updatedOnUtc)
    {
        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.AlreadyDeactivated);
        }

        IsActive = false;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.Deactivated, null, updatedOnUtc));

        return Result.Ok();
    }

    public Result Move(FlowState toState, User changedBy, string? reason, DateTime changedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.Deactivated);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        Guid? fromStateId = FlowStateId;

        _stateHistory.Add(StateTransitionHistory.Create(
            this,
            FlowState,
            toState,
            changedBy,
            reason,
            changedOnUtc));

        FlowStateId = toState.Id;
        UpdatedOnUtc = changedOnUtc;

        if (toState.IsTerminal)
        {
            CompletedOnUtc = changedOnUtc;
        }

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.Moved, toState.Id, changedOnUtc));

        AddDomainEvent(new WorkItemMovedDomainEvent(Id, fromStateId, toState.Id));

        return Result.Ok();
    }

    public Result Assign(User assignee, User changedBy, DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.Deactivated);
        }

        if (!assignee.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (!changedBy.IsActive)
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
        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.Deactivated);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        AssigneeId = null;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.Unassigned, null, updatedOnUtc));

        AddDomainEvent(new WorkItemAssignedDomainEvent(Id, null));

        return Result.Ok();
    }

    public Result AddComment(User author, string content, DateTime createdOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.Deactivated);
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

    public Result UpdateComment(Guid commentId, string content, DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.Deactivated);
        }

        var comment = _comments.FirstOrDefault(c => c.Id == commentId && !c.IsDeleted);

        if (comment is null)
        {
            return Result.Fail(WorkItemErrors.CommentNotFound);
        }

        return comment.UpdateContent(content, updatedOnUtc);
    }

    public Result RemoveComment(Guid commentId, DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.Deactivated);
        }

        var comment = _comments.FirstOrDefault(c => c.Id == commentId && !c.IsDeleted);

        if (comment is null)
        {
            return Result.Fail(WorkItemErrors.CommentNotFound);
        }

        return comment.Remove(updatedOnUtc);
    }

    public Result LogTime(User user, decimal hours, string? description, DateTime loggedOnUtc, DateTime createdOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(WorkItemErrors.Deactivated);
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
}
