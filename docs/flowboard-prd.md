# Flowboard — Product Requirements Document

**Version:** 1.0

**Status:** Draft

> **Terminology note.** In this document, *Project* always refers to the **domain entity** (a long-lived software project managed inside Flowboard), never to Flowboard itself. Flowboard is referred to as "Flowboard" or "the system".

---

## 1. Overview

**System Name:** Flowboard

**System Type:** Internal software project management platform (API + Web UI)

**Primary Users:**

* Product Owners
* Functional Analysts
* Developers
* QA (Quality Assurance)
* Team Administrators

**Team shape:**
Flowboard serves a **small team**: an owner who is also the primary developer, plus a handful of collaborators — a QA analyst, and developers who may be permanent or engaged temporarily for a single project. Membership is heterogeneous and **scoped**: a collaborator brought in for one project must not see the others. Some projects are engagements for external clients, which makes that isolation a contractual requirement rather than a preference. This is the premise behind §5.4; without it, most of the access-control machinery would be ceremony.

**Objective:**
Provide a robust platform to manage software development work through structured workflows, enabling collaboration, traceability, and efficient execution across both long-lived projects and time-boxed milestones.

**Scope boundary:**
Flowboard is an **internal tool** serving a single organization. There is **no** `Client` / `Organization` / tenant level in the domain model — client work is represented by a `Project` with `ProjectKind = Client`, not by a tenant. `Project` is the root entity of the work hierarchy. If multi-tenancy is required in the future, it must be introduced as a deliberate architectural change (see §14).

---

## 2. Problem Statement

Development teams need tools to:

* Manage tasks and requirements
* Coordinate work across different roles
* Track progress visibility
* Maintain change traceability
* **Manage continuous, open-ended work** (maintenance, defects, technical debt) alongside **time-boxed initiatives** with a defined start, scope, and end

The last point is the structural gap this model closes, and the cost of leaving it open is **coordination**. With one person, what to push on next lives in the owner's head. With three, it has to live in the tool: the team needs a shared, visible answer to *what are we working toward right now*. The same gap appears whenever a developer is engaged for a defined chunk of work — that engagement has a start, an end, and a scope, and there is no entity to represent it.

A model with only `Project` forces the team to either spin up a whole new project per initiative — fragmenting the backlog, the code prefix, the membership list, and the workflow — or track initiatives outside Flowboard entirely. The first corrupts cycle-time metrics; the second defeats the purpose of the board.

Existing solutions (Jira, Trello, Notion) can be:

* Expensive
* Oversized
* Poorly adapted to internal needs

**Flowboard aims to fill this gap with an internal, flexible, and extensible solution.**

---

## 3. Goals & Objectives

### 3.1 Goals

* Centralize software work management across projects and milestones
* Standardize workflows while allowing per-project variation
* Improve collaboration across a small, partly temporary team
* Enable full traceability of the work item lifecycle
* Support both continuous project work and time-boxed milestone work in a single model

### 3.2 Non-Goals (MVP)

* No complex external integrations (Git, SSO, chat platforms)
* No advanced analytics (velocity, burndown, forecasting)
* No WorkItem nesting (no Epic → Story → Subtask hierarchy)
* No grouping level above `Project` (no cross-project initiatives — see §4.1)
* No multi-tenancy / client isolation at the infrastructure level
* No real-time collaboration (websockets, live cursors)

---

## 4. Glossary (Ubiquitous Language)

| Term | Definition | Lifetime |
|---|---|---|
| **Project** | A long-lived body of software work owned by the organization (e.g. "Coinly", "Client X — Billing Revamp"). Owns the backlog, the code prefix, workflows, boards, and membership. The unit at which people are granted access. | Permanent until archived |
| **ProjectKind** | Classification of a project: `Product`, `Client`, `Research`, `Internal`. Determines seeded flow templates and personal-board filtering. Never affects codes or authorization. | Configuration |
| **Milestone** | A time-boxed initiative within a single project, with a defined scope and end (e.g. "Mobile app v1", "Phase 1 delivery"). Groups work items for planning and reporting. Never a security boundary. | Temporal |
| **Component** | A durable functional area or surface within a project (e.g. `admin`, `portal`, `api`, `billing`). Answers *where in the project does this work live*. Controlled vocabulary, defined per project. Never a security boundary and never part of the work item code. | Permanent until retired |
| **WorkItem** | The atomic unit of work. Always belongs to exactly one project; optionally belongs to one milestone and one component. Flat — work items do not contain other work items. | Until Done/Archived |
| **WorkItemType** | Classification of a work item: Story, Bug, TechnicalTask, Investigation. Used for filtering and metric segmentation. Does **not** affect which flow applies. | Configuration |
| **Flow** | An ordered set of states and allowed transitions, defined **per project**. Every work item in the project moves through the same flow, whatever its type. | Configuration |
| **FlowState** | A single state within a flow (e.g. Backlog, In Progress, Code Review, QA, Done). | Configuration |
| **Board** | A Kanban visualization of work items grouped by flow state. | View |
| **Tag** | A free-form cross-cutting keyword applied to a work item (e.g. `technical-debt`, `security`). Crosses project and milestone boundaries. | Optional |
| **Membership** | The association of a user to a project with a role. The only mechanism that grants visibility. | Permanent until revoked |

