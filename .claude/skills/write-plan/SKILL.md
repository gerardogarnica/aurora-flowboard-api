---
name: write-plan
description: Escribe el plan de implementación de un spec aprobado. Lo divide en tareas commiteables con TDD, código completo, comandos de verificación y la skill del repo que implementa cada tarea, y lo guarda en docs/plans/YYYY-MM-DD-<tema>-plan.md con la plantilla del repo. Úsala cuando el usuario pida el plan de un spec, o al terminar la skill spec.
argument-hint: <ruta del spec, p. ej. docs/specs/2026-10-20-optimistic-concurrency-design.md>
---

# Escribir un plan de implementación

Spec: **$ARGUMENTS**

Habla con el usuario en español. Sigue `.claude/rules/documentation.md` (estructura, encabezado, estados, idioma). El plan lo ejecutará la skill `execute-plan`, posiblemente en otra sesión y sin el contexto de esta: todo lo que necesite tiene que estar escrito en el plan.

**No escribas código de producción** mientras uses esta skill: el código va dentro del plan, no en `src/`.

<!-- Inspirado en el método de superpowers (MIT, Jesse Vincent). Texto propio. -->

## 1. Entrada

- Lee el spec completo. Si su encabezado no dice `**Estado:** Aprobado`, para y dilo: el plan se escribe sobre un diseño aprobado.
- Si no se indicó ruta, busca en `docs/specs/` el spec aprobado más reciente sin `**Plan:**` y confírmalo con el usuario.

## 2. Contexto

- Lee las rules de `.claude/rules/` de las áreas afectadas y las skills que se usarán al implementar: `domain-entity`, `create-feature` y `unit-testing` (con sus referencias). Los nombres de ficheros, tipos, tests y carpetas del plan tienen que ser los que esas skills esperan.
- Lee el código que se va a tocar: firmas, builders de test existentes (`*Data.cs`), helpers como `MockDbSetHelper`.

## 3. Mapa de ficheros

Antes de dividir en tareas, decide qué ficheros se crean y cuáles se modifican, con la responsabilidad de cada uno, y escríbelo en la sección `## Mapa de ficheros` del plan (quien lo ejecute empieza sin contexto y lo necesita). Un fichero, una responsabilidad; si uno ya existente crece demasiado, dilo en el plan en vez de repartirlo por tu cuenta.

## 4. Tareas

Crea `docs/plans/YYYY-MM-DD-<tema>-plan.md` a partir de `docs/templates/plan.md`, con la misma fecha y tema que el spec y `**Estado:** Borrador`.

- **Una tarea es una unidad commiteable**: p. ej. un método de dominio y sus tests, un handler y sus tests, un endpoint, una configuración de EF y su migración, una actualización de docs. Orden habitual: dominio → application → infraestructura → API → docs.
- Cada tarea lleva:
  - **Skill:** `domain-entity`, `create-feature`, `unit-testing` o `—`.
  - **Ficheros:** rutas exactas a crear, modificar y testear.
  - Pasos de 2 a 5 minutos, en orden de TDD:
    1. el test que falla, con su **código completo**;
    2. el comando para verlo fallar (`<ejecutable>.exe -class "Namespace.ClassName"`) y el fallo esperado; si es un error de compilación, el stub que hace falta para ver fallar la assertion;
    3. la implementación, con su **código completo**;
    4. el comando para verlo pasar;
    5. `Commit` con el mensaje exacto (en inglés, imperativo) y los ficheros a añadir.
- Nada de "añadir validación", "manejar errores" o "similar a la tarea 3": escribe el código. Si una tarea repite un patrón, repite el código.
- Lo que no tiene proyecto de tests (Api, seeder, `Program.cs`) lleva pasos de verificación manual exactos: SQL, petición HTTP, resultado esperado.
- Toda tarea que toque código no cubierto por el ejecutable de tests que usa (Api, Infrastructure) incluye antes del commit `dotnet build <ruta del .csproj>`: un `-class` solo compila el proyecto de tests y sus dependencias, y `CLAUDE.md` exige compilar tras cada cambio.
- La sección "Verificación final" de la plantilla se completa con lo que aplique al plan.

## 5. Autorrevisión

Revisa el fichero y corrige sin preguntar:

1. **Cobertura:** cada criterio de éxito (sección 2 del spec) y cada elemento del diseño (secciones 6–9) tiene al menos una tarea. Lista los que no y añade tareas.
2. **Marcadores:** ningún "TBD", "TODO", "etc." ni paso sin código o sin comando.
3. **Consistencia:** los nombres de tipos, métodos, errores y ficheros coinciden entre tareas y con el spec.
4. **Skills:** todas las tareas tienen su línea `**Skill:**` y respetan las convenciones de esa skill.
5. **Tamaño:** ninguna tarea mezcla capas que convenga commitear por separado.

## 6. Cierre

- Enlaza el plan desde el encabezado del spec (`**Plan:**`) y, en la tabla "Specs y planes" de `docs/README.md`, rellena la columna "Plan" de la fila que ya creó `spec` (no añadas otra fila; si no existe, créala).
- Pide al usuario que lo revise. Si pide cambios, aplícalos y repite el paso 5.
- Con su aprobación, cambia el encabezado a `**Estado:** Aprobado`. **Aprobar el plan autoriza un commit por tarea durante su ejecución**; nunca push ni PR sin que el usuario lo pida.
- No hagas commit del plan salvo que el usuario lo pida.
- Siguiente paso, **solo a petición del usuario**: no invoques `execute-plan` por tu cuenta, porque escribe código y hace commits. Tras la aprobación, ofrécelo y espera su respuesta: "¿Lo ejecuto aquí o prefieres una sesión nueva con `/execute-plan <ruta del plan>`?". Si el plan es largo, recomienda la sesión nueva, que empieza con el contexto limpio.
