namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

// Values accepted by the GET work-items query string, case-sensitive: they are the values the API
// serializes. GetWorkItemsValidator rejects anything else, so the handler parses without checking again.
internal static class WorkItemSearchFilters
{
    public const string MeValue = "me";
    public const string TrueValue = "true";
    public const string DefaultSort = "stageThenTimeInState";

    private const char PrioritySeparator = ',';

    public static bool IsStage(string value) =>
        Enum.GetNames<FlowStateStage>().Contains(value, StringComparer.Ordinal);

    public static FlowStateStage ParseStage(string value) =>
        Enum.Parse<FlowStateStage>(value);

    // Matched against the names instead of Enum.TryParse, which also accepts numbers ("2").
    public static bool IsPriorityList(string value) =>
        SplitPriorities(value).All(name => Enum.GetNames<Priority>().Contains(name, StringComparer.Ordinal));

    public static List<Priority> ParsePriorities(string value) =>
        [.. SplitPriorities(value).Select(Enum.Parse<Priority>).Distinct()];

    private static string[] SplitPriorities(string value) =>
        value.Split(PrioritySeparator, StringSplitOptions.TrimEntries);
}
