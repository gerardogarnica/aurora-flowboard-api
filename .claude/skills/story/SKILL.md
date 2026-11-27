---
name: story
description: Escribe una historia de usuario ligera ("Como… quiero… para…" y de 2 a 5 criterios de aceptación Dado/Cuando/Entonces), con los campos para registrarla como work item `Story` en Flowboard, y la guarda en docs/stories/YYYY-MM-DD-<tema>-story.md con la plantilla del repo. Úsala cuando el usuario quiera detallar una historia candidata de un feature de docs/backlog/ o escribir una historia suelta antes de pasar a spec.
argument-hint: <ruta del feature y título de la historia, o la historia en texto libre>
---

# Historia de usuario

Encargo: **$ARGUMENTS**

Habla con el usuario en español. Sigue `.claude/rules/documentation.md` (estructura, encabezado, estados, idioma).

La historia es **ligera** a propósito: dice qué necesita un usuario y cómo se sabrá que está hecho. El diseño técnico va en `spec`. **No escribas código** ni detalles de implementación mientras uses esta skill.

**No llames a la API de Flowboard** ni pidas credenciales. La sección "Registro en Flowboard" deja los campos listos para que el usuario cree el work item a mano.

## 1. Entrada

- **Si viene de un feature** (`docs/backlog/...`): léelo y toma la historia candidata indicada. Si no se indicó, pregunta cuál. Hereda el rol, el alcance y el milestone del feature.
- **Si es suelta:** sigue igual, con `**Feature:** —`.
- Si el texto describe un **bug**, dilo: un bug va directo como work item `Bug` y no necesita historia. Pregunta si aun así la quiere.
- Si falta el rol, la necesidad o el beneficio y no puedes deducirlo del contexto, pregúntalo **en un solo mensaje**.

## 2. Borrador

Presenta en el chat, siguiendo `docs/templates/story.md`:

1. **La historia:** "**Como** <rol>, **quiero** <acción>, **para** <beneficio>".
   - El rol usa los nombres de Flowboard: `Administrator`/`Member`, o un `ProjectRole` (`Admin`, `Analyst`, `Developer`, `QA`, `Viewer`).
   - El "para" es obligatorio: es lo que permite juzgar una solución alternativa.
2. **De 2 a 5 criterios de aceptación** en formato "**Dado** …, **cuando** …, **entonces** …".
   - Describen comportamiento **observable**: lo que el usuario ve o recibe, incluido un 403 o un 404.
   - No describen implementación: nada de clases, tablas, policies ni queries.
   - Incluye al menos un caso de permisos o de error cuando aplique (p. ej. un `Viewer`).
3. **Notas:** lo que queda fuera y las dependencias. "Ninguna" si no hay.
4. **Registro en Flowboard:**
   - título de hasta 200 caracteres, como resumen y no como la frase "Como…";
   - tipo `Story`;
   - prioridad `Low`/`Medium`/`High`/`Critical`;
   - puntos (entero mayor que 0, u omitidos);
   - milestone y componente si aplican.

Termina con: "¿Qué cambias? Dímelo en una frase o responde "ok"."

## 3. ¿Es demasiado grande?

Sin checklist formal. Solo avisa y propón partirla si ves alguna de estas señales:

- salen más de 5 criterios;
- el "quiero" junta dos acciones con "y";
- mezcla roles distintos con necesidades distintas.

Si el usuario acepta partirla, escribe la primera y añade las otras como candidatas en el feature, o en "Notas" si es suelta.

## 4. Escritura

- Crea `docs/stories/YYYY-MM-DD-<tema>-story.md` a partir de `docs/templates/story.md`.
  - `<tema>` va en kebab-case inglés.
  - `**Estado:** Borrador`.
  - `**Tickets:** —` hasta que exista el work item.
  - `**Feature:**` lleva la ruta del feature, o `—`.
  - `**Spec:** —`.
- Si viene de un feature, añade la ruta de la historia a su `**Historias:**`, separada por comas, y a la columna "Historias" de su fila en `docs/README.md`.
- Añade la fila de la historia en `docs/README.md`, tabla "Historias", con `—` en "Ticket" y en "Spec".

## 5. Revisión del usuario

- Pide al usuario que revise el fichero y aplica los cambios que pida.
- Con su aprobación:
  - cambia el encabezado a `**Estado:** Aprobado` y la columna "Estado" de su fila en `docs/README.md`;
  - recuérdale que cree el work item en Flowboard con los campos de "Registro en Flowboard" y que te diga su código (`FLW-NN`). Ponlo en `**Tickets:**` y en la columna "Ticket".
- No hagas commit salvo que te lo pida.
- Siguiente paso: **propón** (no invoques por tu cuenta) la skill `spec` con la ruta de la historia. `spec` decide si es un cambio acotado (diseño corto en el chat) o arquitectónico (spec en `docs/specs/`).
