---
paths:
  - "test/Aurora.Flowboard.ArchitectureTests/**/*.cs"
  - "Directory.Packages.props"
---

# Architecture test conventions

Stack: xUnit **v3** + NetArchTest.Rules + Shouldly (**not** FluentAssertions — that stays in the two unit test projects). One file per layer: `DomainLayerTests`, `ApplicationLayerTests`, `ApiLayerTests`, plus `LayerDependencyTests` for the inter-assembly rules. The project references only `Aurora.Flowboard.Api`; `BaseTest` exposes the four assemblies.

- **`.Or()` starts a new predicate sequence.** A later `.And()` applies only to the *last* sequence, so `.ImplementInterface(A).Or().ImplementInterface(B).And().AreNotAbstract()` leaves branch A unfiltered. Write one test per interface instead of chaining with `.Or()`; that is why `Command*`/`CommandHandler*` tests come in `X` / `XWithResponse` pairs.
- **Never use `.BeImmutable()` on records.** `init` accessors compile to non-readonly backing fields, so every record is reported as mutable. Detect a record by the synthesized `<Clone>$` method instead.
- **`Type.Name` carries the generic arity** (``PagedResponse`1``), so trim at the backtick before any suffix check.
- **A reflection test that selects zero types passes silently.** When adding one, verify the selector actually matches something before trusting the green.
- The NetArchTest condition is `OnlyHaveDependenciesOn` (plural). It cannot express "Domain has no third-party dependencies" — `Milestone` depends on the namespace-less `<PrivateImplementationDetails>` that Roslyn emits for its `Transitions` dictionary, and that type matches no search term. `Domain_Should_OnlyReference_FrameworkAssemblies` uses `Assembly.GetReferencedAssemblies()` instead, which is stricter and immune to compiler artifacts.
- `ApiLayerTests` scans the IL of `MapEndpoint` to assert every endpoint calls `RequireAuthorization` or `AllowAnonymous`, using **Mono.Cecil**, which is only a *transitive* dependency of `NetArchTest.Rules` — **pin it explicitly in `Directory.Packages.props` before bumping NetArchTest.**
