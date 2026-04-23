using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Users;
using Aurora.Flowboard.Domain.WorkItems;
using Microsoft.EntityFrameworkCore.Storage;

namespace Aurora.Flowboard.Infrastructure.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    internal const string DefaultSchema = "flowboard";

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
    public DbSet<ProjectChangeLog> ProjectChangeLogs { get; set; } = null!;
    public DbSet<Flow> Flows { get; set; } = null!;
    public DbSet<FlowState> FlowStates { get; set; } = null!;
    public DbSet<FlowTransition> FlowTransitions { get; set; } = null!;
    public DbSet<WorkItem> WorkItems { get; set; } = null!;
    public DbSet<WorkItemTag> WorkItemTags { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<TimeEntry> TimeEntries { get; set; } = null!;
    public DbSet<WorkItemChangeLog> WorkItemChangeLogs { get; set; } = null!;
    public DbSet<StateTransitionHistory> StateTransitionHistories { get; set; } = null!;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
