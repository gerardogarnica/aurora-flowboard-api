---
paths:
  - "docs/**"
---

# Documentation in `docs/`

## Layout

| Folder | Holds | File name |
|---|---|---|
| `docs/product/` | PRD and product requirements | `<topic>.md` (e.g. `flowboard-prd.md`) |
| `docs/backlog/` | Backlog features: problem, value, scope, candidate stories | `YYYY-MM-DD-<topic>-feature.md` |
| `docs/stories/` | User stories with acceptance criteria | `YYYY-MM-DD-<topic>-story.md` |
| `docs/architecture/` | Architecture references and templates | `<topic>.md` |
| `docs/architecture/adr/` | Architecture decision records | `NNNN-<topic>.md` (4-digit sequence) |
| `docs/specs/` | Design specs, one per feature or fix | `YYYY-MM-DD-<topic>-design.md` |
| `docs/plans/` | Implementation plans, one per spec | `YYYY-MM-DD-<topic>-plan.md` (same date and topic as its spec) |
| `docs/reviews/` | Code reviews, gap analyses and remediation plans | `<topic>.md` |
| `docs/templates/` | Templates for the documents above | — |

- `<topic>` is lowercase kebab-case in English (`refresh-token-hardening`), matching branch names.
- Never write specs or plans to `docs/superpowers/` (the old default of the superpowers plugin, no longer used in this repo).
- When a document moves, update the links in the documents that are still live (headers, indexes, skills). Steps inside plans that were already executed are a historical record: leave them alone.
- Keep `docs/README.md` (the index) in sync when adding, moving or retiring a document.

## Language

New documents are written in **Spanish**. Identifiers, code, paths, routes, error codes and technical terms with no established translation stay in English (`ChangeUserRoleHandler`, `User.LastAdministrator`, `xmin`). Older documents written in English are not translated.

## Header

Every feature, story, spec, plan and ADR starts with this header right after the title, in this order. Use `—` for a field that does not apply yet.

```markdown
**Fecha:** YYYY-MM-DD
**Estado:** Borrador
**Tickets:** FLW-35, I5
**Rama:** `fix/last-administrator-guard` · **PR:** #34
**Plan:** `docs/plans/...`      ← in a spec (a plan has **Spec:**, an ADR has **Relacionado:**)
```

A feature and a story have no **Rama:** (they precede any branch) and replace the last lines:

| Document | Header after **Estado:** |
|---|---|
| Feature | **Milestone:** (Flowboard milestone name, or `No aplica` for a size-S feature) · **Historias:** (story paths, comma-separated) |
| Story | **Tickets:** (work item code, `FLW-NN`) · **Feature:** (feature path, or `—`) · **Spec:** (spec path, or `No aplica (acotado)`) |

`Estado` lifecycle:

| Estado | Meaning |
|---|---|
| `Borrador` | Being written or under review |
| `Aprobado` | Approved by the user; implementation may start |
| `Implementado` | Merged into `staging`. Set it (and fill `**PR:**`) once the user confirms the merge, never in the PR itself |
| `Obsoleto` | Replaced or abandoned; add a line saying why and what replaces it |

For a feature, `Aprobado` means prioritized and ready for stories, and `Implementado` means all its stories are. For a story, `Implementado` means its spec (or bounded change) is merged.

## Content

Start from the templates in `docs/templates/` (`feature.md`, `story.md`, `spec.md`, `plan.md`, `adr.md`). Sections that don't apply are kept with a one-line reason, not deleted, so a reader can tell "not considered" from "not applicable".

The chain is `feature → story → spec → plan`, each one optional depending on size: a bug or a bounded change needs no feature, and a bounded story needs no spec.

- **Feature:** the *what* and *why* of a backlog item (problem, users, value, scope, priority, size, candidate story titles), never the *how*. Created with the `backlog` skill. It is called "feature" and not "backlog item" because *backlog* already means the `Backlog` stage of a work item (`work-items.md`).
  - It maps to a Flowboard `Milestone` in `Draft` only when it has size M/L or a target date. A size-S feature gets none, and its stories are work items with no milestone.
  - A milestone is never a parent of its stories: the PRD keeps work items flat and has no *Epic*.
- **Story:** a lightweight user story ("Como… quiero… para…" + 2–5 Dado/Cuando/Entonces acceptance criteria, observable behavior only). Created with the `story` skill. It maps to a `WorkItem` of type `Story`; its code goes in **Tickets:** and from there into the spec's **Tickets:**.
  - Plan tasks never become work items.
  - A bug goes straight to a `Bug` work item, with no story.
- **Flowboard records:** the `backlog` and `story` skills end with a "Registro en Flowboard" section holding the fields to copy by hand. They never call the Flowboard API.
- **Spec:** design and decisions, no step-by-step code. Created with the `spec` skill. When it comes from a story, every acceptance criterion has an answer in the design and a test in the Tests section.
- **Plan:** the executable steps derived from an approved spec. Written with the `write-plan` skill and executed with `execute-plan`. Every task carries a `**Skill:**` line naming the repo skill that implements it (`domain-entity`, `create-feature`, `unit-testing`, or `—`), and the global constraints say each task follows its skill: the executor may start without context and only loads a skill the plan names.
- **Approving a plan** (`Estado: Aprobado`) authorizes one commit per task while it is executed. It never authorizes push or a PR; those wait for the user.
- **ADR:** one decision that outlives a single feature (a cross-cutting pattern, a library choice), with the alternatives that were rejected.
- A bounded change (approved in chat) does not get a spec or a plan.
