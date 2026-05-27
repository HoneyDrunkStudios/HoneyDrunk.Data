# HoneyDrunk.Data - Repository Changelog

All notable changes to the HoneyDrunk.Data repository will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

**Note:** See individual package CHANGELOGs for detailed changes:
- [HoneyDrunk.Data.Abstractions CHANGELOG](HoneyDrunk.Data.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Data CHANGELOG](HoneyDrunk.Data/CHANGELOG.md)
- [HoneyDrunk.Data.AspNetCore CHANGELOG](HoneyDrunk.Data.AspNetCore/CHANGELOG.md)
- [HoneyDrunk.Data.EntityFramework CHANGELOG](HoneyDrunk.Data.EntityFramework/CHANGELOG.md)
- [HoneyDrunk.Data.SqlServer CHANGELOG](HoneyDrunk.Data.SqlServer/CHANGELOG.md)
- [HoneyDrunk.Data.Migrations CHANGELOG](HoneyDrunk.Data.Migrations/CHANGELOG.md)
- [HoneyDrunk.Data.Outbox CHANGELOG](HoneyDrunk.Data.Outbox/CHANGELOG.md)
- [HoneyDrunk.Data.Outbox.Abstractions CHANGELOG](HoneyDrunk.Data.Outbox.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Data.Outbox.Dispatcher CHANGELOG](HoneyDrunk.Data.Outbox.Dispatcher/CHANGELOG.md)
- [HoneyDrunk.Data.Testing CHANGELOG](HoneyDrunk.Data.Testing/CHANGELOG.md)

---

## [Unreleased]

## [0.7.0] - 2026-05-27

### Changed (breaking)

- **`IUnitOfWork<TContext>` now exposes a `Type ContextType { get; }` default interface member.** The `TContext` marker had no member referencing it, which Sonar S2326 flags as "unused type parameter." The DIM surfaces the marker at runtime for diagnostics and assertion in tests without forcing any implementer change (source-compatible for all callers; binary-compatible because DIMs are dispatched off the interface). The composition pattern `IUnitOfWork<IAuditDataContext>` (Audit) and similar continue to work unchanged.
- **Package versions bumped** to `HoneyDrunk.Data* 0.7.0` per pre-1.0 semver.

### Changed

- `CorrelationCommandInterceptor.SanitizeForSqlComment` switched from a deny-list (strip `*/`, `/*`, `--`, `\n`, `\r`) to a strict allow-list (`[A-Za-z0-9_-]`) with a 128-character cap, the canonical correlation-ID alphabet (RFC 4122 UUIDs, W3C trace-ids, ULIDs). Any other byte — including newlines, quotes, semicolons, and SQL block-comment terminators — is silently dropped, so the assembled `/* correlation:<id> */` envelope cannot be escaped regardless of upstream input. Sonar Security Hotspot (CorrelationCommandInterceptor.cs SQL injection review) addressed; CA2100 suppression retained with the allow-list justification because the analyzer cannot see through the sanitizer. Two new `Theory` cases (`;DROP TABLE Users`, `'OR'1'='1`) plus three new `Fact` tests cover UUID preservation, length cap, and the all-stripped path.
- `ModelBuilderConventions.ApplySnakeCaseNamingConvention` cognitive complexity 18 → under 15 (Sonar S3776). Extracted `ApplySnakeCaseToEntity` and `RenameIfPresent` helpers; per-entity nested loops moved out of the public extension method.
- `SqliteTestDatabase<TContext>.ThrowIfDisposed` uses `ObjectDisposedException.ThrowIf(_disposed, this)` instead of an if/throw branch (Sonar S6966).

### Tests

- 7 Sonar Blocker S2699 ("Tests should include assertions") findings cleared by wrapping the "does not throw" bodies in `Record.Exception` / `Record.ExceptionAsync` + `Assert.Null(exception)`:
  - `EfUnitOfWorkTests.TransactionScope_RollbackAsync_RollsBackChanges`, `EfUnitOfWorkTests.DisposeAsync_CalledMultipleTimes_DoesNotThrow`
  - `SqliteTestDbContextFactoryTests.DisposeAsync_ClosesConnection`, `SqliteTestDbContextFactoryTests.DisposeAsync_CalledMultipleTimes_DoesNotThrow`
  - `SqliteDbContextFixtureTests.DisposeAsync_CalledMultipleTimes_DoesNotThrow`, `SqliteDbContextFixtureTests.Dispose_CleansUpResources`
  - `DatabaseResetHelperTests.DetachAllEntities_WithNoTrackedEntities_DoesNotThrow`

### Internal

- Onboarded Data to SonarQube Cloud (ADR-0011 D11). Wired a `sonarcloud` job in `pr.yml` that calls `HoneyDrunkStudios/HoneyDrunk.Actions/.github/workflows/job-sonarcloud.yml` on both `pull_request` (after `pr-core` succeeds) and `push` to `main` (standalone). PR analysis gates the merge on new-code findings; main-branch analysis populates the SonarCloud Overview dashboard and the leak-period baseline. Per-project source/test classification is discovered automatically from MSBuild `IsTestProject` properties; per-repo Sonar overrides can be added later via `Directory.Build.props` `<SonarQubeSetting>` items or as new inputs to `job-sonarcloud.yml`. Branch-protection requirement added separately after the first successful run lands.
- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- Adopted HoneyDrunk.Standards.Tests 0.2.9 for Data test/canary/testing projects and refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing alignment.
- Backfilled Data outbox registration test coverage above the Grid PR coverage gate floor.
- Seeded the Data coverage baseline and wired the push-to-main coverage baseline ratchet.
- Bumped `HoneyDrunk.Kernel` / `HoneyDrunk.Kernel.Abstractions` `0.7.0 → 0.8.0`.
- Bumped `HoneyDrunk.Vault` / `HoneyDrunk.Vault.Providers.AppConfiguration` / `HoneyDrunk.Vault.Providers.AzureKeyVault` / `HoneyDrunk.Vault.EventGrid` `0.5.0 → 0.7.0` (Vault 0.6.0 SonarCloud onboarding + 0.7.0 DIM promotion).
- Bumped `HoneyDrunk.Transport` `0.6.0 → 0.7.1`.
- Bumped `Microsoft.EntityFrameworkCore` / `.Sqlite` / `.SqlServer` / `.Relational` / `.Design` / `.InMemory` and `Microsoft.Extensions.DependencyInjection` / `.DependencyInjection.Abstractions` / `.Hosting` / `.Hosting.Abstractions` / `.Logging.Abstractions` / `.Options` `10.0.7 → 10.0.8`.

## [0.6.0] - 2026-05-18

### Changed

- Aligned Data package versions to `0.6.0`, Kernel dependencies to `0.7.0`, Transport dependency to `0.6.0`, and Vault dependencies to `0.5.0`.
- Consolidated duplicated EF registration, SQL provider option, and SQLite test database lifecycle helpers.
- Release notes now include the Outbox package family.

### Fixed

- Outbox context autopopulation now fails fast when operation context is missing or incomplete unless explicit correlation and tenant ids are provided.

## [0.5.1] - 2026-05-04

### Fixed

- Corrected NuGet package release metadata across the Data solution after the 0.5.0 typed tenant adoption release.

## [0.5.0] - 2026-05-04

### Changed

- Adopted Kernel 0.5.0 typed tenant context values across Data's Kernel context adapters while preserving Data's public tenant abstractions.
- Coordinated all project versions to `0.5.0` and Kernel package references to `0.5.0`.

## [0.4.0] - 2026-04-25

### Added

- ADR-0005/0006 bootstrap wiring for HoneyDrunk.Data via env-driven Key Vault and App Configuration helpers.
- `HoneyDrunk.Data.AspNetCore` package with Event Grid invalidation service and endpoint helpers.
- Provider-grouped SQL secret naming with the `Sql--{Purpose}Connection` convention.
- Coordinated all package project versions to `0.4.0`.

### Changed

- SQL Server registration now resolves connection strings through `ISecretStore` per DbContext resolution instead of accepting raw connection strings.
- Migration design-time factories now bootstrap Vault from `AZURE_KEYVAULT_URI` and resolve `Sql--MigrationConnection` through `ISecretStore`.

## [0.3.0] - 2026-02-15

### Added

- `HoneyDrunk.Data.Canary` project with 18 CI invariant tests covering Kernel context, outbox concurrency, and transport boundary isolation

## [0.2.0] - 2026-01-06

### Changed

- Complete restructure of the orchestration layer for improved Kernel integration
- Enhanced `KernelTenantAccessor` with better null handling
- Improved `KernelDataDiagnosticsContext` with additional telemetry tags
- Refined `DataActivitySource` with more granular activity types

### Added

- Comprehensive documentation suite in `/docs` folder
- Improved configuration validation

## [0.1.0] - 2026-01-01

### Added

- Initial release of HoneyDrunk.Data
- `DataOptions` configuration class for data layer settings
- `KernelTenantAccessor` integrating with `IOperationContextAccessor`
- `KernelDataDiagnosticsContext` for telemetry enrichment from Kernel context
- `DataActivitySource` for creating telemetry activities
- `AddHoneyDrunkData()` extension method for service registration
- Entity Framework Core integration with `HoneyDrunkDbContext`
- SQL Server provider with connection-string resolution
- Transactional outbox pattern for reliable message publishing
- Migration infrastructure with `MigrationRunner`
- Testing utilities in `HoneyDrunk.Data.Testing`

[0.7.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.7.0
[0.6.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.6.0
[0.5.1]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.5.1
[0.5.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.5.0
[0.4.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.4.0
[0.3.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.3.0
[0.2.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.2.0
[0.1.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.1.0
