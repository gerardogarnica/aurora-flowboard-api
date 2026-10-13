---
paths:
  - "docs/**"
---

# Documentation in `docs/`

## Layout

| Folder | Holds | File name |
|---|---|---|
| `docs/product/` | PRD and product requirements | `<topic>.md` (e.g. `flowboard-prd.md`) |
| `docs/architecture/` | Architecture references and templates | `<topic>.md` |
| `docs/architecture/adr/` | Architecture decision records | `NNNN-<topic>.md` (4-digit sequence) |
| `docs/specs/` | Design specs, one per feature or fix | `YYYY-MM-DD-<topic>-design.md` |
| `docs/plans/` | Implementation plans, one per spec | `YYYY-MM-DD-<topic>-plan.md` (same date and topic as its spec) |
| `docs/reviews/` | Code reviews, gap analyses and remediation plans | `<topic>.md` |
| `docs/templates/` | Templates for the documents above | — |

- `<topic>` is lowercase kebab-case in English (`refresh-token-hardening`), matching branch names.
- Never write specs or plans to `docs/superpowers/`. Skills that default there (`superpowers:brainstorming`, `superpowers:writing-plans`) must use `docs/specs/` and `docs/plans/` instead.
- When a document moves, update the links in the documents that are still live (headers, indexes, skills). Steps inside plans that were already executed are a historical record: leave them alone.
- Keep `docs/README.md` (the index) in sync when adding, moving or retiring a document.

## Language

New documents are written in **Spanish**. Identifiers, code, paths, routes, error codes and technical terms with no established translation stay in English (`ChangeUserRoleHandler`, `User.LastAdministrator`, `xmin`). Older documents written in English are not translated.

## Header

Every spec, plan and ADR starts with this header right after the title, in this order. Use `—` for a field that does not apply yet.

```markdown
**Fecha:** YYYY-MM-DD
**Estado:** Borrador
**Tickets:** FLW-35, I5
**Rama:** `fix/last-administrator-guard` · **PR:** #34
**Plan:** `docs/plans/...`      ← in a spec (a plan has **Spec:**, an ADR has **Relacionado:**)
```

`Estado` lifecycle:

| Estado | Meaning |
|---|---|
| `Borrador` | Being written or under review |
| `Aprobado` | Approved by the user; implementation may start |
| `Implementado` | Merged into `staging` (set it in the PR that implements it, or right after the merge) |
| `Obsoleto` | Replaced or abandoned; add a line saying why and what replaces it |

## Content

Start from the templates in `docs/templates/` (`spec.md`, `plan.md`, `adr.md`). Sections that don't apply are kept with a one-line reason, not deleted, so a reader can tell "not considered" from "not applicable".

- **Spec:** design and decisions, no step-by-step code. Created with the `spec` skill.
- **Plan:** the executable steps derived from an approved spec. Created with `superpowers:writing-plans` (saved to `docs/plans/`).
- **ADR:** one decision that outlives a single feature (a cross-cutting pattern, a library choice), with the alternatives that were rejected.
- A bounded change (approved in chat) does not get a spec or a plan.
