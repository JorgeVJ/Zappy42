#!/usr/bin/env bash
set -u

SERVER=./server
CLIENT=./client

PASS=0
FAIL=0

run_test() {
    local name="$1"
    shift
    local cmd=("$@")

    echo "TEST: $name"
    echo "CMD : ${cmd[*]}"

    if "${cmd[@]}" >out.log 2>err.log; then
        echo "RESULT: OK"
        PASS=$((PASS+1))
    else
        echo "RESULT: FAIL (expected?)"
        FAIL=$((FAIL+1))
        echo "STDERR:"
        cat err.log
    fi
    echo "------------------------------------"
}

run_fail() {
    local name="$1"
    shift
    local cmd=("$@")

    echo "TEST: $name"
    echo "CMD : ${cmd[*]}"

    if "${cmd[@]}" >out.log 2>err.log; then
        echo "RESULT: UNEXPECTED OK"
        FAIL=$((FAIL+1))
    else
        echo "RESULT: expected failure"
        PASS=$((PASS+1))
    fi
    echo "------------------------------------"
}

# --------------------
# SERVER TESTS
# --------------------

# run_test "server minimal valid" \
#     "$SERVER" -p 4242 -x 20 -y 20 -n red blue -c 10 -t 20

# run_fail "server missing port" \
#     "$SERVER" -x 20 -y 20 -n red -c 10 -t 20

# run_fail "server repeat non-repeatable" \
#     "$SERVER" -p 4242 -p 4243 -x 20 -y 20 -n red -c 10 -t 20

# run_fail "server invalid arity (-x missing value)" \
#     "$SERVER" -p 4242 -x -y 20 -n red -c 10 -t 20

# run_fail "server duplicate team names" \
#     "$SERVER" -p 4242 -x 20 -y 20 -n red red -c 10 -t 20

# run_fail "server negative width" \
#     "$SERVER" -p 4242 -x -1 -y 20 -n red -c 10 -t 20

# run_test "server delimiter handling" \
#    "$SERVER" -p 4242 -x 20 -y 20 -n red -c 10 -t 20 -- file1 file2

# --------------------
# CLIENT TESTS
# --------------------
run_fail "invalid param" \
         "$CLIENT" -

run_test "port param valid" \
         "$CLIENT" -p 4242

run_fail "port param invalid" \
         "$CLIENT" -p

run_test "client teamname valid" \
         "$CLIENT" -n red

run_fail "client teamname invalid" \
         "$CLIENT" -n

run_test "client minimal valid" \
    "$CLIENT" -p 4242 -n red

run_test "client with host flag" \
    "$CLIENT" -p 4242 -n red -h

run_test "client with host flag and param" \
         "$CLIENT" -p 4242 -n red -h hostname

run_fail "client repeat port" \
    "$CLIENT" -p 4242 -p 4243

run_fail "client repeat teamname" \
         "$CLIENT" -n 4242 -n 4243

run_fail "client repeat host" \
         "$CLIENT" -h -h

run_test "client delimiter handling" \
         "$CLIENT"  -p 4242 -- -p 4243

run_fail "client delimiter handling" \
    "$CLIENT" -p -- 4242

# --------------------
# SUMMARY
# --------------------

echo
echo "====== SUMMARY ======"
echo "PASS: $PASS"
echo "FAIL: $FAIL"

if [[ $FAIL -ne 0 ]]; then
    exit 1
fi
exit 0
