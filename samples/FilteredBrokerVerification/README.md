# Filtered broker verification: a repro

This reproduces the provider verification setup from
[HTTP error 429 Too Many Requests to PactFlow](https://community.smartbear.com/t/http-error-429-too-many-requests-to-pactflow/893).
It shows what that setup does to the broker, both in request volume and in the
verification results the broker records.

## The setup being reproduced

- One `PactVerifier` per xUnit `[Fact]`. The forum setup has about 70 per CI run, across about 12 repos on the same PactFlow tenant.
- Every verifier uses `WithPactBrokerSource` and consumer version selectors, so it fetches **all** pacts for the provider.
- `WithFilter(description: ...)` narrows each run to one interaction.
- The test arranges the provider state itself, with no provider-state endpoint.
- `PublishResults(...)` is on for every one of those runs.

`FilteredPerTestVerificationTests` is that setup. `SingleVerificationTests` is the recommended alternative: one unfiltered run that uses `WithProviderStateUrl`.

The two consumers:

| Consumer     | Interactions                                                      |
|--------------|-------------------------------------------------------------------|
| OrdersWeb    | order 1 (200), order 2 (200), missing order (404)                 |
| OrdersMobile | order 1 only                                                      |

`SIMULATE_BUG=order2` makes the provider return `state` instead of `status` for order 2. That is a real contract break.

## Running it

Requires docker, the .NET 10 SDK, `jq` and `curl`.

```bash
./run.sh
```

To test a local PactNet checkout, for example pact-net#569 (libpact_ffi 0.5.13):

```bash
PACTNET_PROJECT=$HOME/dev/pact-foundation/pact-net-569/src/PactNet/PactNet.csproj ./run.sh
```

`run.sh` does the following:

- Starts a Pact Broker behind a Caddy proxy that logs every request.
- Publishes both pacts on branch `main`.
- Runs four scenarios, each under its own provider version.
- For each scenario, prints the broker requests made, every verification result recorded, and the `can-i-deploy` answer.

Clean up afterwards with `docker compose down -v`.

## Results

The results are identical on **PactNet 5.0.1 (libpact_ffi 0.4.27)** and **pact-net#569 (libpact_ffi 0.5.13)**.

### 1. Filtered per-test runs (forum setup)

The broker recorded six verification results for one provider version, and every one is labelled a verification of the whole pact:

```
#102 OrdersWeb    success=true   covers 1/3 interactions: a request for a missing order
#104 OrdersWeb    success=true   covers 1/3 interactions: a request for order 2
#106 OrdersWeb    success=true   covers 1/3 interactions: a request for order 1
#101 OrdersMobile success=true   covers 0/1 interactions: (none)        <- nothing matched the filter
#103 OrdersMobile success=true   covers 0/1 interactions: (none)        <- nothing matched the filter
#105 OrdersMobile success=true   covers 1/1 interactions: a request for order 1
```

- A filter that matches **zero** interactions in a pact still publishes `success=true` for that pact. "All results passed" is vacuously true for an empty set.
- Broker requests: about **11 per verifier run** with two pacts (33 for three facts). The calls are the index, pacticipant and branch lookups, `for-verification`, one fetch per pact, and one publish per pact.
- PactFlow rate limits per **tenant**, not per token. One repo with 70 facts makes about 770 requests per CI run with two pacts, so a single pipeline can hit the limit by itself. Across 12 repos on the same tenant, plus any other API use (scripts, scheduled reports), the 429s are expected.

### 2. A failure masked by later filtered runs

`Order2Exists` runs first and fails against the buggy provider. At that point `can-i-deploy` says `deployable=false`.

The rest of the same CI run then publishes passing, filtered results for the same provider version:

```
#108 OrdersWeb    success=false  covers 1/3 interactions: a request for order 2
#110 OrdersWeb    success=true   covers 1/3 interactions: a request for a missing order
#112 OrdersWeb    success=true   covers 1/3 interactions: a request for order 1

can-i-deploy: deployable=true  (All required verification results are published and successful)
```

**A provider version with a known contract break is now deployable.** The broker uses the latest verification for each pact and provider version, so whichever filtered test publishes last wins. Under xUnit, test order is not something you should depend on.

### 3 and 4. One unfiltered run (recommended)

- One broker round trip: 11 requests total, however many interactions there are.
- One result per pact, covering 3/3 and 1/1 interactions.
- With the bug, `can-i-deploy` stays `false`.

## Why this happens (pact-reference)

`pact_verifier::verify_provider_async`, and the FFI handle that wraps it, publish results whenever publish options are set (`rust/pact_verifier/src/lib.rs`, `if let Some(publish) = publish_options`, line 1181 at pact-reference `e938dfb8`). Neither checks `FilterInfo`:

- `verify_pact_internal` skips interactions that don't match the filter.
- `publish_result` / `publish_to_broker` then posts `success = all(results ok)` for every pact fetched. That includes pacts where no interactions ran.

This is unchanged in libpact_ffi 0.4.27, 0.5.4, 0.5.13 and current `main`. **pact-net#569 does not address it.** What #569 does bring, from FFI 0.5.5 (pact-reference#533), is retry with back-off on 429/5xx that honours `Retry-After`. That helps with the 429s, but it does not fix the request volume or the misleading results.

## Recommendations for the forum setup

1. **Run one verifier per provider**, not one per test, and handle provider states with `WithProviderStateUrl` or a state-change middleware. This removes the N× broker traffic and the results that mask each other.
2. **Never publish from filtered runs.** `WithFilter` / `PACT_DESCRIPTION` / `PACT_PROVIDER_STATE` are for local debugging. Gate `PublishResults` on CI and on there being no filter.
3. Upgrade to a PactNet release on libpact_ffi ≥ 0.5.5 for 429 retry/back-off (pact-net#569).
4. The limit is shared by everything on the tenant, so separate tokens won't help. Once each repo runs one verifier, avoid starting many provider pipelines at the same moment, and check for other API clients on the tenant.

## Core fix ([pact-reference#611](https://github.com/pact-foundation/pact-reference/pull/611))

A related fix for the verification notices shown when results aren't published is in [pact-reference#612](https://github.com/pact-foundation/pact-reference/pull/612).

This brings the core in line with pact-ruby ([0c56752](https://github.com/pact-foundation/pact-ruby/commit/0c56752e04675d48e7320e44ed3918d3d567b796)). pact-jvm has similar behaviour.

In `pact_verifier`, results for a pact are published only if every interaction in that pact was verified. Otherwise the verifier logs this warning and publishes nothing for that pact (no result, tags or branch):

```
WARNING: Cannot publish verification for <consumer> as not all interactions have been verified (x of y were verified). Re-run the verification without the filter parameters or environment variables to publish the verification.
```

Re-running `run.sh` against pact-net#569 with a libpact_ffi built from #611:

- **Scenario 1:** across 3 runs there is only one publish, for OrdersMobile, which the Order1 filter covers 1/1. Nothing is published for OrdersWeb (1/3 each) or for OrdersMobile runs that matched 0/1. Broker requests drop from 33 to 18.
- **Scenario 2:** the failing filtered run no longer publishes, and neither do the passing filtered runs. `can-i-deploy` for OrdersWeb is no longer `true` ("no verified pact"), so the broken provider version can't be deployed.
- **Scenarios 3 and 4:** unchanged.
