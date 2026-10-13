# .NET Clean Architecture — plantilla de arranque

> **Qué es esto.** Plantilla de referencia para arrancar **otro repositorio** con la misma arquitectura que Aurora Flowboard. No es una guía de trabajo para este repo: la solución ya está scaffoldeada, y para trabajar sobre ella se usan las skills `domain-entity`, `create-feature` y `unit-testing`.
>
> Se archivó aquí al retirar la skill `dotnet-clean-architecture`, porque describía un acto que solo ocurre una vez. Si arrancas un servicio nuevo, empieza por este documento y después reconstruye las convenciones leyendo `src/` de Aurora Flowboard.

Clean Architecture (también Onion o Hexagonal): separación por capas con dependencias unidireccionales hacia dentro.

## Capas

| Capa | Alcance |
|------|---------|
| Domain | Entidades, value objects, domain events, Result |
| Application | Commands, queries, handlers, validators, DTOs, behaviors |
| Infrastructure | EF Core, autenticación, servicios externos, cross-cutting |
| Api | Minimal APIs, middlewares, formato de respuesta |

**Regla de dependencia:** las dependencias apuntan hacia dentro. Domain no depende de nada. Application depende solo de Domain. Infrastructure implementa las interfaces declaradas en Application. API depende de Infrastructure.

## Estructura de proyectos

```
{SolutionName}/
├── src/
│   ├── {name}.Domain/
│   │   ├── Abstractions/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── BaseError.cs
│   │   │   ├── DomainEvent.cs        # IDomainEvent + DomainEvent
│   │   │   └── Result.cs
│   │   ├── {Aggregate}/
│   │   │   ├── {Entity}.cs
│   │   │   ├── {Entity}Errors.cs
│   │   │   └── Events/
│   │   ├── Shared/
│   │   │   └── {ValueObject}.cs
│   │   ├── GlobalUsings.cs
│   │   └── {name}.Domain.csproj
│   │
│   ├── {name}.Application/
│   │   ├── Abstractions/
│   │   │   ├── Behaviors/
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   ├── PerformanceBehavior.cs
│   │   │   │   └── ValidationBehavior.cs
│   │   │   ├── Messaging/
│   │   │   │   ├── ICommand.cs
│   │   │   │   ├── ICommandHandler.cs
│   │   │   │   ├── IDomainEventHandler.cs
│   │   │   │   ├── IQuery.cs
│   │   │   │   └── IQueryHandler.cs
│   │   │   ├── Time/
│   │   │   │   └── IDateTimeProvider.cs
│   │   │   ├── Validations/
│   │   │   │   ├── RuleBuilderOptionsExtensions.cs
│   │   │   │   └── ValidationError.cs
│   │   │   ├── Authentication/
│   │   │   ├── Data/
│   │   │   └── Exceptions/
│   │   ├── {Feature}/
│   │   │   ├── Create/
│   │   │   ├── Update/
│   │   │   ├── GetById/
│   │   │   └── GetAll/
│   │   ├── DependencyInjection.cs
│   │   ├── GlobalUsings.cs
│   │   └── {name}.Application.csproj
│   │
│   ├── {name}.Infrastructure/
│   │   ├── Authentication/
│   │   ├── Configurations/
│   │   │   └── {Entity}Configuration.cs
│   │   ├── Database/
│   │   │   ├── Migrations/
│   │   │   └── ApplicationDbContext.cs
│   │   ├── DomainEvents/
│   │   ├── Interceptors/
│   │   │   └── InsertOutboxMessagesInterceptor.cs
│   │   ├── Outbox/
│   │   ├── Time/
│   │   ├── DependencyInjection.cs
│   │   ├── GlobalUsings.cs
│   │   └── {name}.Infrastructure.csproj
│   │
│   └── {name}.Api/
│       ├── Endpoints/
│       │   └── {Feature}/
│       ├── Middlewares/
│       ├── Extensions/
│       ├── Responses/
│       ├── Program.cs
│       ├── DependencyInjection.cs
│       ├── GlobalUsings.cs
│       ├── appsettings.json
│       └── {name}.Api.csproj
│
├── test/
│   ├── {name}.Domain.UnitTests/
│   └── {name}.Application.UnitTests/
│
└── {SolutionName}.slnx
```

