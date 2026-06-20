# Changelog

All notable changes to the HoneyDrunk.Data repository are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Per-package detail lives in the solution-level changelog at
[`HoneyDrunk.Data/CHANGELOG.md`](HoneyDrunk.Data/CHANGELOG.md) and in each
package's own `CHANGELOG.md`.

## [Unreleased]

## [0.7.0] - 2026-05-27 - Outbox hardening and SonarCloud onboarding

### Added

- `IUnitOfWork<TContext>` exposes a `Type ContextType { get; }` default interface member so the `TContext` marker is observable at runtime for diagnostics and tests.
- Onboarded the repository to SonarQube Cloud (ADR-0011) via a `sonarcloud` job in `pr.yml`.
- Enabled ADR-0044 Grid Review Runner request generation for repository PRs.

### Changed

- `CorrelationCommandInterceptor` SQL-comment sanitization switched from a deny-list to a strict allow-list (`[A-Za-z0-9_-]`, 128-char cap), preventing escape of the `/* correlation:<id> */` envelope.
- Reduced cognitive complexity in `ModelBuilderConventions.ApplySnakeCaseNamingConvention` and simplified `SqliteTestDatabase` disposal guards.
- Bumped Kernel to `0.8.0`, Vault packages to `0.7.0`, Transport to `0.7.1`, and EF Core / Microsoft.Extensions packages to `10.0.8`.

## [0.6.0] - 2026-05-18 - Registration consolidation

### Changed

- Aligned package versions to `0.6.0` and consolidated duplicated EF registration, SQL provider option, and SQLite test database lifecycle helpers.

### Fixed

- Outbox context autopopulation now fails fast when operation context is missing or incomplete unless explicit correlation and tenant ids are provided.

## [0.5.1] - 2026-05-04 - Package metadata fix

### Fixed

- Corrected NuGet package release metadata across the Data solution after the 0.5.0 typed tenant adoption release.

## [0.5.0] - 2026-05-04 - Typed tenant context

### Changed

- Adopted Kernel 0.5.0 typed tenant context values across Data's Kernel context adapters while preserving Data's public tenant abstractions.

## [0.4.0] - 2026-04-25 - Vault-driven bootstrap

### Added

- Env-driven Key Vault and App Configuration bootstrap wiring (ADR-0005/0006).
- `HoneyDrunk.Data.AspNetCore` package with Event Grid invalidation service and endpoint helpers.

### Changed

- SQL Server registration now resolves connection strings through `ISecretStore` per DbContext resolution.

## [0.3.0] - 2026-02-15 - Canary invariant tests

### Added

- `HoneyDrunk.Data.Canary` project with 18 CI invariant tests covering Kernel context, outbox concurrency, and transport boundary isolation.

## [0.2.0] - 2026-01-06 - Orchestration restructure

### Added

- Comprehensive documentation suite under `HoneyDrunk.Data/docs/`.
- Improved configuration validation.

### Changed

- Restructured the orchestration layer for improved Kernel integration.

## [0.1.0] - 2026-01-01 - Initial release

### Added

- Initial release of HoneyDrunk.Data: repository contracts, unit of work, tenant identity access, EF Core integration, SQL Server provider, transactional outbox, migration infrastructure, and testing utilities.

[0.7.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.7.0
[0.6.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.6.0
[0.5.1]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.5.1
[0.5.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.5.0
[0.4.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.4.0
[0.3.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.3.0
[0.2.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.2.0
[0.1.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.1.0
