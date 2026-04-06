namespace Aurora.Flowboard.Application.WorkItems.GetById;

public sealed record GetWorkItemByIdQuery(Guid WorkItemId) : IQuery<WorkItemResponse>;
