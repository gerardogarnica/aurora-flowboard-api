---
name: backlog
description: Define un feature del backlog a alto nivel (problema, usuarios, valor, alcance, prioridad, tamaño e historias candidatas), decide si amerita un Milestone en Flowboard y escribe docs/backlog/YYYY-MM-DD-<tema>-feature.md con la plantilla del repo. Úsala cuando el usuario traiga una idea, una necesidad o un feature que quiera registrar o priorizar antes de escribir historias o specs.
argument-hint: <idea o feature, en una o dos frases>
---

# Feature del backlog

Encargo: **$ARGUMENTS**

Habla con el usuario en español. Sigue `.claude/rules/documentation.md` (estructura, encabezado, estados, idioma).

Un feature es una apuesta, no un diseño: dice **qué** y **por qué**, nunca **cómo**. **No escribas código** ni hables de capas, tablas o endpoints mientras uses esta skill. Eso llega con `spec`.

**No llames a la API de Flowboard** ni pidas credenciales. La sección "Registro en Flowboard" deja los campos listos para que el usuario los copie a mano.

## 1. Contexto

- Lee el PRD (`docs/product/flowboard-prd.md`), sobre todo:
  - los *Non-Goals* (§3.2);
  - el glosario y los términos prohibidos en el MVP (*Epic*, *Sprint*, *Release*, *Initiative*…);
  - las mejoras futuras (§15).
- Mira `docs/backlog/` y `docs/reviews/backend-gap-analysis.md` por si el feature ya existe o se solapa con otro. Si se solapa, dilo y pregunta si se amplía el existente o se crea uno nuevo.
- Si el feature choca con un *Non-Goal* o usa un término prohibido, dilo explícitamente. No lo reformules en silencio: es una decisión de producto del usuario.

## 2. Lo mínimo que falta

Si el encargo no deja claro alguno de estos puntos, pregúntalos **en un solo mensaje**, con una línea por punto:

1. **Problema:** qué pasa hoy y por qué molesta.
2. **Usuarios:** qué rol lo necesita. Usa los nombres de Flowboard (`Administrator`/`Member`, o el `ProjectRole` del proyecto).
3. **Valor:** qué se gana y cómo se notaría.

No preguntes lo que puedas proponer tú (alcance, prioridad, tamaño, historias): propónlo en el borrador.

## 3. Borrador

Presenta en el chat el feature completo siguiendo `docs/templates/feature.md`, en versión corta:

- Problema, usuarios y valor, en una o dos frases cada uno.
- **Alcance:** qué entra y qué queda fuera, de forma explícita.
- **Prioridad** (Alta/Media/Baja) y **tamaño** (S/M/L), con una línea de motivo.
- **Historias candidatas:** solo títulos, de 1 a 6. Si salen más de 6, propone partir el feature.
- **¿Milestone?**, con la regla de la sección 4 y su motivo.

Termina con: "¿Qué cambias? Dímelo en una frase o responde "ok"."

Ajusta lo que pida el usuario y vuelve a mostrar solo lo que cambió.

## 4. ¿Milestone o no?

En Flowboard un `Milestone` es una iniciativa con alcance y fin dentro de un proyecto: responde "¿por qué se hace esto ahora?" (PRD §4). No es una épica ni un padre jerárquico de las historias.

- **Sí:** el feature tiene tamaño M o L, o tiene una fecha objetivo o un time-box. Se crea en `Draft` y pasa a `Active` cuando empiece el trabajo.
- **No:** el feature es tamaño S y sin fecha. Sus historias van como work items sin milestone, que el PRD trata como caso de primera clase. Escribe `No aplica` en `**Milestone:**` y el motivo en la sección 7.

Los campos sugeridos respetan los límites del dominio:
- nombre de hasta 100 caracteres, único en el proyecto;
- descripción de hasta 500 caracteres;
- fechas en `YYYY-MM-DD`.

## 5. Escritura

- Crea `docs/backlog/YYYY-MM-DD-<tema>-feature.md` a partir de `docs/templates/feature.md`.
  - `<tema>` va en kebab-case inglés.
  - `**Estado:** Borrador`.
  - `**Milestone:**` lleva `—` si todavía no se ha creado, o `No aplica`.
  - `**Historias:**` lleva `—`.
- Añade su fila en `docs/README.md`, tabla "Features", con `—` en la columna "Historias".

## 6. Revisión del usuario

- Pide al usuario que revise el fichero y aplica los cambios que pida.
- Con su aprobación:
  - cambia el encabezado a `**Estado:** Aprobado` y la columna "Estado" de su fila en `docs/README.md`;
  - si lleva milestone, recuérdale que lo cree en Flowboard con los campos de la sección 7 y que te diga el nombre final para ponerlo en `**Milestone:**`.
- No hagas commit salvo que te lo pida.
- Siguiente paso: **propón** (no invoques por tu cuenta) la skill `story` con la ruta del feature y la primera historia candidata.
