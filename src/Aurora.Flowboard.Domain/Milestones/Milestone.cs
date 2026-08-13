using Aurora.Flowboard.Domain.Milestones.Events;
using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.Milestones;

public sealed class Milestone : BaseEntity
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    private static readonly Dictionary<MilestoneStatus, MilestoneStatus[]> Transitions = new()
    {
        [MilestoneStatus.Draft] = [MilestoneStatus.Active, MilestoneStatus.Archived],
        [MilestoneStatus.Active] = [MilestoneStatus.OnHold, MilestoneStatus.Completed, MilestoneStatus.Archived],
        [MilestoneStatus.OnHold] = [MilestoneStatus.Active, MilestoneStatus.Archived],
        [MilestoneStatus.Completed] = [MilestoneStatus.Archived],
        [MilestoneStatus.Archived] = []
    };

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public MilestoneStatus Status { get; private set; }
    public DateOnly? TargetStartDate { get; private set; }
    public DateOnly? TargetEndDate { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public Project Project { get; init; } = null!;

    private bool IsModifiable => Status is MilestoneStatus.Draft or MilestoneStatus.Active or MilestoneStatus.OnHold;

    private Milestone() : base(Guid.Empty) { } // EF Core

    private Milestone(
        Guid id,
        Guid projectId,
        string name,
        string? description,
        DateOnly? targetStartDate,
        DateOnly? targetEndDate,
        Guid createdBy,
        DateTime createdOnUtc) : base(id)
    {
        ProjectId = projectId;
        Name = name;
        Description = description;
        Status = MilestoneStatus.Draft;
        TargetStartDate = targetStartDate;
        TargetEndDate = targetEndDate;
        CreatedBy = createdBy;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<Milestone> Create(
        string name,
        string? description,
        DateOnly? targetStartDate,
        DateOnly? targetEndDate,
        Project project,
        User createdBy,
        DateTime createdOnUtc)
    {
        if (!project.IsAdmin(createdBy.Id))
        {
            return Result.Fail<Milestone>(MilestoneErrors.OnlyAdminCanManageMilestone);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<Milestone>(MilestoneErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<Milestone>(MilestoneErrors.NameTooLong);
        }

        if (project.Milestones.Any(m => string.Equals(m.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail<Milestone>(MilestoneErrors.DuplicateName);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail<Milestone>(MilestoneErrors.DescriptionTooLong);
        }

        if (targetStartDate is not null && targetEndDate is not null && targetEndDate < targetStartDate)
        {
            return Result.Fail<Milestone>(MilestoneErrors.InvalidDateRange);
        }

        var milestone = new Milestone(
            Guid.NewGuid(),
            project.Id,
            name.Trim(),
            description?.Trim(),
            targetStartDate,
            targetEndDate,
            createdBy.Id,
            createdOnUtc)
        {
            Project = project
        };

        project.RegisterMilestone(milestone);

        milestone.AddDomainEvent(new MilestoneCreatedDomainEvent(milestone.Id, milestone.ProjectId));

        return milestone;
    }

    public Result Update(
        string name,
        string? description,
        DateOnly? targetStartDate,
        DateOnly? targetEndDate,
        User changedBy,
        DateTime updatedOnUtc)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(MilestoneErrors.OnlyAdminCanManageMilestone);
        }

        if (!IsModifiable)
        {
            return Result.Fail(MilestoneErrors.OperationNotAllowedInCurrentStatus);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(MilestoneErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail(MilestoneErrors.NameTooLong);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail(MilestoneErrors.DescriptionTooLong);
        }

        if (targetStartDate is not null && targetEndDate is not null && targetEndDate < targetStartDate)
        {
            return Result.Fail(MilestoneErrors.InvalidDateRange);
        }

        Name = name.Trim();
        Description = description?.Trim();
        TargetStartDate = targetStartDate;
        TargetEndDate = targetEndDate;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new MilestoneUpdatedDomainEvent(Id));

        return Result.Ok();
    }

    public Result ChangeStatus(MilestoneStatus newStatus, User changedBy, int openWorkItemCount, DateTime updatedOnUtc)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(MilestoneErrors.OnlyAdminCanManageMilestone);
        }

        if (!IsValidTransition(Status, newStatus))
        {
            return Result.Fail(MilestoneErrors.InvalidStatusTransition);
        }

        if (newStatus is MilestoneStatus.Completed or MilestoneStatus.Archived && openWorkItemCount > 0)
        {
            return Result.Fail(MilestoneErrors.CannotCloseWithOpenWorkItems);
        }

        MilestoneStatus oldStatus = Status;
        Status = newStatus;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new MilestoneStatusChangedDomainEvent(Id, oldStatus, newStatus));

        return Result.Ok();
    }

    private static bool IsValidTransition(MilestoneStatus from, MilestoneStatus to) =>
        Transitions.TryGetValue(from, out MilestoneStatus[]? targets) && targets.Contains(to);
}
