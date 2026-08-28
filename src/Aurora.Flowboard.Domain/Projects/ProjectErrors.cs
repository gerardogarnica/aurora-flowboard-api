namespace Aurora.Flowboard.Domain.Projects;

public static class ProjectErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "Project.NotFound",
        "The project with the specified identifier was not found");

    public static readonly BaseError NameRequired = BaseError.Validation(
        "Project.NameRequired",
        "Project name is required");

    public static readonly BaseError NameTooLong = BaseError.Validation(
        "Project.NameTooLong",
        "Project name cannot exceed 100 characters");

    public static readonly BaseError DescriptionTooLong = BaseError.Validation(
        "Project.DescriptionTooLong",
        "Project description cannot exceed 500 characters");

    public static readonly BaseError InvalidStatusTransition = BaseError.Validation(
        "Project.InvalidStatusTransition",
        "The requested status transition is not allowed");

    public static readonly BaseError OperationNotAllowedInCurrentStatus = BaseError.Validation(
        "Project.OperationNotAllowedInCurrentStatus",
        "This operation is not allowed in the project's current status");

    public static readonly BaseError MemberAlreadyExists = BaseError.Conflict(
        "Project.MemberAlreadyExists",
        "The user is already a member of this project");

    public static readonly BaseError MemberNotFound = BaseError.NotFound(
        "Project.MemberNotFound",
        "The user is not a member of this project");

    public static readonly BaseError CannotRemoveLastAdmin = BaseError.Validation(
        "Project.CannotRemoveLastAdmin",
        "Cannot remove the only admin member of the project");

    public static readonly BaseError OnlyAdminCanRemoveMembers = BaseError.Forbidden(
        "Project.OnlyAdminCanRemoveMembers",
        "Only admin members can remove other members from the project");

    public static readonly BaseError OnlyAdminCanAddMembers = BaseError.Forbidden(
        "Project.OnlyAdminCanAddMembers",
        "Only admin members can add members to the project");

    public static readonly BaseError OnlyAdminCanUpdateProject = BaseError.Forbidden(
        "Project.OnlyAdminCanUpdateProject",
        "Only admin members can update the project");

    public static readonly BaseError OnlyAdminCanChangeStatus = BaseError.Forbidden(
        "Project.OnlyAdminCanChangeStatus",
        "Only admin members can change the project status");

    public static readonly BaseError PrefixRequired = BaseError.Validation(
        "Project.PrefixRequired",
        "Project prefix is required");

    public static readonly BaseError PrefixTooLong = BaseError.Validation(
        "Project.PrefixTooLong",
        "Project prefix cannot exceed 3 characters");

    public static readonly BaseError PrefixInvalidCharacters = BaseError.Validation(
        "Project.PrefixInvalidCharacters",
        "Project prefix must contain only alphabetic characters");

    public static readonly BaseError PrefixAlreadyExists = BaseError.Conflict(
        "Project.PrefixAlreadyExists",
        "A project with the specified prefix already exists");

    public static readonly BaseError KindUnchanged = BaseError.Validation(
        "Project.KindUnchanged",
        "The project already has the specified kind");

    public static readonly BaseError OnlyAdminCanChangeKind = BaseError.Forbidden(
        "Project.OnlyAdminCanChangeKind",
        "Only admin members can change the project kind");

    public static readonly BaseError OnlyAdminCanModifyFlow = BaseError.Forbidden(
        "Project.OnlyAdminCanModifyFlow",
        "Only project administrators can modify the flow");

    public static readonly BaseError FlowStateNameRequired = BaseError.Validation(
        "Project.FlowStateNameRequired",
        "Flow state name is required");

    public static readonly BaseError FlowStateNameTooLong = BaseError.Validation(
        "Project.FlowStateNameTooLong",
        "Flow state name cannot exceed 50 characters");

    public static readonly BaseError DuplicateFlowStateName = BaseError.Conflict(
        "Project.DuplicateFlowStateName",
        "A flow state with this name already exists in the project");

    public static readonly BaseError FlowStateNotFound = BaseError.NotFound(
        "Project.FlowStateNotFound",
        "The flow state with the specified identifier was not found");

    public static readonly BaseError MaxActiveFlowStatesReached = BaseError.Validation(
        "Project.MaxActiveFlowStatesReached",
        "Cannot add more active flow states to the project as it has reached the maximum limit of 10");

    public static readonly BaseError LastCompletedFlowState = BaseError.Validation(
        "Project.LastCompletedFlowState",
        "Cannot remove the last completed flow state from the project");

    public static readonly BaseError LastCancelledFlowState = BaseError.Validation(
        "Project.LastCancelledFlowState",
        "Cannot remove the last cancelled flow state from the project");

    public static readonly BaseError NoInitialFlowState = BaseError.Validation(
        "Project.NoInitialFlowState",
        "The project has no active flow state that can be used as the initial state");

    public static readonly BaseError FlowTransitionAlreadyExists = BaseError.Conflict(
        "Project.FlowTransitionAlreadyExists",
        "A transition between these flow states already exists in the project");

    public static readonly BaseError FlowTransitionNotFound = BaseError.NotFound(
        "Project.FlowTransitionNotFound",
        "The flow transition with the specified identifier was not found");

    public static readonly BaseError FlowTransitionRoleAlreadyAllowed = BaseError.Conflict(
        "Project.FlowTransitionRoleAlreadyAllowed",
        "The role is already allowed for this flow transition");

    public static readonly BaseError FlowTransitionRoleNotAllowed = BaseError.Validation(
        "Project.FlowTransitionRoleNotAllowed",
        "The role is not allowed for this flow transition");

    public static readonly BaseError FlowTransitionFromStateNotFound = BaseError.Validation(
        "Project.FlowTransitionFromStateNotFound",
        "The source flow state does not belong to this project");

    public static readonly BaseError FlowTransitionToStateNotFound = BaseError.Validation(
        "Project.FlowTransitionToStateNotFound",
        "The destination flow state does not belong to this project");

    public static readonly BaseError InitialFlowStatesMissingActiveState = BaseError.Validation(
        "Project.InitialFlowStatesMissingActiveState",
        "The initial flow states must include at least one state with the Active category");

    public static readonly BaseError InitialFlowStatesMissingCompletedState = BaseError.Validation(
        "Project.InitialFlowStatesMissingCompletedState",
        "The initial flow states must include at least one state with the Completed category");

    public static readonly BaseError InitialFlowStatesMissingCancelledState = BaseError.Validation(
        "Project.InitialFlowStatesMissingCancelledState",
        "The initial flow states must include at least one state with the Cancelled category");
}