**The three axes.** These are independent and must not be collapsed into one another:

| Axis | Entity | Question it answers |
|---|---|---|
| Structure | `Component` | *Where* in the project does this live? |
| Effort | `Milestone` | *Why* is this being done now? |
| State | `FlowState` | *How far along* is it? |

A work item has one position on each axis, independently. Component is durable; milestone is temporal; state is transient. `ProjectKind` sits outside the three — it classifies the container, not the work.

**Explicitly not in the language (MVP):** Epic, Sprint, Release, Initiative, Portfolio, Tenant, Workspace. These terms must not appear in code, API, or UI until formally introduced. `Product` and `Client` are valid **only** as `ProjectKind` values, never as entities.

---

### 4.1 Cross-Project Initiatives (Out of Scope)

`Milestone.ProjectId` is required and immutable, so a milestone cannot span projects. This constraint is deliberate: a multi-project milestone would simultaneously break code prefix allocation, flow resolution, and membership.

The case it genuinely excludes is a shared initiative across separately owned projects (e.g. a compliance effort touching three projects). The answer is **not** to relax the constraint. The correct answer is a grouping level **above** `Project` — Linear's Initiative, Jira's Advanced Roadmaps, Asana's Portfolio. This is deferred (§15). Until it exists, cross-project initiatives are represented as one milestone per project, correlated by a shared naming convention. A shared tag may be applied as a human-readable marker, but tags are not a query axis in the MVP (§5.5), so the correlation is manual.

**Do not use components for this.** Components live inside a project and are scoped to it; two projects may both define a component named `billing`, and they are unrelated entities. Components never span projects.

**A note on boundaries.** Multiple frontends, repositories, or services do not imply multiple projects. Coinly with an API, a web app, and a mobile app is **one** project with three components — not three projects. Split only when ownership genuinely diverges: separate roadmaps decided by different people, or independent release cycles.

---

## 5. Core Features (MVP Scope)

### 5.1 Project Management

* Create and edit projects
* Configure project metadata: name, key/prefix, description, kind, status
* Archive projects (no hard delete)
* Manage project membership (associate users with roles)
* Project kinds: `Product`, `Client`, `Research`, `Internal`
* Project statuses: `Active`, `Maintenance`, `Completed`, `Archived`

**Acceptance criteria:**

* A project key is 1–3 uppercase letters, unique across the system, and **immutable after creation**.
* Every project has a `ProjectKind`, set at creation. It **may be changed by an `Admin`**, and the change is recorded in the project change log. Changing it does not alter the project's existing flow, work item codes, or membership — the `Research` → `Product` transition ("I evaluated the stack, now I'm building it") is expected, and forcing a new project would fragment the code sequence.
* Distinguishing `Product` from `Internal`: a `Product` has, or will have, users beyond the team (Coinly, Flowboard). An `Internal` project is tooling the team uses and never distributes (scripts, automation, infrastructure).
* `ProjectKind` determines which flow templates are provisioned at creation (§5.7) and is a first-class filter on the personal board (§5.8). It never affects the work item code or authorization.
* Typical status paths: `Product` and `Internal` run `Active → Maintenance → Archived`; `Client` and `Research` run `Active → Completed → Archived`. The status set is deliberately kind-neutral rather than four separate enums.
* Creating a project provisions its flow, a default board, and creates the requester as `Admin` member.
* A project cannot be moved to `Completed` or `Archived` while it holds **open** work items — items whose flow state is not terminal, the same definition used in §5.2. They must first be finished, cancelled, or archived.
* Archiving a project hides it from all default views but preserves all work items and their codes.

