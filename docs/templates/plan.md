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

## Tareas

### Tarea 1: <nombre>

**Ficheros:**
- Crear: `ruta`
- Modificar: `ruta`
- Test: `ruta`

- [ ] Escribir el test que falla
- [ ] Verlo fallar (`-class "Namespace.ClassName"`)
- [ ] Implementar lo mínimo
- [ ] Verlo pasar
- [ ] Commit

## Verificación final

- [ ] `dotnet-test-runner` limpio (build + los tres ejecutables de tests)
- [ ] `dotnet ef migrations has-pending-model-changes` (si cambió el modelo)
- [ ] `arch-guard` y `code-reviewer`
- [ ] Verificación manual:
