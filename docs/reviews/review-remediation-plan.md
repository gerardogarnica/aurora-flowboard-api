# Plan de ramas para las correcciones de la revisión

Revisión completa del repositorio sobre `44f0f22` (2026-10-04). El estado de cada punto se sigue en la página de seguimiento: https://claude.ai/artifact/HgWcansbBYbngpQjsF5u2s

Criterio: una rama por fase cuando los cambios son pequeños y del mismo tema, y una rama propia cuando el cambio es grande, lleva migración o es un refactor puro.

## Ramas propuestas

| Rama | Puntos | Motivo |
|---|---|---|
| `fix/redact-sensitive-logging` | C1 | Es crítico y pequeño, sale primero y solo |
| `fix/auth-rate-limiting` | I2 | Solo toca la configuración de la API |
| `fix/refresh-token-hardening` | I3 + I4 (+ M11, M12 si se quiere) | Comparten el modelo de tokens y la migración del hash |
| `fix/last-administrator-guard` | I5 | Toca dominio, handler y seeder; necesita tests propios |
| `fix/project-domain-invariants` | I6 + I7 (+ M16) | Invariantes de `Project`, todos con tests de dominio |
| `perf/project-read-projections` | I8 | Solo cambian consultas, fácil de comparar antes y después |
| `fix/optimistic-concurrency` | I9 | Afecta a todos los agregados y al manejo de excepciones |
| `refactor/...`, una por extracción o grupo pequeño | R1–R11 | Refactors sin cambio de comportamiento, separados de las correcciones |
| Backlog por tema | M10–M24 | Por ejemplo, `fix/cancellation-tokens` (M18), `fix/validation-gaps` (M17, M19, M24), `chore/ops-config` (M22) |

## Orden y dependencias

1. **C1 antes que R9.** Los dos tocan los behaviors. Si se hace primero el refactor, la corrección de seguridad espera sin necesidad.
2. **R1 antes que R2, R3 y R4.** Todos usan el helper `GetCurrentUserAsync`.
3. **R5 resuelve M19.** Si se hace R5, cerrar M19 en el mismo PR.
4. **I3 lleva migración:** conviene que no haya otra migración abierta a la vez, para no generar conflictos en el snapshot del modelo.

## Flujo por rama

- Crear cada rama desde `staging` actualizado. Las ramas `fix/*` se fusionan en `staging`, y luego `staging` pasa a `main`.
- Antes de abrir cada PR: `dotnet build`, los tres ejecutables de tests y los agentes `arch-guard` y `code-reviewer`, porque cada cambio cruza capas.
- Al abrir el PR, anotar su número en las notas del punto en la página de seguimiento y pasarlo a "En curso". Cuando se fusione, marcarlo como "Hecho".
