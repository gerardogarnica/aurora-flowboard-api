---
paths:
  - "src/**/Dashboard/**/*.cs"
  - "src/Aurora.Flowboard.Application/Projects/Shared/MyProjectsQueryExtensions.cs"
  - "src/Aurora.Flowboard.Application/Users/GetMySummary/**/*.cs"
  - "test/**/Dashboard/**/*.cs"
---

# Dashboard

Spec: `docs/specs/2026-11-07-dashboard-design.md` (definitions in section 2).

## One project set: my active projects

`Projects/Shared/MyProjectsQueryExtensions.cs` defines it; don't write the filter again in a handler.

**My active projects** (`WhereMyActiveProjects`, `AcceptsOpenWork`): I am a member and the status is `Active` or `Maintenance`. It feeds everything on these screens:

- `my-summary`: the Sidebar list, `counts.projects`, `counts.members` and `myOpenIssues`.
- `GET dashboard`: every number (`me`, `team`, including `closedLast7Days`, `aging`, `milestones`) and the portfolio rows with their members.
- `GET work-items`.

Rules:

- Why: these are the projects that accept work items (`Project.IsModifiable`). A `Completed` project's items cannot be moved, and the PRD §5.1 rule that forbids completing a project with open items is not enforced yet. A `Completed` or `Archived` project is reached through `GET projects`, not the Sidebar.
- There used to be a second set, `WhereMyProjects` (`Status != Archived`), for the Sidebar and portfolio. It was removed on 2026-11-09 by product decision (spec, decision 1). Don't reintroduce it for a new list.
- A new list of projects uses this set and `OrderLikeSidebar` (status, then name, `OrdinalIgnoreCase`), so it matches the Sidebar.
- `MyProjectsQueryExtensionsTests` asserts that `AcceptsOpenWork`, `WhereMyActiveProjects` and `Project.IsModifiable` agree for every `ProjectStatus`.
- `me.open` must always equal `myOpenIssues`; `GetDashboardHandlerTests` asserts it over the same data.

## One aggregate query

- `GetDashboardHandler` makes 8 reads. Every open-work counter comes from one `GROUP BY` over the open items of my active projects, keyed by state, milestone, mine, unassigned, critical/high and age band. Don't split it into one count per metric (the numbers must add up: buckets 7-13 + 14+ == `team.stalled`) and don't load work items into memory (the cost must depend on the combinations, not on the number of items).
- The age band is a ternary chain over cutoffs (`now - 4/7/14` days) inside the `GroupBy`: no `switch` in expression trees, no day arithmetic in SQL, one `now` per request.
- `DashboardCalculator` is the pure part: every rule that is not a SQL filter lives there and is tested without a database. The handler only reads. It filters `Completed` projects out again, from the numbers and from the portfolio, so the rule holds whatever rows it is given.
- `closedLast7Days` filters by `Completed` category, because `Move` also sets `CompletedOnUtc` when cancelling and nothing clears it on reopening.
- **Consistency limit:** the milestone closed counts, `closedLast7Days` and `oldestInFlight` are separate reads, so a concurrent move can make them disagree with the `GROUP BY` for one load; the next load fixes it. Accepted: no read transaction and no cache. A future cache must respect the time budgets and stampede protection in `CLAUDE.md`.
- Unit tests run on LINQ-to-Objects (`MockDbSetHelper`): the `GROUP BY` translation, the ternary and `Contains` over `Priority` (a string column) are checked against Postgres by reading the SQL logs.
