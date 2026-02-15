# Changelog — HoneyDrunk.Data.Outbox.Dispatcher

## 0.3.0

- Canary test coverage for outbox concurrency invariants and transport boundary isolation.

## 0.2.0

- **Breaking:** Dispatch uses lease-based claiming instead of status-only.
- `OutboxDispatcherOptions.LeaseDuration` — configurable lease duration (default: 5 minutes).
- Failure handling: `ReleaseForRetryAsync` with exponential backoff when retries remain, `DeadLetterAsync` when exhausted.
- `MessagePoisoned` log renamed to `MessageDeadLettered`.
- Source-generated logging via `Log.cs`.
- Comprehensive test coverage (12 dispatcher tests).

## 0.1.0

- Initial release.
- `OutboxDispatcherService` — `BackgroundService` polling loop with exponential backoff.
- `OutboxDispatcherOptions` — batch size, poll interval, retry policy, default destination.
- `AddOutboxDispatcher()` — DI registration extension.
- Publishes via `ITransportPublisher` using `TransportEnvelope` + `EndpointAddress`.
