# Changelog - HoneyDrunk.Data.Outbox.Dispatcher

## [Unreleased]

### Changed


- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

## 0.6.0

- Package version bumped to `0.6.0` and Transport reference aligned to `0.6.0`.

## 0.5.1

- Corrected NuGet package release metadata for the 0.5.x release train.

## 0.4.0

- Coordinated package train bump for the ADR-0005/0006 Data rollout.

## 0.3.0

- Canary test coverage for outbox concurrency invariants and transport boundary isolation.

## 0.2.0

- **Breaking:** Dispatch uses lease-based claiming instead of status-only.
- `OutboxDispatcherOptions.LeaseDuration` - configurable lease duration (default: 5 minutes).
- Failure handling: `ReleaseForRetryAsync` with exponential backoff when retries remain, `DeadLetterAsync` when exhausted.
- `MessagePoisoned` log renamed to `MessageDeadLettered`.
- Source-generated logging via `Log.cs`.
- Comprehensive test coverage (12 dispatcher tests).

## 0.1.0

- Initial release.
- `OutboxDispatcherService` - `BackgroundService` polling loop with exponential backoff.
- `OutboxDispatcherOptions` - batch size, poll interval, retry policy, default destination.
- `AddOutboxDispatcher()` - DI registration extension.
- Publishes via `ITransportPublisher` using `TransportEnvelope` + `EndpointAddress`.
