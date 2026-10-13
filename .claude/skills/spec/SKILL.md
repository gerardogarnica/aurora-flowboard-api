---
name: spec
description: Crea un spec de diseño con preguntas guiadas. Explora el código, redacta un borrador con supuestos numerados, refina solo los que el usuario elija, compara enfoques y escribe docs/specs/YYYY-MM-DD-<tema>-design.md con la plantilla del repo. Úsala cuando el usuario pida un spec, una especificación o un diseño, traiga una historia de usuario o un ticket que definir, o cuando review-fix clasifique un punto como arquitectónico.
argument-hint: <historia de usuario, ticket o punto de la revisión>
---

# Spec con preguntas guiadas

Encargo: **$ARGUMENTS**

Habla con el usuario en español. Sigue `.claude/rules/documentation.md` (estructura, encabezado, estados, idioma).

**No escribas código de producción** mientras uses esta skill. Termina con un spec aprobado; la implementación empieza después del plan.

## 1. Contexto

- Lee lo que trae el encargo y lo que lo rodea:
  - el PRD (`docs/product/`);
  - specs relacionadas (`docs/specs/`);
  - el código afectado y las rules de `.claude/rules/` de esas áreas.
- Si el **propósito** no está claro (para quién es, qué problema resuelve, cómo se sabe que está bien), haz primero una sola pregunta sobre eso. No propongas soluciones sin saberlo.
- Si el encargo mezcla varios subsistemas independientes, dilo y propón dividirlo; haz el spec del primero.
- **Si te invoca `review-fix`**, el encargo ya está verificado y clasificado como arquitectónico: no repitas su resumen ni la pregunta de propósito, salta el paso 2 y empieza en el paso 3. Usa la rama del encargo en el encabezado y los IDs de la revisión en `**Tickets:**`.

## 2. Clasificación

Dila en voz alta para que el usuario pueda corregirla:

- **Acotado:** cambia un flujo que ya existe (un handler, una entidad, un endpoint). No lleva spec: presenta un diseño corto en el chat y espera la aprobación. Con ella, esta skill termina y la implementación sigue con las skills del repo (`domain-entity`, `create-feature`, `unit-testing`) y el flujo de `CLAUDE.md`.
- **Arquitectónico:** feature nueva, varios agregados, un patrón nuevo o una migración compleja. Sigue con el paso 3.
- Si dudas, elige arquitectónico.

## 3. Borrador y supuestos

Presenta en el chat un borrador corto (objetivo, alcance, diseño a grandes rasgos) y, debajo, **todos** los supuestos numerados que no dice el encargo:

```
## Supuestos
1. [Funcional]  Un Viewer no puede ...
2. [Técnico]    El estado se guarda como string (HasConversion<string>) ...
3. [Alcance]    La notificación por email queda fuera (FLW-xx) ...
```

- Cada supuesto es concreto y verificable. Nada de "comportamiento estándar".
- Incluye supuestos funcionales, técnicos (capas, datos, errores y códigos HTTP, migración) y de alcance.
- Termina con: "¿Cuáles quieres cambiar? Responde con los números (p. ej. "2, 5") o "ninguno"."

## 4. Refinamiento

- Pregunta **solo** por los números elegidos, **de uno en uno**, con `AskUserQuestion`:
  - de 2 a 4 opciones concretas, una frase cada una;
  - la recomendada primero, con "(Recomendado)";
  - al menos una opción que sea el estándar de la industria;
  - la respuesta libre ya la añade la herramienta.
- Tras cada respuesta, confírmala en una línea y sigue; no vuelvas a mostrar todo el borrador.
- No añadas supuestos nuevos a mitad del refinamiento. Si una respuesta destapa algo bloqueante, dilo explícitamente y pregúntalo como un punto más.
- Al terminar, muestra la lista de supuestos actualizada y pregunta si queda alguno por revisar.

## 5. Enfoques

- Propón 2 o 3 enfoques con ventajas e inconvenientes. Empieza por el recomendado y explica por qué.
- Aplica YAGNI: quita de cada enfoque lo que el objetivo no necesita.
- Espera a que el usuario elija.

## 6. Diseño por secciones

- Antes de diseñar las capas, lee las skills que se usarán al implementar, para que el diseño ya siga sus convenciones (no las ejecutes: aquí no se escribe código):
  - `.claude/skills/domain-entity/SKILL.md` y sus referencias `aggregate-root.md`, `errors-and-events.md` y `ef-configuration.md` (la tabla "Persistence consequences" decide el mapping y la sección 8 del spec);
  - `.claude/skills/create-feature/SKILL.md` y sus referencias `command-slice.md`, `query-slice.md` y `endpoint.md` (comando o consulta, nombres, carpeta por caso de uso, endpoint).
  - Usa en el spec sus nombres exactos: `{Verb}{Entity}Command`, `{Verb}{Entity}Handler`, `{Entity}Errors.{Code}`, `Endpoints/{Feature}/{UseCase}.cs`.
- Presenta el diseño en el orden de la plantilla: capas (Domain, Application, Infrastructure, API), errores y códigos HTTP, datos y migraciones, tests.
- Ajusta la extensión a la complejidad: unas frases si es directo, hasta 200–300 palabras si tiene matices.
- Pregunta tras cada sección si va bien. Vuelve atrás si algo no encaja.

## 7. Escritura

- Crea `docs/specs/YYYY-MM-DD-<tema>-design.md` a partir de `docs/templates/spec.md`:
  - `<tema>` en kebab-case inglés, igual que la rama;
  - `**Estado:** Borrador` y el resto del encabezado completo;
  - los supuestos finales van a la sección 4 y los enfoques a la sección 5;
  - las secciones que no apliquen se quedan con una línea de motivo.
- Añade la fila del spec en `docs/README.md` (tabla "Specs y planes"), con la columna "Plan" en `—`: la rellena `write-plan`.

## 8. Autorrevisión

Revisa el fichero y corrige sin preguntar:

1. **Marcadores:** ningún "TBD", "TODO", sección vacía ni requisito vago.
2. **Coherencia:** las secciones no se contradicen y el diseño cumple el objetivo.
3. **Supuestos:** cada respuesta del paso 4 está reflejada.
4. **Alcance:** lo excluido indica dónde se cubrirá; el spec cabe en un solo plan (si no, propone dividirlo).
5. **Ambigüedad:** ningún requisito admite dos lecturas; si alguno sí, elige una y escríbela.
6. **Convenciones:** el diseño no contradice `domain-entity` ni `create-feature` (nombres, visibilidad, errores como `Result`, mapping, comando frente a consulta).
7. **ADR:** si alguna decisión trasciende esta feature (un patrón transversal, una librería nueva, una convención que afecta a otros agregados), propón al usuario registrarla como ADR en `docs/architecture/adr/` con `docs/templates/adr.md`, enlazado desde el spec.

## 9. Revisión del usuario

- Pide al usuario que revise el fichero. Si pide cambios, aplícalos y repite el paso 8.
- Con su aprobación, cambia el encabezado a `**Estado:** Aprobado` y la columna "Estado" de su fila en `docs/README.md`.
- No hagas commit salvo que te lo pida.
- Siguiente paso, **automático**: en cuanto el usuario apruebe el spec, invoca la skill `write-plan` con la ruta del spec, sin preguntar. Escribir el plan no toca código, y la siguiente aprobación del usuario es la del plan. `write-plan` lo guarda en `docs/plans/` y lo enlaza desde el encabezado del spec (`**Plan:**`).
