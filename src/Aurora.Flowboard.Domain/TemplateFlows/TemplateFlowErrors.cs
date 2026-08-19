namespace Aurora.Flowboard.Domain.TemplateFlows;

public static class TemplateFlowErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "TemplateFlow.NotFound",
        "The flow state template with the specified identifier was not found");

    public static readonly BaseError DuplicateForKind = BaseError.Conflict(
        "TemplateFlow.DuplicateForKind",
        "A flow state template already exists for this project kind");

    public static readonly BaseError StateNameRequired = BaseError.Validation(
        "TemplateFlow.StateNameRequired",
        "Template flow state name is required");

    public static readonly BaseError StateNameTooLong = BaseError.Validation(
        "TemplateFlow.StateNameTooLong",
        "Template flow state name cannot exceed 50 characters");

    public static readonly BaseError DuplicateStateName = BaseError.Conflict(
        "TemplateFlow.DuplicateStateName",
        "A template flow state with this name already exists in this template");

    public static readonly BaseError StateNotFound = BaseError.NotFound(
        "TemplateFlow.StateNotFound",
        "The template flow state with the specified identifier was not found");

    public static readonly BaseError MaxActiveStatesReached = BaseError.Validation(
        "TemplateFlow.MaxActiveStatesReached",
        "Cannot add more active states to the template as it has reached the maximum limit of 10");

    public static readonly BaseError InvalidReorderSet = BaseError.Validation(
        "TemplateFlow.InvalidReorderSet",
        "The reorder set must contain exactly the template's current active states");
}
