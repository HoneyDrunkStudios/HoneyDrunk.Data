# HoneyDrunk.Data agent instructions

Own persistence conventions, EF implementation and transactional outbox storage. Applications retain business decisions and explicit tenant filtering. Preserve transaction, lease, replay and concurrency behavior; tracking and ordinary CRUD do not decide product history.

Start with [README.md](README.md) and the relevant source/tests. Project files and lockfiles own SDK, framework and dependency versions.

Read the [shared engineering conventions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md) and this repository's owning documentation before editing. Apply the parts relevant to this stack; preserve existing public contracts, dependency direction and repository-specific behavior. Verify shared capabilities in current code before reusing them; a catalog entry or scaffold is not an implemented integration.

Work within the selected request. Preserve unrelated changes and use a separate worktree when needed. Review the final diff, use Conventional Commits and ready-for-review PRs with exactly one accurate `Authorship:` line and a `Request:` line; include the authorship in commit trailers. Run meaningful checks for the affected behavior and report the reviewed/tested revision, failures and unrun checks. For documentation-only changes, check links, paths and instruction consistency. Preserve required checks and inspect actual latest-head Sonar new-code findings where analysis applies; do not suppress findings or weaken gates to obtain a pass. Legacy Grid Review is retired; do not restore its workers, queues or bypass labels. A configured replacement reviewer is not evidence of a completed review or enforcing merge check.

## Verification

From the repository root for code/build changes:

```sh
dotnet restore HoneyDrunk.Data/HoneyDrunk.Data.slnx
dotnet build HoneyDrunk.Data/HoneyDrunk.Data.slnx -c Release --no-restore
dotnet test HoneyDrunk.Data/HoneyDrunk.Data.slnx -c Release --no-build
```

Use the checked-in workflow and relevant test documentation for additional integration prerequisites, coverage and consumer checks. Do not use live resources or credentials merely to make a local check pass.

## Code Review Rules

Apply the [shared review criteria](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md#code-review) to changed behavior, using the repository boundaries above. Report actionable findings with the failing path, concrete impact and a small corrective action; disclose unavailable evidence. These rules grant no cross-repository access or merge authority.

- Keep persistence queries/transactions and shared outbox storage mechanics here; applications retain business decisions and explicit tenant filtering. Preserve contracts/implementation dependency direction and reuse existing EF/outbox seams.
- Trace tracked updates, transaction ownership, leases, rowversion, replay and cancellation under partial failure or concurrency. Flag unintended writes, cross-tenant queries, N+1/unbounded access and unsafe schema changes using a concrete operation.
- Require focused persistence/contract and contention/replay tests for changed behavior. Check effective EF/schema parity, justified indexes and table/column metadata; keep SQL-project ownership distinct from EF migrations and do not replace tracked updates with generic mutation bindings.