---

### 5.2 Milestone Management

* Create and edit milestones **within a project** (`ProjectId` is required and immutable)
* Configure basic milestone information: name, description, target dates
* Archive milestones (no hard delete)
* Milestone statuses: `Draft`, `Active`, `OnHold`, `Completed`, `Archived`

**Acceptance criteria:**

* A milestone always belongs to exactly one project and cannot be moved between projects.
* A milestone cannot be moved to `Completed` or `Archived` while it holds **open** work items — items whose flow state is not terminal (§5.7). They must first be finished, cancelled, or removed from the milestone (§5.5).
* Closed work items **stay assigned** to the milestone once it is completed or archived. The association is what makes the milestone reportable afterwards — "what shipped in Phase 1" must remain answerable.
* A `Completed` or `Archived` milestone accepts no new work item assignments.
* Archiving a milestone hides it from default views and from milestone selection on new items, but preserves its work items and their codes.
* Milestone names are unique within a project, not globally.
* A milestone does not grant or restrict access to anything (see §5.4).

---

### 5.3 Component Management

Components are the durable structural subdivision of a project — its functional areas or delivery surfaces (`admin`, `portal`, `api`, `billing`). They answer *where in the project does this work live*, independently of *why* (milestone) and *how far along* (state).

* Create, rename, and retire components within a project
* Assign a component to a work item
* Filter and group boards by component

**Acceptance criteria:**

* A component belongs to exactly one project and cannot be moved between projects. Component names are unique within a project, not globally.
* `WorkItem.ComponentId` is **nullable**. Work with no clear home is valid and appears under a "no component" filter.
* A work item has **at most one** component. Work spanning two surfaces is split into separate work items: an authentication change affecting both the API and the web app becomes two items, one per component, not one item counted twice.
* A component cannot be `Retired` while it holds **open** work items — items whose flow state is not terminal (§5.7). They must first be finished, cancelled, or removed from the component (§5.5).
* Components are **not** part of the work item code. The code prefix comes from the project alone (`CNL-412`, never `CNL-API-412`).
* Components are **not** a security boundary. Authorization remains project-level (§5.4).
* Components do **not** have their own flows. Every work item in a project moves through the project's single flow (§5.7).
* A component may be assigned to a work item, changed, or removed at any time. None of these operations changes the item's code.
* A component in use by at least one work item cannot be deleted; it is **retired** — hidden from selection on new items while remaining visible on existing ones and in historical reporting.
* Components are managed by project `Admin`s. Non-admins select from the existing set and cannot create new ones.

**Rationale for single-valued.** Jira allows multiple components per issue; Azure DevOps area paths are single-valued. Single was chosen because it keeps per-component metrics meaningful (an item counted in two components inflates both) and because it pushes toward splitting cross-surface work, which is desirable independently. The decision is also the reversible direction: moving from one-to-many to many-to-many later is an additive migration, while the reverse would require discarding data.

---

### 5.4 User, Role & Access Management

* User management (create, deactivate)
* Global roles: `Administrator`, `Member`
* Project-level roles: `Admin`, `Analyst`, `Developer`, `QA`, `Viewer`
* Role-based access control (RBAC) evaluated **at the project level only**

**Acceptance criteria:**

* A user's permissions on a work item are derived from their membership in the item's **project**. The item's milestone is irrelevant to authorization.
* A user with no membership in a project cannot see the project, its milestones, its work items, or its board. **This is the mechanism that makes temporary collaborators safe**: a developer engaged for one client's project must not be able to see other clients' projects or the team's own products.
* `Viewer` is read-only and exists for a specific case: **granting an external client visibility of their own project board** without any ability to modify it.
* Every user has exactly one **global role**. `Administrator` performs system-wide operations — creating and deactivating users, and administering any project — and can see all projects without being a member of any. `Member` is the default: it grants nothing on its own, and all of a `Member`'s visibility comes from project membership.
* The isolation guarantee above applies to `Member`s. An `Administrator` sees everything by design; that is the owner's role, not a role given to a temporary collaborator or a client.
* **`Administrator` (global) and `Admin` (project-level) are different roles and must not be conflated.** A user can be a project `Admin` on one project while holding the global role `Member` — that is the normal case for a trusted collaborator who runs one project but must not see the others.
* A project must always retain at least one `Admin`.
* Membership revocation is immediate and audited. Membership has no expiry date in the MVP — offboarding a temporary collaborator is a manual action (§16).

