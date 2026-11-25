#!/usr/bin/env bash
# Reproduces the setup from
# https://community.smartbear.com/t/http-error-429-too-many-requests-to-pactflow/893
#
# Usage:
#   ./run.sh                                     # PactNet 5.0.1 from NuGet (libpact_ffi 0.4.27)
#   PACTNET_PROJECT=/path/to/src/PactNet/PactNet.csproj ./run.sh   # local PactNet checkout
#
# Requires: docker, dotnet, jq, curl
set -euo pipefail

cd "$(dirname "$0")"
BROKER=http://localhost:9292
TEST_DIR=FilteredVerification.Tests
BUILD_ARGS=()
if [[ -n "${PACTNET_PROJECT:-}" ]]; then
  BUILD_ARGS+=("-p:PactNetProject=${PACTNET_PROJECT}")
fi
RUN_ID="${RUN_ID:-$(date +%s)}"

bold() { printf '\n\033[1m%s\033[0m\n' "$*"; }

# ---------------------------------------------------------------- broker
bold "Starting Pact Broker (behind a request-logging proxy) on $BROKER"
docker compose down -v >/dev/null 2>&1 || true
docker compose up -d >/dev/null
for _ in $(seq 1 60); do
  curl -sf "$BROKER/diagnostic/status/heartbeat" >/dev/null && break
  sleep 1
done

publish_pact() { # consumer version branch file
  local content
  content=$(base64 < "$4" | tr -d '\n')
  curl -sf -X POST "$BROKER/contracts/publish" -H 'Content-Type: application/json' -d @- >/dev/null <<EOF
{"pacticipantName":"$1","pacticipantVersionNumber":"$2","branch":"$3",
 "contracts":[{"consumerName":"$1","providerName":"OrdersApi","specification":"pact",
               "contentType":"application/json","content":"$content"}]}
EOF
}
bold "Publishing consumer pacts (branch main)"
publish_pact OrdersWeb    web-1    main pacts/OrdersWeb-OrdersApi.json
publish_pact OrdersMobile mobile-1 main pacts/OrdersMobile-OrdersApi.json
echo "OrdersWeb    web-1    : 3 interactions"
echo "OrdersMobile mobile-1 : 1 interaction  (only 'a request for order 1')"

dotnet build "$TEST_DIR" ${BUILD_ARGS[@]+"${BUILD_ARGS[@]}"} -v q -nologo >/dev/null

# ---------------------------------------------------------------- helpers
proxy_lines() { docker compose logs proxy --no-log-prefix 2>/dev/null | grep -c '"handled request"' || true; }

broker_requests_since() { # starting line count
  docker compose logs proxy --no-log-prefix 2>/dev/null | grep '"handled request"' | tail -n +"$(( $1 + 1 ))" \
    | jq -r '"\(.request.method) \(.request.uri | sub("\\?.*";"") | gsub("/[0-9a-f]{40}";"/{sha}"))  -> \(.status)"' \
    | sort | uniq -c | sort -rn
}

run_tests() { # filter, extra env...
  local filter=$1; shift
  env "$@" dotnet test "$TEST_DIR" ${BUILD_ARGS[@]+"${BUILD_ARGS[@]}"} --no-build --filter "$filter" \
    --logger "console;verbosity=normal" -nologo 2>&1 \
    | grep -E "^\s+(Passed|Failed) |Total tests|Passed!|Failed!" || true
}

max_vid() {
  curl -sf -G "$BROKER/matrix" --data-urlencode "q[][pacticipant]=OrdersApi" \
    | jq '[.matrix[].verificationResult._links.self.href // empty | split("/")[-1] | tonumber] | max // 0'
}

show_results() { # provider version, verification id floor
  local v=$1 floor=$2 matrix
  matrix=$(curl -sf -G "$BROKER/matrix" \
    --data-urlencode "q[][pacticipant]=OrdersApi" --data-urlencode "q[][version]=$v" --data-urlencode "latestby=cvpv")
  local top; top=$(max_vid)

  bold "Broker: every verification result published for OrdersApi $v (oldest first)"
  for consumer in OrdersWeb OrdersMobile; do
    local pact base
    pact=$(printf '%s' "$matrix" | jq -r --arg c "$consumer" '.matrix[] | select(.consumer.name==$c) | .pact._links.self.href')
    base=$(printf '%s' "$matrix" | jq -r --arg c "$consumer" '.matrix[] | select(.consumer.name==$c) | .verificationResult._links.self.href // empty' | sed -E 's#/[0-9]+$##')
    [[ -z "$base" ]] && continue
    local ids; ids=$(curl -sf "$pact" | jq -c '[.interactions[] | {key: ._id, value: .description}] | from_entries')
    for ((id = floor + 1; id <= top; id++)); do
      curl -sf "$base/$id" 2>/dev/null | jq -r --arg c "$consumer" --argjson ids "$ids" \
        '"  #\(._links.self.href | split("/")[-1]) \($c | .+"            " | .[:12]) success=\(.success | tostring | .+" " | .[:5])  covers \(.testResults | length)/\($ids | length) interactions: \([.testResults[] | $ids[.interactionId] // .interactionId] | if length == 0 then "(none)" else join(", ") end)"' || true
    done
  done

  bold "can-i-deploy OrdersApi $v --to main consumers:"
  curl -sf -G "$BROKER/matrix" \
    --data-urlencode "q[][pacticipant]=OrdersApi" --data-urlencode "q[][version]=$v" \
    --data-urlencode "latestby=cvp" --data-urlencode "latest=true" --data-urlencode "mainBranch=true" \
    | jq -r '"  deployable=\(.summary.deployable)  (\(.summary.reason))"'
}

scenario() { # title, provider version, test filter, extra env...
  local title=$1 version=$2 filter=$3; shift 3
  bold "================================================================"
  bold "$title   (provider version $version)"
  local before floor
  floor=$(max_vid); before=$(proxy_lines)
  run_tests "$filter" PROVIDER_VERSION="$version" "$@"
  local after
  after=$(proxy_lines)
  bold "Broker requests made: $((after - before))"
  broker_requests_since "$before"
  show_results "$version" "$floor"
}

# ---------------------------------------------------------------- scenarios
scenario "1. Forum setup: one filtered verifier per [Fact], publishing each run" \
  "filtered-$RUN_ID" "FullyQualifiedName~FilteredPerTestVerificationTests"

scenario "2. Forum setup + a real provider bug on order 2 (Order2Exists runs FIRST, then the rest)" \
  "filtered-bug-$RUN_ID" "FullyQualifiedName~FilteredPerTestVerificationTests.Order2Exists" SIMULATE_BUG=order2
# continue the same CI run (same provider version) with the remaining tests
bold "... same CI run continues: Order1Exists, MissingOrder (provider version filtered-bug-$RUN_ID)"
run_tests "FullyQualifiedName~FilteredPerTestVerificationTests.Order1Exists|FullyQualifiedName~FilteredPerTestVerificationTests.MissingOrder" \
  PROVIDER_VERSION="filtered-bug-$RUN_ID" SIMULATE_BUG=order2
show_results "filtered-bug-$RUN_ID" "$(( $(max_vid) - 6 ))"

scenario "3. Recommended: one unfiltered verifier with a provider-state endpoint" \
  "single-$RUN_ID" "FullyQualifiedName~SingleVerificationTests"

scenario "4. Recommended + the same provider bug" \
  "single-bug-$RUN_ID" "FullyQualifiedName~SingleVerificationTests" SIMULATE_BUG=order2

bold "Done. Broker UI: $BROKER   (docker compose down -v to clean up)"
