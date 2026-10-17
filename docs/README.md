# Documentación de Aurora Flowboard

Las convenciones completas (nombres, encabezado, estados, idioma) están en `.claude/rules/documentation.md`. Las plantillas están en `templates/`.

## Estructura

| Carpeta | Contenido |
|---|---|
| `product/` | Requisitos de producto |
| `architecture/` | Referencias de arquitectura y ADRs (`adr/NNNN-<tema>.md`) |
| `specs/` | Specs de diseño: `YYYY-MM-DD-<tema>-design.md` |
| `plans/` | Planes de implementación: `YYYY-MM-DD-<tema>-plan.md` |
| `reviews/` | Revisiones, análisis de gaps y planes de remediación |
| `templates/` | Plantillas de spec, plan y ADR |

Los documentos nuevos se escriben en español; código, rutas e identificadores van en inglés.

## Índice

### Producto
- [Flowboard — Product Requirements Document](product/flowboard-prd.md)

### Arquitectura
- [.NET Clean Architecture — plantilla de arranque](architecture/clean-architecture-template.md)
- ADRs:
  - [0001 — Concurrencia optimista con `xmin`, SSI y traducción en el pipeline](architecture/adr/0001-optimistic-concurrency.md)

### Specs y planes

| Fecha | Tema | Estado | Spec | Plan |
|---|---|---|---|---|
| 2026-08-22 | Actualización por campo de work items | Implementado | [spec](specs/2026-08-22-work-item-field-updates-design.md) | [plan](plans/2026-08-22-work-item-field-updates-plan.md) |
| 2026-10-07 | Endurecimiento de refresh tokens (I3, I4, M11) | Implementado | [spec](specs/2026-10-07-refresh-token-hardening-design.md) | [plan](plans/2026-10-07-refresh-token-hardening-plan.md) |
| 2026-10-16 | Concurrencia optimista y conflictos de persistencia (I9) | Aprobado | [spec](specs/2026-10-16-optimistic-concurrency-design.md) | [plan](plans/2026-10-16-optimistic-concurrency-plan.md) |

### Revisiones
- [Plan de ramas para las correcciones de la revisión](reviews/review-remediation-plan.md)
- [Gap analysis frente al PRD](reviews/backend-gap-analysis.md)
