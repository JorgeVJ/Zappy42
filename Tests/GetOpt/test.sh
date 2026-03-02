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
    echo -ne "CMD : '${cmd[*]}' "

    if "${cmd[@]}" >out.log 2>err.log; then
        echo "RESULT: OK"
        PASS=$((PASS+1))
    else
        echo "RESULT: KO"
        FAIL=$((FAIL+1))
        echo "STDERR:"
        cat err.log
    fi
}

run_fail() {
    local name="$1"
    shift
    local cmd=("$@")

    echo "TEST: $name"
    echo -ne "CMD : '${cmd[*]}' "

    if "${cmd[@]}" >out.log 2>err.log; then
        echo "RESULT: KO"
        FAIL=$((FAIL+1))
    else
        echo "RESULT: OK"
        PASS=$((PASS+1))
    fi
}

# --------------------
# SERVER TESTS
# --------------------

 run_test "server minimal valid" \
    "$SERVER" -p 4242 -x 20 -y 20 -n red blue -c 10 -t 20

 run_fail "server missing port" \
     "$SERVER" -x 20 -y 20 -n red -c 10 -t 20

 run_fail "server repeat port" \
     "$SERVER" -p 4242 -p 4243 -x 20 -y 20 -n red -c 10 -t 20

 run_fail "server invalid arity (-x missing value)" \
     "$SERVER" -p 4242 -x -y 20 -n red -c 10 -t 20

 run_test "server delimiter handling" \
    "$SERVER" -p 4242 -x 20 -y 20 -n red -c 10 -t 20 -- file1 file2

echo
echo "====== Server SUMMARY ======"
echo "PASS: $PASS"
echo "FAIL: $FAIL"
echo

PASS=$((0))
FAIL=$((0))
# --------------------
# CLIENT TESTS
# --------------------
run_test "client minimal valid" \
         "$CLIENT" -p 4242 -n red

run_test "client with host flag" \
         "$CLIENT" -p 4242 -n red -h

run_fail "client repeat port" \
         "$CLIENT" -p 4242 -p 4243 -n red

run_fail "client missing team" \
         "$CLIENT" -p 4242

run_test "client with host flag" \
         "$CLIENT" -p 4242 -n red -h

run_test "client with host flag" \
         "$CLIENT" -p 4242 -n red -h foo

run_fail "client repeat port" \
         "$CLIENT" -p 4242 -p 4243 -n red

run_fail "client missing team" \
         "$CLIENT" -p 4242

run_test "client delimiter handling" \
         "$CLIENT" -p 4242 -n red -- foo

# --------------------
# SUMMARY
# --------------------

echo
echo "====== CLIENT SUMMARY ======"
echo "PASS: $PASS"
echo "FAIL: $FAIL"

exit $FAIL
