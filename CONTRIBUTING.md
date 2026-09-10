# Contributing to FluentDocs

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — the exact version is
  pinned in [`global.json`](global.json)
- An editor that honours `.editorconfig`

## Setup

```bash
git clone https://github.com/deniscuciuc/fluentdocs.git
cd fluentdocs
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Nothing external is required — the tests generate real DOCX, PDF, XLSX and PPTX files in
memory and read them back.

## Code style

Enforced by the build and by `dotnet format --verify-no-changes` in CI:

- `net10.0`, `LangVersion latest`, nullable reference types and implicit usings on
- `TreatWarningsAsErrors`, .NET analyzers at `Recommended`
- File-scoped namespaces; one namespace per project, matching the project name
- XML doc comments on public types and members
- `ArgumentNullException.ThrowIfNull` on every public entry point (CA1062 enforces this)
- `ConfigureAwait(false)` on every await in `src/` (CA2007 enforces this)
- **`CultureInfo.InvariantCulture` on every format and parse** (CA1305 and CA1310 enforce
  this). A locale-formatted number written into an XLSX cell is stored as text, silently —
  this is the single easiest way to break the library.

Deliberate deviations are listed in [`.editorconfig`](.editorconfig), each with the reason.

## Architecture

`FluentDocs.Abstractions` holds the builders and a serializable definition model. It knows
nothing about any file format. Each renderer package implements `IFileGenerator` for one
`GeneratedFileFormat` and depends only on Abstractions plus its own rendering library.

That split is the point: a definition can be built in one process, queued, and rendered in
another, and a new output format never requires touching the builder.

Renderers return `GenerationResult.Failed` rather than throwing, so a batch of reports does
not die on one malformed input. Reserve exceptions for programming errors — a null argument,
an unreachable switch arm.

## Adding a renderer

1. `dotnet new classlib -o src/FluentDocs.Documents.YourFormat -n FluentDocs.Documents.YourFormat`
2. Strip the generated `.csproj` to the SDK element, a `<Description>` and `<PackageTags>`,
   and its references. Everything else is inherited.
3. Add a `GeneratedFileFormat` value, implement `IFileGenerator`, and set `SupportedFormat`.
4. Add a `services.AddSingleton<IFileGenerator, YourFormatFileGenerator>()` registration
   extension, and call it from `AddFluentDocs()` in `src/FluentDocs`.
5. Add `src/FluentDocs.Documents.YourFormat/README.md` — every package ships its own.
6. Add the project to `FluentDocs.slnx`, **and to the expected package list in
   `.github/workflows/ci.yml`** — the pack job asserts the exact set.
7. Add `tests/FluentDocs.Documents.YourFormat.Tests`, plus a round-trip case in
   `tests/FluentDocs.IntegrationTests` that generates a file and reads it back.
8. Document it in `docs/` and add rows to the tables in `README.md`.

## Documentation

Any C# you put in `README.md` or `docs/` must also exist in
`tests/FluentDocs.Docs.Snippets`, which compiles in CI. A documented snippet that does not
compile is a bug, and this is what catches it.

## Pull requests

- Keep a PR to a single concern.
- New behaviour and bug fixes need tests.
- Conventional commits: `feat(tables): …`, `fix(pdf): …`, `docs(readme): …`.
- Note breaking changes in `CHANGELOG.md` under `## [Unreleased]`.
- CI must be green: format, build and test on Linux and Windows, and the pack assertion.

## Releasing

See [docs/release-process.md](docs/release-process.md). Releases are tag-driven; only
maintainers can approve the publish step.
