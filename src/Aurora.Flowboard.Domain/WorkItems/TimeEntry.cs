using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.WorkItems;

public sealed class TimeEntry
{
    public const int MaxDescriptionLength = 500;

    public Guid Id { get; private set; }
    public Guid WorkItemId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Hours { get; private set; }
    public string? Description { get; private set; }
    public DateTime LoggedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private TimeEntry() { } // EF Core

    private TimeEntry(
        Guid id,
        Guid workItemId,
        Guid userId,
        decimal hours,
        string? description,
        DateTime loggedOnUtc,
        DateTime createdOnUtc)
    {
        Id = id;
        WorkItemId = workItemId;
        UserId = userId;
        Hours = hours;
        Description = description;
        LoggedOnUtc = loggedOnUtc;
        CreatedOnUtc = createdOnUtc;
    }

    internal static Result<TimeEntry> Create(
        WorkItem workItem,
        User user,
        decimal hours,
        string? description,
        DateTime loggedOnUtc,
        DateTime createdOnUtc)
    {
        if (hours <= 0)
        {
            return Result.Fail<TimeEntry>(WorkItemErrors.TimeEntryHoursInvalid);
        }

        return new TimeEntry(
            Guid.NewGuid(), 
            workItem.Id, 
            user.Id, 
            hours, 
            description?.Trim(), 
            loggedOnUtc, 
            createdOnUtc);
    }
}
