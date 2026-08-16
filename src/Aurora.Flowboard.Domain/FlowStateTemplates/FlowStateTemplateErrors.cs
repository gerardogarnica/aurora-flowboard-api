namespace Aurora.Flowboard.Domain.FlowStateTemplates;

public static class FlowStateTemplateErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "FlowStateTemplate.NotFound",
        "The flow state template with the specified identifier was not found");

    public static readonly BaseError DuplicateForKind = BaseError.Conflict(
        "FlowStateTemplate.DuplicateForKind",
        "A flow state template already exists for this project kind");

    public static readonly BaseError StateNameRequired = BaseError.Validation(
        "FlowStateTemplate.StateNameRequired",
        "Template flow state name is required");

    public static readonly BaseError StateNameTooLong = BaseError.Validation(
        "FlowStateTemplate.StateNameTooLong",
        "Template flow state name cannot exceed 50 characters");

    public static readonly BaseError DuplicateStateName = BaseError.Conflict(
        "FlowStateTemplate.DuplicateStateName",
        "A template flow state with this name already exists in this template");

    public static readonly BaseError StateNotFound = BaseError.NotFound(
        "FlowStateTemplate.StateNotFound",
        "The template flow state with the specified identifier was not found");

    public static readonly BaseError MaxActiveStatesReached = BaseError.Validation(
        "FlowStateTemplate.MaxActiveStatesReached",
        "Cannot add more active states to the template as it has reached the maximum limit of 10");

    public static readonly BaseError InvalidReorderSet = BaseError.Validation(
        "FlowStateTemplate.InvalidReorderSet",
        "The reorder set must contain exactly the template's current active states");
}
