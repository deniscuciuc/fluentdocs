# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-09-11

First public release. Extracted from a private monorepo, relicensed under MIT, and renamed
from `QGCore.FluentFiles` — `FluentFiles` was already taken on nuget.org.

### Breaking

- Every namespace, package and assembly renamed from `QGCore.FluentFiles.*` to
  `FluentDocs.*`, and `AddFluentFiles()` to `AddFluentDocs()`.
- `QGCore.FluentFiles.DependencyInjection` is now simply **`FluentDocs`**. It was always the
  umbrella that referenced all seven renderers, and `dotnet add package FluentDocs` is what
  the old documentation already told people to type — for a package that did not exist.
- `IColumnBuilder.AutoWidth(bool auto = true)` is now `AutoWidth(bool enabled = true)`.
  `auto` is a reserved word in C++ and made the interface awkward to implement from other
  languages (CA1716).

### Fixed

- **Numbers and dates were formatted with the machine's current culture.** Every
  `ToString()` and interpolated `StringBuilder.Append` in the renderers now uses
  `CultureInfo.InvariantCulture`. On a machine with a comma decimal separator this produced
  XLSX cells that Excel stores as *text* rather than numbers, and DOCX and PPTX attribute
  values that are invalid OpenXML. Enforced from here on by CA1305 and CA1310 as build
  errors.
- **The test suite never ran.** The xUnit v3 test projects had no test runner referenced at
  all, so `dotnet test` discovered zero tests and exited successfully — in CI as well as
  locally. With `xunit.runner.visualstudio` added and xUnit bumped to 3.2.2, all **149 tests
  execute and pass**, including 78 integration tests that generate real files and read them
  back.
- `ScottPlotChartRenderer` never disposed the `Plot` it created, leaking the native drawing
  surface behind every rendered chart (CA2000).
- Public `GenerateAsync` and `RenderAsync` entry points now validate their arguments instead
  of surfacing a `NullReferenceException` from inside a renderer (CA1062).
- `ConfigureAwait(false)` is applied throughout `src/`, so generating a document from a UI
  thread cannot deadlock (CA2007).

### Added

- MIT license. The source repository claimed PolyForm Strict — source-available, not open
  source — and shipped no `LICENSE` file at all, so every package it produced had no license
  metadata.
- Package metadata on all eight packages: description, tags, per-package README, symbol
  packages (`.snupkg`), SourceLink and deterministic CI builds.
- MinVer tag-based versioning, replacing a hardcoded `<Version>` and a set of version-bumping
  shell scripts.
- .NET analyzers at `Recommended` with `TreatWarningsAsErrors`, and an `.editorconfig` that
  explains every deliberately loosened rule.
- `tests/FluentDocs.Docs.Snippets`, which compiles every code sample in `README.md` and
  `docs/` so a documented snippet cannot silently rot. It caught one wrong `AddColumn`
  overload in the README before this release.
- CI on Linux and Windows with coverage, a pack job that asserts the exact set of eight
  packages, CodeQL, gitleaks, Dependabot, `SECURITY.md`, `CODEOWNERS` and templates.
- Release via NuGet Trusted Publishing (OIDC) behind a manual environment gate — no
  long-lived API key.

### Removed

- Documentation describing publication to a private GitHub Packages feed.

[Unreleased]: https://github.com/deniscuciuc/fluentdocs/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/deniscuciuc/fluentdocs/releases/tag/v1.0.0
