namespace Aurora.Flowboard.Application.WorkItems.GetByCode;

public sealed record GetWorkItemByCodeQuery(string Code) : IQuery<WorkItemResponse>;
