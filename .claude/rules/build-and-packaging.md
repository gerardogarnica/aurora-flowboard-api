---
paths:
  - "**/*.csproj"
  - "*.slnx"
  - "**/Dockerfile"
  - "Directory.Build.props"
  - "Directory.Packages.props"
---

# Build and packaging

## No spaces in project names

All project folder/file names use the dot-separated `Aurora.Flowboard.*` convention. Do not reintroduce a space in a project name: a space in a `ProjectReference`'s target breaks the .NET SDK's publish-time copy-local resolution for that project's *transitive* `PackageReference`s. `dotnet build` copies them fine, but `dotnet publish` silently drops them, which only surfaces as a `FileNotFoundException` at runtime in the published/container image.

## Central package management

`Directory.Packages.props` manages all NuGet versions centrally — never add `Version` attributes to individual `.csproj` files. `Directory.Build.props` treats warnings as errors and enables SonarAnalyzer.
