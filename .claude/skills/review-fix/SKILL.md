---
name: review-fix
description: Arranca y lleva hasta el PR la corrección de uno o más puntos de la revisión del repositorio (C1, I5, I6 I7 M16…) siguiendo docs/reviews/review-remediation-plan.md y la página de seguimiento.
disable-model-invocation: true
argument-hint: <IDs de la revisión, p. ej. "I6 I7 M16">
---

# Corregir puntos de la revisión

Puntos: **$ARGUMENTS**

Habla con el usuario en español. Código, commits y PR van en inglés.

<!-- Los pasos 1 y 6 (y el frontmatter) son lo único específico de la revisión. Los pasos 2–5 son el
     flujo general del repo y solo dependen del "encargo" que produce el paso 1. -->

## 1. Contexto y encargo

Objetivo de este paso: terminar con un **encargo** que use el resto de la skill:
- **rama** objetivo;
- **problema** verificado en el código y su **impacto**;
- **acción propuesta**;
- **alcance**: qué entra y qué cubren otros puntos.

1. Lee `docs/reviews/review-remediation-plan.md`: qué rama agrupa estos IDs, qué otros puntos van en ella y las dependencias de orden (p. ej. C1 antes que R9, R1 antes que R2–R4, una sola migración abierta a la vez). Si los IDs pedidos no coinciden con la agrupación del plan, pregunta antes de seguir.
2. Lee cada punto de la página de seguimiento con `ArtifactData` → `get`:
   - `url`: `https://claude.ai/artifact/HgWcansbBYbngpQjsF5u2s`
   - `collection`: `findings`, `doc_id`: el ID (p. ej. `I6`)
   - Campos: `title`, `problem`, `impact`, `where`, `action`, `status`, `note`, `phase`, `severity`.
   - No uses `Artifact` → `read`: devuelve el HTML de la página, que no contiene los datos.
   - Guarda la `version` de cada documento; la necesitas en el paso 6.
3. Verifica en el código cada entrada de `where`. Las líneas son de la revisión sobre `44f0f22` y pueden haber cambiado; si el problema ya no existe, dilo y propón marcarlo como `dismissed`.
4. Lo que el plan asigna a otros puntos (p. ej. la concurrencia a I9) queda fuera de alcance; anótalo con su ID.
5. Lee las rules de `.claude/rules/` que toquen las áreas afectadas.
6. **Retomar:** busca en `docs/specs/` y `docs/plans/` documentos cuyo `**Tickets:**` incluya estos IDs. Si existen, no empieces de cero; dile al usuario dónde se quedó y sigue desde ahí:
   - spec en `Borrador` → retoma la skill `spec` en su revisión;
   - spec `Aprobado` sin plan, o plan en `Borrador` → `write-plan`;
   - plan `Aprobado` con tareas sin marcar → ofrece `execute-plan` (paso 4);
   - plan `Aprobado` con todas las tareas marcadas → paso 6.

## 2. Rama

- `git fetch origin`. Si la rama del encargo no existe, créala desde `origin/staging` (`git switch --no-track -c <rama> origin/staging`).
- Si ya existe, comprueba que parte de staging (`git merge-base --is-ancestor origin/staging HEAD`) y que no arrastra commits ajenos (`git log --oneline origin/staging..HEAD`). Si parte de otra base (p. ej. `main`), avisa al usuario y deja que decida.
- Anota los cambios sin commitear que ya existían (p. ej. `.claude/settings.local.json`, `appsettings.Development.json`): no son parte del encargo y nunca se incluyen en el commit.

## 3. Diseño (antes de tocar código)

1. Resume lo que entiendes: el problema verificado, la acción propuesta, lo que asumes y el criterio de éxito. Separa lo que dice el encargo de tus supuestos.
2. Clasifica y dilo en voz alta:
   - **Acotado**: cambia flujos que ya existen (un handler, una entidad, el seeder). Diseño corto en el chat.
   - **Arquitectónico**: afecta a varios agregados, introduce un patrón nuevo o lleva una migración compleja. **Pasa directamente a la skill `spec`** con el encargo del paso 1 (no hagas los puntos 3–5 de este paso: las preguntas, el diseño y la aprobación los hace `spec`). `spec` encadena `write-plan`; con el plan aprobado, sigue en el paso 4.
   - Si dudas, elige el más pesado.

   Los puntos 3–5 son solo para los encargos acotados.