**Rationale:** milestone-level permissions were considered and rejected. They multiply the authorization surface (an item can move between milestones, changing who can see it) with no corresponding benefit — access is granted per engagement, and an engagement maps to a project.

---

### 5.5 Work Item Management

* Create and manage work items
* Types: `Story`, `Bug`, `TechnicalTask`, `Investigation`
* Assignment to users
* Priority and estimation
* Optional association to a milestone
* Optional association to a component
* Tags (cross-cutting, free-form, descriptive only)

**Acceptance criteria:**

* `ProjectId` is **required** and immutable after creation.
* `MilestoneId` is **nullable**. A work item with no milestone is valid and is a first-class case, not an exception — it represents continuous project work (defects, maintenance, technical debt).
* A work item may be assigned to a milestone, reassigned to a different milestone **within the same project**, or removed from its milestone at any time, provided the target milestone is neither `Completed` nor `Archived` (§5.2). None of these operations changes its code.
* A work item cannot be moved to a milestone belonging to a different project.
* `ComponentId` is **nullable** and, if set, must reference a component of the item's own project (§5.3).
* Work items are **flat**: there is no parent/child relationship between work items.
* Type is immutable after creation. With a single flow per project, changing type no longer breaks the item's flow position — but it does rewrite history for cycle-time segmentation (§13), which is the reason the constraint stays.
* Tags are **descriptive annotations only**. They are not filterable, groupable, or reportable in the MVP, and boards offer no tag filter (§5.8). `Component` and `Milestone` are the structured query axes (§5.3).

---

### 5.6 Work Item Code System

Every work item receives a human-readable, permanent identifier of the form `{ProjectKey}-{Sequence}` (e.g. `CNL-101`).

**Rules:**

* The prefix is the **project** key. It is never derived from the milestone or the component.
* The sequence is a per-project counter, monotonically increasing, starting at 1.
* The full code is **persisted as a composite string** on the work item, not computed at read time.
* The code is **immutable for the lifetime of the item**, including across milestone reassignment, component reassignment, and milestone archival.
* Sequence numbers are never reused, including after item archival.

**Implementation:** assignment uses a pessimistic lock on the `Project` row (`SELECT ... FOR UPDATE`) within the creation transaction, following Jira's approach. This serializes creation per project; the contention is acceptable at the expected scale and must be revisited only if bulk import becomes a requirement.

**Rationale for project-level (not milestone-level) prefix:** items move between milestones routinely. A milestone-derived code would either break on move or silently misrepresent the item's location. The prefix must hang off the durable entity.

---

### 5.7 Workflow Management (Flow Engine)

* State definitions (`FlowState`)
* Transitions between states
* Role-based transition validations
* **One flow per project.** Every work item in the project moves through it, whatever its type
* **The seeded template depends on `ProjectKind`** at project creation

Example (`Product` / `Client` project):
`Backlog → In Progress → Code Review → QA → Done`

Example (`Research` project):
`Backlog → Investigating → Concluded`

**Acceptance criteria:**

* Each project has **exactly one** flow. Work item type plays no part in flow resolution — a bug and a story in the same project share one lifecycle.
* `ProjectKind` influences only which template is seeded at creation. Once created, the flow is edited freely by `Admin`s and is no longer tied to the kind; changing a project's kind does not alter its existing flow (§5.1).
* Where a type genuinely needs different handling, that is expressed with states the whole project shares, not with a separate flow.
* Each flow has exactly one initial state and at least one terminal state.
* A transition may declare required roles; a user lacking them receives `403` and the item does not move. This is how QA gates a `Done` transition.
* A flow state in use by at least one work item cannot be deleted.
* Flow configuration is done by project `Admin`s. There is no end-user workflow designer in the MVP.

---

### 5.8 Boards (Kanban View)

Three board types:

| Board | Scope | Purpose |
|---|---|---|
| **Project board** | All active work items in a project | Day-to-day operational view |
| **Milestone board** | Work items assigned to one milestone | Scoped initiative tracking |
| **My work** | Items assigned to the current user, across all projects | Personal prioritization |

* Columns are the flow states
* Filtering by type, assignee, priority, component (including "no component"), milestone (including "no milestone")
* **"My work" additionally filters by `ProjectKind`** — "show me all client work" is a first-class query
* Optional swimlanes grouped by component
* Ordering within a column

**Acceptance criteria:**

