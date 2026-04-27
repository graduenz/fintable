# Testing Guidelines

## Implementing Tests

- All tests should be added to the `Fintable.Tests` project and should use xUnit v3 as the test framework.
- Unit tests should be written using the AAA pattern (including comments), use Moq for mocking, and Bogus for creating test data.
- Unit tests should always target 100% code coverage.
- Integration tests for controllers should inherit `BaseControllerTests` in order to set up and make requests to the API.
- One assertion theme per test (avoid multiple unrelated behaviors in one test).
- Class-oriented tests: unit tests should have one test class per tested class.
- Avoid xUnit1051 warning: for async method calls in tests, use `TestContext.Current.CancellationToken` as the cancellation token parameter when possible.

## Test Naming Convention

Pattern: `MethodName_Scenario_ExpectedResult`

- Start with the member under test (`MethodName`).
- Scenario segment should describe the single condition being tested.
- Expected segment should describe the observable outcome.
- Use present tense and behavior-focused wording.
- Keep controller/integration tests aligned with HTTP intent.
- For `[Theory]`, keep the method name generic and move variations to `InlineData`.

### Examples

| Type | Example |
|---|---|
| Unit test | `GetRequiredKeys_UnknownType_ReturnsNull` |
| Unit test (input variants) | `GetYearRanges_NullReferenceDate_UsesCurrentYear` |
| Integration endpoint test | `Delete_ExistingProvider_ReturnsNoContent` |
| Validation/business rule test | `Validate_MissingMetadata_ReturnsIsFullySetUpFalse` |

## Integration Test Assertions (Required)

Every integration test must assert all applicable layers of behavior:

1. **HTTP status code** — assert the exact expected status (`200`, `201`, `204`, `400`, `404`, etc.).
2. **Response payload contract** — assert response body shape/contract using DTO deserialization. Validate key fields relevant to the scenario (ids, names, flags, collections, required keys, etc.). For error responses, assert expected problem/detail payload when applicable.
3. **Persistent database effect** — assert database state changes caused by the request (created/updated/deleted rows and important field values). Before DB verification after HTTP calls, clear EF tracking (`Db.ChangeTracker.Clear()`) to avoid false positives from tracked entities.

Apply by endpoint type:

| Method | Required assertions |
|---|---|
| GET | Status + payload contract |
| POST | Status + payload contract + created entity persisted correctly |
| PUT/PATCH | Status + payload contract + updated persisted state |
| DELETE | Status + payload contract (if any) + entity removed (or soft-delete state persisted) |

For endpoints that mutate a resource (POST, PUT, PATCH, DELETE), if a GET endpoint is available to fetch the resource, include a GET request in the assertions to verify the mutation.

Do not consider an integration test complete if it validates only the status code without payload and persistence checks where relevant.

## Test Verification (Build + Test)

After adding or changing tests, always validate the solution with:

```shell
dotnet build
dotnet test
```

- Treat a change as incomplete until both commands succeed.
- If tests fail, fix the root cause and re-run until green.
- For bug fixes and refactors, prefer running the full suite (`dotnet test`) instead of only targeted tests before finalizing.

## Local Coverage Workflow

Before opening/merging a PR, run local coverage to inspect missed branches and lines:

```shell
powershell -ExecutionPolicy Bypass -File ./scripts/test-coverage.ps1
```

The coverage workflow generates:

- OpenCover XML at `artifacts/coverage/coverage.opencover.xml` (tooling-compatible).
- HTML report at `artifacts/coverage-report/index.html` (human-friendly inspection).

Use the HTML report to prioritize future tests:

- Focus first on low-coverage services/controllers touched by recent changes.
- Prefer adding branch-focused tests for condition-heavy methods over broad, low-assertion tests.