3. Haz las preguntas que importen **de una en una**, con `AskUserQuestion`, de 2 a 4 opciones y la recomendada primero. Pregunta solo lo que cambie el diseño; lo que tenga un valor por defecto razonable, decídelo y dilo.
4. Presenta el diseño: capas y ficheros que cambian, tests, docs y lo que queda fuera de alcance (con dónde se cubrirá).
5. **Para y espera aprobación explícita.** No escribas código de producción hasta entonces.

## 4. Implementación con TDD

- **Si hay un plan aprobado** (encargo arquitectónico), ejecútalo con la skill `execute-plan` cuando el usuario lo pida (`write-plan` se lo ofrece al aprobarse el plan, aquí o en una sesión nueva). `execute-plan` ya incluye el TDD y la verificación de los pasos 4 y 5, y continúa en el paso 6. El resto de este paso y el paso 5 son para los encargos acotados.
- Test primero, verlo fallar, código mínimo y verlo pasar. Orden habitual: dominio → application → infraestructura → API → docs.
- Aquí manda el TDD aunque `create-feature` ponga los tests al final de su flujo: escribe primero los tests de cada pieza y después la pieza.
- En C#, un test que llama a una API inexistente no compila; ese error de compilación cuenta como RED. Para ver fallar las **assertions**, añade antes la firma con un stub sin comportamiento y después implementa.
- Lo que no tiene proyecto de tests (la capa Api, el seeder, `Program.cs`) se verifica a mano. Dale al usuario los pasos exactos: SQL, reinicio, petición HTTP y resultado esperado.
- Sigue las skills del repo cuando apliquen: `domain-entity`, `create-feature`, `unit-testing`.
- Si la decisión es nueva en un área, regístrala en la rule correspondiente de `.claude/rules/` (no en `CLAUDE.md`).

## 5. Verificación

- Build y tests: lanza el agente `dotnet-test-runner` (como en `create-feature` y `domain-entity`). Durante los ciclos de TDD puedes ejecutar tú un solo ejecutable con `-class`, pero la verificación final es la del agente.
- Si el agente informa de un fichero bloqueado (`MSB3021`/`MSB3027`, "locked by Microsoft Visual Studio / Aurora.Flowboard.Api"), la API está corriendo: pide al usuario que la detenga y vuelve a lanzarlo.
- Si cambió algo del modelo de EF (entidades o configuraciones): `dotnet ef migrations has-pending-model-changes --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api`, con el build al día.
- Lanza `arch-guard` y `code-reviewer` **en paralelo**, pasándoles la lista exacta de ficheros del encargo, el diseño acordado y lo que está fuera de alcance, para que no lo vuelvan a discutir. Verifica cada hallazgo antes de aceptarlo, corrige los errores propios y presenta las sugerencias opcionales en una tabla con tu recomendación para que el usuario elija.

## 6. Cierre

**General:**
- **No hagas commit ni push hasta que el usuario lo pida.** Única excepción: al ejecutar un plan aprobado, `execute-plan` hace un commit por tarea (aprobar el plan lo autoriza). Push y PR siempre esperan a que el usuario los pida.
- Al hacer commit, añade los ficheros uno por uno (nunca `git add -A`). Mensajes en inglés y en imperativo. Separa el código y los tests de los cambios de documentación (rules), como en el historial.
- PR contra `staging`, en inglés, con las secciones: `Summary` (qué corrige, antes y ahora), `Changes` (por capa), `Out of scope` (con dónde se cubrirá) y `Test plan` (recuentos de tests, agentes y verificación manual).

**Específico de la revisión:**
- En el PR, `Summary` empieza nombrando los puntos (p. ej. "Fixes finding **I5** from the full repository review").
- Actualiza el tracker de cada punto con `ArtifactData` → `update`, usando `if_version` con la versión que leíste en el paso 1: `status: "in_progress"` y una `note` con el número de PR y un resumen de una línea. Cuando el usuario confirme el merge, pasa a `status: "done"`.

**Tras el merge (general):** si el encargo tuvo spec y plan, pásalos a `**Estado:** Implementado`, rellena `**PR:**` en sus encabezados y actualiza la columna "Estado" de `docs/README.md` (commit solo si el usuario lo pide).