* Board columns are the project's flow states, in their defined order. Because a project has a single flow, every board within it has the same columns — there is no union of states to reconcile and no ambiguity in column ordering.
* The project board must offer "no milestone" and "no component" filters as first-class options.
* Component swimlanes are a view option, not a separate board type. Retired components still render if they hold work items.
* Moving a card triggers the same transition validation as the API (§5.7); a rejected move reverts the card visually and surfaces the reason.

---

### 5.9 Comments & Collaboration

* Comments on work items
* User mentions
* Conversation history

**Acceptance criteria:**

* A user may only be mentioned if they are a member of the item's project.
* Comments are editable by their author and soft-deleted, never hard-deleted.

---

### 5.10 Time Tracking (Basic)

* Initial estimation
* Worked time logging

**Acceptance criteria:**

* Time logging is available on all projects, but its weight differs by kind: on `Client` projects logged hours are **billable and therefore critical**; elsewhere they are informational.
* Logged time must be attributable to a user, a work item, and a date, so that a client engagement can be reconstructed for invoicing.

---

### 5.11 Audit & History

* Work item change log
* State transition history (with timestamp, actor, from/to state)
* Assignment changes
* Milestone reassignment history
* Membership changes (added, removed, role changed)
* **Global role changes** (with timestamp, actor, from/to role)

**Acceptance criteria:**

* State transition history must be complete and gap-free — it is the raw material for cycle-time metrics (§13) and cannot be reconstructed retroactively.
* Membership history must be complete: with external collaborators, *who had access to this project and when* is an auditable question.
* Every change to a user's global role (§5.4) is recorded with the acting user, the timestamp, and both the previous and the new role. Granting `Administrator` widens visibility to **every** project at once, making it the most privilege-escalating action in the system; it must never be inferable only from current state.
* Global role history is retained even after a user is deactivated. *Who was an `Administrator` during a given client engagement* must remain answerable after the fact.

**Implementation note.** Unlike project membership, which already writes to a project change log, the global role has no audit trail today: `Role` is a closed value type rather than a database entity, so the role lives only as a claim. Recording these changes requires a user-level change log on the `User` aggregate (§10) — it is new work, not a matter of surfacing data that already exists.

---

### 5.12 Notifications (Basic)

* Internal domain events
* Foundation for future delivery channels

---

## 6. User Stories (Examples)

* As a **product owner**, I want to see all open work on a project regardless of milestone, so I can prioritize the backlog as a whole.
* As a **developer**, I want to log a production bug without choosing a milestone, so I don't have to invent one.
* As a **developer**, I want to see my assigned tasks across all projects, to prioritize my work.
* As a **QA**, I want to validate tasks before they can be marked complete.
* As an **analyst**, I want to document requirements within a task.
* As an **analyst**, I want to move a work item into a milestone once it's been scheduled, without its identifier changing.
* As an **admin**, I want to give a temporary developer access to exactly one project, so that other clients' work stays private.
* As an **admin**, I want to give a client read-only access to their own project board.
* As an **admin**, I want to see all my client work in one view, separately from my own products.
* As an **admin**, I want a bug to follow a different lifecycle than a story.

---

## 7. Functional Requirements

* Full CRUD for all main entities, with archival replacing deletion for `Project`, `Milestone`, and `WorkItem`
* Project classification by `ProjectKind`, driving the seeded flow template and personal-board filtering
* One configurable workflow per project
* Project-scoped component vocabulary, optionally assigned to work items
* Business validations on state transitions
* Immutable work item codes, allocated per project
* Project-scoped access control as the sole visibility mechanism
* Persistence in a relational database
* RESTful API

---

## 8. Non-Functional Requirements

### 8.1 Performance

* Response < 300ms for common read operations (p95)
* Board queries must remain < 300ms with a project containing 5,000 active work items

### 8.2 Scalability

* Architecture prepared for modular growth

### 8.3 Security

* JWT-based authentication
* Role-based authorization evaluated at the project level
* No cross-project data leakage: every query scoped to the caller's memberships

### 8.4 Maintainability

* Clean Architecture
* Layer separation (Domain, Application, Infrastructure, API)

### 8.5 API Contract

* **Versioning:** URL-based, all routes grouped under `/api/v1/flowboard`. Breaking changes require a version increment.
* **Errors:** RFC 7807 `ProblemDetails` for all non-2xx responses, with a stable machine-readable `type`.
* **Pagination:** all collection endpoints paginated; cursor-based for board queries.
* **Idempotency:** transition endpoints accept an `If-Match` / expected-current-state parameter and reject stale moves with `409 Conflict`, preventing lost updates when two people move the same card.
* **Timestamps:** UTC, ISO 8601.

---

## 9. Technical Architecture

### 9.1 Stack

* .NET 10 (API)
* Entity Framework Core
* PostgreSQL
* React + Vite (Web)

### 9.2 Architecture Style

* Clean Architecture
* Modular monolith
* Domain-Driven Design (DDD)

### 9.3 Core Modules

* Projects (includes Components)
* Milestones
* WorkItems
* Flows
* Boards
* Users

---

## 10. Domain Model

```
Project (1) ──── (N) Milestone
   │                    │
   │                    │ (optional)
   ├──── (N) WorkItem ──┘
   │           │
   │           │ (optional)
   └──── (N) Component

Project (1) ──── (1) Flow ──── (N) FlowState
Project (1) ──── (N) Membership ──── (1) User
WorkItem (1) ──── (N) Comment
WorkItem (1) ──── (N) WorkItemTag
```

**Note on tags.** Tags are **not** a shared entity with a many-to-many join. Each `WorkItemTag` is a row owned by its work item, holding a normalized name. Two items tagged `security` are two independent rows, not two references to one tag. This keeps tags genuinely free-form and requires no vocabulary administration. There is deliberately no global tag registry, and consequently no tag-based filtering or reporting (§5.5) — structured querying is what `Component` and `Milestone` are for (§5.3).

### Aggregate roots

| Aggregate root | Contains | Notes |
|---|---|---|
| **Project** | Membership, Flow, FlowState | Owns code sequence allocation; locked during work item creation. Carries `ProjectKind` |
| **Milestone** | — | References Project by ID |
| **Component** | — | References Project by ID |
| **WorkItem** | Comment, TimeEntry, WorkItemTag, history entries | References Project and (optionally) Milestone and Component by ID |
| **User** | UserChangeLog | Referenced by ID from all other aggregates. Owns the audit trail of its own global role changes (§5.11) |

**Invariants:**

* `WorkItem.ProjectId` is required and immutable.
* `WorkItem.MilestoneId` is nullable; if set, the referenced milestone must belong to `WorkItem.ProjectId`.
* `WorkItem.ComponentId` is nullable; if set, the referenced component must belong to `WorkItem.ProjectId`.
* `WorkItem.Code` is assigned once at creation, never changes, and is derived from `Project` alone.
* `Milestone.ProjectId` is required and immutable.
* A milestone in `Completed` or `Archived` status holds no open work items, and accepts no new assignments. Closed items remain assigned to it permanently.
* `Component.ProjectId` is required and immutable. Component names are unique within a project.
* `WorkItemTag.WorkItemId` is required and immutable. Tag names are normalized to lowercase and are unique **within a work item**, not across the system.
* `Project.ProjectKind` is always set; it is mutable but every change is recorded in the project change log.
* Cross-aggregate references are by ID only; consistency across aggregates is achieved via domain events.

---

## 11. API Design (High-Level)

```
GET    /api/v1/flowboard/projects?kind=
POST   /api/v1/flowboard/projects
GET    /api/v1/flowboard/projects/{id}
PATCH  /api/v1/flowboard/projects/{id}
POST   /api/v1/flowboard/projects/{id}/archive

GET    /api/v1/flowboard/projects/{id}/milestones
POST   /api/v1/flowboard/projects/{id}/milestones
PATCH  /api/v1/flowboard/milestones/{id}
POST   /api/v1/flowboard/milestones/{id}/archive

GET    /api/v1/flowboard/projects/{id}/components
POST   /api/v1/flowboard/projects/{id}/components
PATCH  /api/v1/flowboard/components/{id}
PATCH  /api/v1/flowboard/components/{id}/retire

GET    /api/v1/flowboard/projects/{id}/board
GET    /api/v1/flowboard/milestones/{id}/board
GET    /api/v1/flowboard/me/board?kind=

GET    /api/v1/flowboard/work-items?projectId=&milestoneId=&componentId=&type=&assigneeId=
POST   /api/v1/flowboard/work-items
GET    /api/v1/flowboard/work-items/{code}
PATCH  /api/v1/flowboard/work-items/{id}/move
PATCH  /api/v1/flowboard/work-items/{id}/title
PATCH  /api/v1/flowboard/work-items/{id}/description
PATCH  /api/v1/flowboard/work-items/{id}/type
PATCH  /api/v1/flowboard/work-items/{id}/priority
PATCH  /api/v1/flowboard/work-items/{id}/estimated-points
PATCH  /api/v1/flowboard/work-items/{id}/estimated-completion-date
PATCH  /api/v1/flowboard/work-items/{id}/component
PATCH  /api/v1/flowboard/work-items/{id}/milestone

GET    /api/v1/flowboard/projects/{id}/flow
PUT    /api/v1/flowboard/projects/{id}/flow

GET    /api/v1/flowboard/projects/{id}/members
POST   /api/v1/flowboard/projects/{id}/members

POST   /api/v1/flowboard/work-items/{id}/comments
```

