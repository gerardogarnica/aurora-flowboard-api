namespace Aurora.Flowboard.Application.Abstractions.Data;

public interface IApplicationDbContext : IAsyncDisposable
{
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<ProjectChangeLog> ProjectChangeLogs { get; }
    DbSet<Flow> Flows { get; }
    DbSet<FlowState> FlowStates { get; }
    DbSet<FlowTransition> FlowTransitions { get; }
    DbSet<WorkItem> WorkItems { get; }
    DbSet<Comment> Comments { get; }
    DbSet<TimeEntry> TimeEntries { get; }
    DbSet<WorkItemChangeLog> WorkItemChangeLogs { get; }
    DbSet<StateTransitionHistory> StateTransitionHistories { get; }

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
