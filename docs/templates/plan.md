# <Título> — Plan de implementación

**Fecha:** YYYY-MM-DD
**Estado:** Borrador
**Tickets:** —
**Rama:** `<tipo>/<tema>` · **PR:** —
**Spec:** `docs/specs/YYYY-MM-DD-<tema>-design.md`

**Objetivo:** una frase.

**Arquitectura:** dos o tres frases con el enfoque elegido en el spec.

## Restricciones globales

- Reglas de `CLAUDE.md` y `.claude/rules/` que aplican a todas las tareas.
- Cada tarea se implementa siguiendo la skill indicada en su línea **Skill:**. Invócala (Skill tool) antes de escribir el código de la tarea, aunque empieces sin contexto.
- Se ejecuta con la skill `execute-plan`. Aprobar el plan (`**Estado:** Aprobado`) autoriza un commit por tarea; nunca push ni PR sin que el usuario lo pida.

## Tareas

### Tarea 1: <nombre>

**Skill:** `domain-entity` | `create-feature` | `unit-testing` | — (si ninguna aplica: config, docs, migración manual)

**Ficheros:**
- Crear: `ruta`
- Modificar: `ruta`
- Test: `ruta`

- [ ] Escribir el test que falla
- [ ] Verlo fallar (`-class "Namespace.ClassName"`)
- [ ] Implementar lo mínimo
- [ ] Verlo pasar
- [ ] Commit: `<mensaje en inglés, imperativo>` (autorizado por la aprobación del plan)

## Verificación final

- [ ] `dotnet-test-runner` limpio (build + los tres ejecutables de tests)
- [ ] `dotnet ef migrations has-pending-model-changes` (si cambió el modelo)
- [ ] `arch-guard` y `code-reviewer`
- [ ] Verificación manual:
