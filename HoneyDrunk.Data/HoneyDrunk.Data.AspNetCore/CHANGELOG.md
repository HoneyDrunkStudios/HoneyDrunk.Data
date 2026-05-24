# Changelog

All notable changes to HoneyDrunk.Data.AspNetCore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [Unreleased]

### Changed


- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

## [0.6.0] - 2026-05-18

### Changed


- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- Package version bumped to `0.6.0` and Kernel/Vault EventGrid references aligned with the current Core release train.

## [0.5.1] - 2026-05-04

### Fixed

- Corrected NuGet package release metadata for the 0.5.x release train.

## [0.4.0] - 2026-04-25

### Added

- Initial package with `AddHoneyDrunkDataAspNetCore()` and `MapHoneyDrunkDataVaultInvalidationWebhook()` for ADR-0006 Event Grid cache invalidation.
