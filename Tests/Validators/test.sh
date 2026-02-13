#!/usr/bin/env bash

BIN="./server"
PASS=0
FAIL=0

run_test() {
    NAME="$1"
    EXPECT="$2"
    shift 2

    $BIN "$@" > /dev/null 2>&1
    CODE=$?

    if [ "$CODE" -eq "$EXPECT" ]; then
        echo "[PASS] $NAME"
        PASS=$((PASS+1))
    else
        echo "[FAIL] $NAME (expected $EXPECT got $CODE)"
        FAIL=$((FAIL+1))
    fi
}

echo "Running CLI tests..."
echo

# ---------------------------
# VALID CASE
# ---------------------------

run_test "Valid arguments" 0 \
    -p 8080 -x 20 -y 20 -c 4 -t 30 -n red -n blue

# ---------------------------
# INVALID WIDTH
# ---------------------------

run_test "Width too small" 1 \
         -p 8080 -x 5 -y 20 -c 4 -t 30 -n red

run_test "Width too big" 1 \
    -p 8080 -x 65 -y 20 -c 4 -t 30 -n red


# ---------------------------
# INVALID HEIGHT
# ---------------------------

run_test "Height too small" 1 \
         -p 8080 -y 5 -x 20 -c 4 -t 30 -n red

run_test "Height too big" 1 \
    -p 8080 -y 65 -x 20 -c 4 -t 30 -n red

# ---------------------------
# INVALID WIDTH
# ---------------------------

run_test "Clients not enought" 1 \
         -p 8080 -x 20 -y 20 -c 0 -t 30 -n red

run_test "Client too much" 1 \
    -p 8080 -x 20 -y 20 -c 17 -t 30 -n red

# ---------------------------
# Invalid TEAMs
# ---------------------------

run_test "Duplicate team name" 1 \
    -p 8080 -x 20 -y 20 -c 4 -t 30 -n red red

run_test "min len team name" 1 \
         -p 8080 -x 20 -y 20 -c 4 -t 30 -n red ""

run_test "max len team name" 1 \
         -p 8080 -x 20 -y 20 -c 4 -t 30 -n "abcdefghijklmnñopqrstuvwxyzabcdefghijklmnñopqrstuvwxyz" red

run_test "max team nbr" 1 \
    -p 8080 -x 20 -y 20 -c 16 -t 30 -n a b c d e f g h i j k l m n o p

run_test "team nbr > clientnbr" 1 \
    -p 8080 -x 20 -y 20 -c 4 -t 30 -n a b c d e f g h i j k l m

echo
echo "Passed: $PASS"
echo "Failed: $FAIL"

if [ "$FAIL" -ne 0 ]; then
    exit 1
fi

exit 0
