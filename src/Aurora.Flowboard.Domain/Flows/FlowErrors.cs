namespace Aurora.Flowboard.Domain.Flows;

public static class FlowErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "Flow.NotFound",
        "The flow with the specified identifier was not found");

    public static readonly BaseError NameRequired = BaseError.Validation(
        "Flow.NameRequired",
        "Flow name is required");

    public static readonly BaseError NameTooLong = BaseError.Validation(
        "Flow.NameTooLong",
        "Flow name cannot exceed 100 characters");

    public static readonly BaseError AlreadyDeactivated = BaseError.Validation(
        "Flow.AlreadyDeactivated",
        "The flow is already deactivated");

    public static readonly BaseError IsDefault = BaseError.Validation(
        "Flow.IsDefault",
        "Cannot perform this operation on a default flow");

    public static readonly BaseError MaxActiveStatesReached = BaseError.Validation(
        "Flow.MaxActiveStatesReached",
        "Cannot add more active states to the flow as it has reached the maximum limit of 10");

    public static readonly BaseError Deactivated = BaseError.Validation(
        "Flow.Deactivated",
        "Cannot perform this operation on a deactivated flow");

    public static readonly BaseError StateNameRequired = BaseError.Validation(
        "Flow.StateNameRequired",
        "Flow state name is required");

    public static readonly BaseError StateNameTooLong = BaseError.Validation(
        "Flow.StateNameTooLong",
        "Flow state name cannot exceed 100 characters");

    public static readonly BaseError DuplicateStateName = BaseError.Conflict(
        "Flow.DuplicateStateName",
        "A state with this name already exists in the flow");

    public static readonly BaseError StateNotFound = BaseError.NotFound(
        "Flow.StateNotFound",
        "The flow state with the specified identifier was not found");

    public static readonly BaseError TransitionAlreadyExists = BaseError.Conflict(
        "Flow.TransitionAlreadyExists",
        "A transition between these states already exists in the flow");

    public static readonly BaseError TransitionNotFound = BaseError.NotFound(
        "Flow.TransitionNotFound",
        "The flow transition with the specified identifier was not found");

    public static readonly BaseError TransitionRoleAlreadyAllowed = BaseError.Conflict(
        "Flow.TransitionRoleAlreadyAllowed",
        "The role is already allowed for this transition");

    public static readonly BaseError TransitionFromStateNotFound = BaseError.Validation(
        "Flow.TransitionFromStateNotFound",
        "The source state does not belong to this flow");

    public static readonly BaseError TransitionToStateNotFound = BaseError.Validation(
        "Flow.TransitionToStateNotFound",
        "The destination state does not belong to this flow");

    public static readonly BaseError LastCompletedState = BaseError.Validation(
        "Flow.LastCompletedState",
        "Cannot remove the last completed state from the flow");

    public static readonly BaseError LastCancelledState = BaseError.Validation(
        "Flow.LastCancelledState",
        "Cannot remove the last cancelled state from the flow");
}
