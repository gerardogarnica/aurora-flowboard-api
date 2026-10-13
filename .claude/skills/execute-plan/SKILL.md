---
name: execute-plan
description: Ejecuta tarea a tarea un plan aprobado de docs/plans/. Invoca la skill indicada en cada tarea, aplica TDD, marca los checkboxes, hace un commit por tarea y termina con la verificación completa (dotnet-test-runner, check de EF, arch-guard y code-reviewer). Úsala solo cuando el usuario pida ejecutar o implementar un plan; nunca la encadenes por tu cuenta tras write-plan.
argument-hint: <ruta del plan (opcional), p. ej. docs/plans/2026-10-20-optimistic-concurrency-plan.md>
---

# Ejecutar un plan

Plan: **$ARGUMENTS**

Habla con el usuario en español. Código, commits y PR van en inglés.

<!-- Inspirado en el método de superpowers (MIT, Jesse Vincent). Texto propio. -->

## 1. Comprobaciones previas

- Si no se indicó ruta, busca en `docs/plans/` los planes con `**Estado:** Aprobado` que tengan tareas sin marcar (`- [ ]`). Si hay uno, confírmalo con el usuario antes de empezar; si hay varios, pregúntale cuál; si no hay ninguno, dilo y para.
- El encabezado del plan dice `**Estado:** Aprobado`. Si no, para: un plan sin aprobar no autoriza ni código ni commits.
- La rama actual es la del encabezado (`**Rama:**`) y parte de staging (`git merge-base --is-ancestor origin/staging HEAD`). Si la rama no existe, créala con `git switch --no-track -c <rama> origin/staging`.
- Anota los cambios sin commitear que ya existían (p. ej. `.claude/settings.local.json`, `appsettings.Development.json`): nunca entran en los commits del plan.
- Si hay tareas ya marcadas (`- [x]`), retoma desde la primera sin marcar y comprueba con `git log` que su commit existe.
- Si el spec, el plan o `docs/README.md` tienen cambios sin commitear (lo normal tras `spec` y `write-plan`), pregunta al usuario si se commitean antes, en un commit de documentación propio. Ese commit **no** lo cubre la aprobación del plan: sin su respuesta, no lo hagas.

## 2. Revisión crítica

Lee el plan entero, y el spec que enlaza, **antes** de la primera tarea. Si hay contradicciones, información que falta, código que no encaja con el estado actual del repo o pasos que ya no tienen sentido, dilo y pregunta antes de empezar. Es más barato corregir el plan ahora que a mitad.

## 3. Por cada tarea

1. Invoca (Skill tool) la skill de su línea `**Skill:**`, salvo que sea `—`. De esa skill aplica sus convenciones y plantillas; **el orden de los pasos (TDD) y la verificación los marca el plan**, aunque la skill escriba los tests al final o pida lanzar `dotnet-test-runner` al terminar (aquí se lanza según el paso 5).
2. Sigue los pasos en orden:
   - escribe el test y **míralo fallar** con el comando del plan; si falla por otro motivo que el esperado, para;
   - implementa y **míralo pasar**;
   - si el plan trae el código, úsalo; si tienes que desviarte (una firma cambió, el código no compila), aplica el cambio mínimo y anótalo en la tarea (`> Desviación: ...`).
3. Marca los checkboxes de la tarea en el fichero del plan.
4. **Commit** con el mensaje del plan, añadiendo los ficheros uno por uno (nunca `git add -A`), incluido el plan con sus checkboxes. Aprobar el plan autoriza este commit; nunca hagas push ni abras PR.

## 4. Cuándo parar

Para y pregunta, sin improvisar, si:

- un test falla de forma inesperada o uno que ya pasaba se rompe;
- el plan resulta incorrecto o incompleto para una tarea;
- la desviación necesaria cambia el diseño del spec (un tipo nuevo, otra capa, otro código HTTP);
- aparece algo bloqueante del entorno (p. ej. un fichero bloqueado porque la API está corriendo en Visual Studio: pide que la detengan).

Las desviaciones que el usuario apruebe quedan anotadas en el plan.

## 5. Verificación

- Al terminar cada capa y al final: agente `dotnet-test-runner` (build y los tres ejecutables de tests).
- Si cambió el modelo de EF: `dotnet ef migrations has-pending-model-changes --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api`, con el build al día.
- Al final, `arch-guard` y `code-reviewer` **en paralelo**, con la lista de ficheros del plan, el spec y lo que está fuera de alcance. Verifica cada hallazgo antes de aceptarlo. Corrige los errores propios **sin hacer commit** (la aprobación del plan solo cubre un commit por tarea) y pregunta al usuario si los commitea. Presenta las sugerencias opcionales en una tabla con tu recomendación para que el usuario elija.
- Da al usuario los pasos de verificación manual del plan.

## 6. Final

- El plan sigue en `**Estado:** Aprobado`. Cuando el usuario confirme que se fusionó en `staging`, pasa el plan y su spec a `**Estado:** Implementado` y actualiza la columna "Estado" de `docs/README.md` (commit solo si el usuario lo pide).
- Informa con los commits creados, los recuentos de tests, el resultado de los agentes y las desviaciones.
- Push y PR solo cuando el usuario los pida. Si el plan viene de `review-fix`, sigue en su paso 6.