**Notes:**

* `GET /work-items` requires `projectId` **or** `assigneeId`; unscoped listing is not supported.
* `milestoneId=none` and `componentId=none` filter items with no milestone / no component assignment.
* `kind=` filters projects and the personal board by `ProjectKind`.
* `GET /work-items/{idOrCode}` resolves both GUIDs and human codes (`CNL-101`) — this is what makes codes useful in conversation.
* Every editable field of a work item has its own `PATCH` route, so each change can be audited independently and the front-end's inline controls can save one field at a time. There is no general update endpoint. A `null` body value clears the field.
* `PATCH /work-items/{id}/milestone` rejects a target milestone that is `Completed` or `Archived` with `400 Bad Request` (`WorkItem.MilestoneNotAcceptingAssignments`). `POST /work-items` returns the same error for the same reason.
* `POST /milestones/{id}/archive` and any transition to `Completed` return `409 Conflict` when the milestone still holds open work items, listing the offending item codes so they can be resolved or reassigned (§5.2).
* `POST /projects/{id}/archive` and any transition to `Completed` behave the same way (§5.1), except that open items cannot be reassigned out of a project — they must be finished, cancelled, or archived.
* Every endpoint is scoped to the caller's project memberships; a non-member receives `404`, not `403`, so that project existence is not disclosed.

---

## 12. Onboarding & Initial State

### 12.1 System bootstrap

Every environment starts from an empty schema. The system therefore has a chicken-and-egg problem at first run: creating a user requires an `Administrator` (§5.4), and on a fresh database no user exists.

* A single `Administrator` account is **seeded when the schema is created**. This is the only user that is not created through the API.
* Its initial credentials are environment-supplied, never hard-coded in the repository, and must be changed on first sign-in.
* Every subsequent user — including other `Administrator`s — is created through the API by that account.
* The seeded account is a real user, not a special case in the authorization model: it holds the global role `Administrator` and nothing else distinguishes it.

### 12.2 Project onboarding

Creating a project provisions:

* Its flow, seeded from the template for the project's `ProjectKind`
* A default project board
* The creating user as project `Admin`
* **No components.** The component set is project-specific and cannot be usefully guessed; the UI prompts for it on first use and work items remain valid without one.
* **No milestones.** Projects can exist without milestones indefinitely.

The UI must not require creating a milestone before the first work item.

**Progressive disclosure:** while a project has no milestones, the milestone level is hidden from navigation and omitted from the work item form. It appears once the first milestone is created. This keeps the newly introduced hierarchy level from taxing the simple case — a solo maintenance day on a product should not require touching it.

---

## 13. Success Metrics

* **Cycle time, segmented by `ProjectKind`, by project, and by work item type** (further segmentable by component)
* Reduction in use of external tools
* Improved work visibility across the team

**Note on measurement:** cycle time must be segmented. Aggregating a client engagement and an internal script into one number produces a figure that reflects work mix rather than throughput. The useful question this answers is whether client work moves slower than own-product work, and where the difference sits. A baseline must be captured during the first full month of adoption before any target is set.

Adoption metrics (active users, weekly actives) are deliberately excluded: with a team this size they measure nothing.

---

## 14. Risks & Considerations

| Risk | Mitigation |
|---|---|
| Friction from the extra hierarchy level | Progressive disclosure (§12); no milestone required to create work |
| Pressure to move the code prefix onto `Milestone` | Never. The prefix hangs off the durable entity (§5.6); moving it post-launch, with thousands of items, becomes an identifier-collision migration rather than a schema change |
| A temporary collaborator retains access after the engagement ends | Revocation is immediate and audited (§5.11); membership expiry dates are an open question (§16). Until then, offboarding is a checklist item, not a system guarantee |
| Cross-project data leakage on a client engagement | Every query scoped to caller memberships (§8.3); non-members receive `404` so project existence is not disclosed (§11) |
| Over-engineering the workflow engine | One flow per project, configured by admins — no per-type flows and no end-user workflow designer in the MVP |
| Permission management complexity | Project-level RBAC only (§5.4); milestones and components carry no permissions |
| One flow proving too coarse when a type needs different handling | Express the difference with states the whole project shares (§5.7); per-type flows remain an additive change if the constraint ever genuinely binds |
| Pressure to add multi-tenancy | Explicitly out of scope (§1). Would require revisiting code uniqueness, authorization, and data isolation as a coordinated change |
| Spinning up a project per initiative, recreating the old model | Glossary (§4) enforced in code, API, and UI; `Milestone` exists precisely to absorb this |
| `Product` and `Internal` kinds blurring together | Explicit distinguishing rule in §5.1: distribution beyond the team is the test |
| Components used as a substitute for separate projects | Components carry no code prefix, no flow, and no permissions (§5.3) — they are deliberately too weak to be misused this way |
| Component proliferation (dozens of near-duplicate areas) | Admin-only creation; retirement instead of deletion; expected order of magnitude is single digits per project |

---

## 15. Future Enhancements

Ordered by expected value:

1. **Membership expiry** — time-bounded access for temporary collaborators, with automatic revocation
2. **Project backlog view** — a flat, ordered planning view (`GET /projects/{id}/backlog`), complementing the board's execution view. It serves triage of unassigned work, milestone planning, and totalling estimates when quoting a client. Removed from §11 until three questions are answered: **what it contains** (items not started / items with no milestone / all open items — the document currently supports all three readings), **how order is persisted** (a manual rank on `WorkItem`, which §10 does not have today, versus a fixed sort by priority and age), and **how to avoid colliding** with the flow state also named `Backlog` (§5.7)
3. **Intake / triage inbox** — where bugs and requests land before routing. Overlaps substantially with the backlog view above; decide the two together
4. **Cycles / sprints** as an independent time axis (structure ≠ time ≠ state)
5. **Releases / versions**, owned by the project
6. **Flow metrics**: lead time, cycle time, cumulative flow diagram, WIP limits (domain events already provide the raw data)
7. **Billing export** for `Client` projects, derived from logged time (§5.10)
8. **Initiative / portfolio level above `Project`** — the correct answer to cross-project initiatives (§4.1). Must not be approximated by relaxing `Milestone.ProjectId`
9. Multi-valued components, if single-valued proves too restrictive (§5.3)
10. Work item hierarchy (Epic → Story → Subtask), if flat items prove insufficient
11. Git repository integration
12. Workflow automation
13. SSO / corporate authentication
14. Real-time notifications
15. AI-assisted prioritization

---

## 16. Open Questions

* What is the default flow template for each `ProjectKind`, exactly? (§5.7 shows illustrative examples; the shipped defaults need sign-off)
* Should membership carry an expiry date, so a temporary collaborator is revoked automatically at the end of an engagement? (§5.4, §14)
* Should `Viewer` be able to comment, or is it strictly read-only? A client who can comment is useful; a client who can comment is also a support channel.
* What is the retention policy for archived projects and their work items?
* Does estimation use story points, hours, or both? `Client` projects may need hours specifically.
* Should the project board default to showing items with no milestone, or hide them behind a filter?
* Should component be **required** on `Bug` items specifically, to force triage of where a defect lives?
* Is there a maximum component count per project worth enforcing, or is admin-only creation sufficient governance?

---

## Conclusion

Flowboard keeps apart two things that are easy to collapse into one. The **project** is durable: it owns the backlog, the identity scheme, the workflow, and access. The **milestone** is temporal: it groups work for planning and owns none of those. Work items belong to a project always, and to a milestone only when it's meaningful.

Within a project, **components** provide the durable structural axis — where work lives — independently of the temporal milestone axis and the transient state axis. Together the three axes describe a work item without any of them having to stand in for another. **`ProjectKind`** sits outside them, classifying the container rather than the work, so that a client engagement, an owned product, a research spike, and an internal tool can share one system without pretending to be the same thing.

Two properties carry the most weight in practice. The work item identifier hangs off the only entity stable enough to carry it. And project membership is the single gate on visibility — which is what makes it safe to bring someone in for one engagement without opening the rest.
