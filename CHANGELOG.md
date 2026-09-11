# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2026-09-11

### Changed

- Dependencies brought current: `xunit.v3` 4.0.0, `Microsoft.Extensions.*` 10.0.12,
  `NSubstitute` 6.2.0, `ClosedXML` 0.105.1, `ScottPlot` 5.1.59.
- **Migrated the test suite to Microsoft.Testing.Platform.** xunit v3 4.0 runs on MTP, and
  the .NET 10 SDK no longer supports running MTP-based test projects through VSTest — the
  old setup fails outright. The runner is declared in `global.json`, test projects build as
  executables that host their own runner, and coverage comes from
  `Microsoft.Testing.Extensions.CodeCoverage` rather than `coverlet.collector`. CI collects
  Cobertura output as before.

## [1.0.0] - 2026-09-11

Initial release. One fluent builder API that produces a serializable document definition,
and eight packages that render it to DOCX, PDF, XLSX, PPTX, Markdown, CSV and charts.

### Packages

`FluentDocs` (umbrella, registers everything via `AddFluentDocs()`), `FluentDocs.Abstractions`,
`FluentDocs.Documents.Docx`, `FluentDocs.Documents.Pdf`, `FluentDocs.Documents.Markdown`,
`FluentDocs.Tables`, `FluentDocs.Presentations`, `FluentDocs.Charts`.

All eight are versioned and released together, so a fix in one renderer ships as one version
bump across the set.

### Notable behaviour

- **Every renderer formats with `CultureInfo.InvariantCulture`.** A report generated on a
  machine with a comma decimal separator is byte-identical to one generated anywhere else.
  This matters most for XLSX, where a locale-formatted number is silently stored as *text*
  rather than a number. CA1305 and CA1310 are build errors so it stays that way.
- Renderers return `GenerationResult.Failed` carrying a `GenerationError` rather than
  throwing, so one malformed input does not take down a batch of reports.
- `IColumnBuilder.AutoWidth` takes a parameter named `enabled`. `auto` is a reserved word in
  C++ and made the interface awkward to implement from other languages (CA1716).

### Build, CI and release

- .NET analyzers at `Recommended` with `TreatWarningsAsErrors`; every deliberately loosened
  rule is explained in `.editorconfig`. `NuGetAudit` runs in `all` mode at `low` level, so a
  dependency with a published advisory fails the build.
- MinVer derives the version from the git tag — there is no `<Version>` anywhere.
- Symbol packages, SourceLink and deterministic CI builds; per-package README, description
  and tags.
- CI builds and tests on Linux and Windows with coverage, and a pack job asserts the exact
  set of eight packages so a test or example project cannot silently become published.
- `tests/FluentDocs.Docs.Snippets` compiles every code sample in `README.md` and `docs/`, so
  a documented snippet cannot rot. It caught a wrong `AddColumn` overload in the README
  before this release.
- Releases are tag-driven and publish through NuGet Trusted Publishing (OIDC) behind a manual
  environment gate — no long-lived API key.
- CodeQL, gitleaks and Dependabot are enabled.

### Testing

149 tests across nine suites, including 78 integration tests that generate real DOCX, PDF,
XLSX and PPTX files and read them back.

[Unreleased]: https://github.com/deniscuciuc/fluentdocs/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/deniscuciuc/fluentdocs/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/deniscuciuc/fluentdocs/releases/tag/v1.0.0
