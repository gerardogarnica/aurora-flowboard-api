namespace Aurora.Flowboard.Application.Abstractions.Data;

public interface IApplicationDbContext : IAsyncDisposable
{
    DbSet<User> Users { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<ProjectChangeLog> ProjectChangeLogs { get; }
    DbSet<Component> Components { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<FlowState> FlowStates { get; }
    DbSet<FlowTransition> FlowTransitions { get; }
    DbSet<TemplateFlow> TemplateFlows { get; }
    DbSet<TemplateFlowState> TemplateFlowStates { get; }
    DbSet<WorkItem> WorkItems { get; }
    DbSet<Comment> Comments { get; }
    DbSet<TimeEntry> TimeEntries { get; }
    DbSet<WorkItemChangeLog> WorkItemChangeLogs { get; }
    DbSet<WorkItemTag> WorkItemTags { get; }
    DbSet<StateTransitionHistory> StateTransitionHistories { get; }

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