## Crear solución y proyectos

```bash
dotnet new sln -n {SolutionName}

dotnet new classlib -n {name}.Domain -o src/{name}.Domain
dotnet new classlib -n {name}.Application -o src/{name}.Application
dotnet new classlib -n {name}.Infrastructure -o src/{name}.Infrastructure
dotnet new webapi -n {name}.Api -o src/{name}.Api

dotnet sln add src/{name}.Domain/{name}.Domain.csproj
dotnet sln add src/{name}.Application/{name}.Application.csproj
dotnet sln add src/{name}.Infrastructure/{name}.Infrastructure.csproj
dotnet sln add src/{name}.Api/{name}.Api.csproj

cd src/{name}.Application
dotnet add reference ../{name}.Domain/{name}.Domain.csproj

cd ../{name}.Infrastructure
dotnet add reference ../{name}.Application/{name}.Application.csproj

cd ../{name}.Api
dotnet add reference ../{name}.Infrastructure/{name}.Infrastructure.csproj
```

> **Nunca uses un espacio en el nombre de un proyecto.** Un espacio en el destino de un `ProjectReference` rompe la resolución copy-local del SDK para los `PackageReference` *transitivos* de ese proyecto: `dotnet build` los copia bien, pero `dotnet publish` los descarta en silencio y solo aparece como `FileNotFoundException` en runtime.

## Convenciones de nombres

| Elemento | Convención | Ejemplo |
|------|------------|---------|
| Solución | PascalCase | `AuroraFlowboard` |
| Proyectos | PascalCase con puntos | `Aurora.Flowboard.Domain` |
| Namespaces | PascalCase | `Aurora.Flowboard.Domain.Users` |
| Clases | PascalCase | `WorkItem` |
| Interfaces | IPascalCase | `IDomainEvent` |
| Commands | `[Acción][Entidad]Command` | `CreateUserCommand` |
| Queries | `[Acción][Entidad]Query` | `GetUserByIdQuery` |
| Handlers | `[Acción][Entidad]Handler` | `CreateUserHandler` |
| Validators | `[Acción][Entidad]Validator` | `CreateUserValidator` |
| Responses | `[Entidad]Response` | `UserResponse` |
| Domain events | `[Entidad][Acción]DomainEvent` | `UserCreatedDomainEvent` |
| Errores | `[Entidad]Errors` | `UserErrors` |

## Reglas críticas

1. **Domain no tiene dependencias** — ni de otras capas ni de paquetes externos
2. **Application depende solo de Domain** — sin preocupaciones de infraestructura
3. **Infrastructure implementa las interfaces** declaradas en Application
4. **Api solo referencia Infrastructure**
5. **Inyección por constructor**, usando constructor primario
6. **Result pattern** en lugar de excepciones para errores de negocio
7. **Commands modifican estado, queries leen** (CQRS)
8. **Un handler por command/query** — nada compartido
9. **Sin repository pattern** — se permite usar la interfaz del DbContext en los handlers
10. **Los domain events se levantan en el dominio**, se manejan en Application
11. **Siempre `CancellationToken`** en métodos async
12. **`record` para DTOs** y datos inmutables
13. **Constantes**, nunca magic numbers ni strings

## Cadena de behaviors

Se registran con `Scrutor` mediante `services.Decorate(...)`, sin registro manual de cada handler. Cada `Decorate` envuelve al anterior, así que el orden de registro es el inverso al de ejecución:

```
LoggingBehavior → PerformanceBehavior → ValidationBehavior → handler
```

Logging envuelve a validation para que el log capture el round-trip completo, incluidos los fallos de validación. Decide explícitamente si `ValidationBehavior` decora también a los queries o solo a los commands — en Aurora Flowboard decora **ambos**.

## Errores frecuentes

- **N+1**: usa `.Include()` o joins explícitos
- **Over-fetching**: proyecta solo las columnas necesarias
- **Índices ausentes**: revisa los planes de ejecución y añade índices para los filtros comunes
- **Deadlocks**: no mezcles sync y async
- **Timeouts**: configura timeouts en los clientes HTTP
- **Cache stampede**: usa locks distribuidos al poblar la caché
