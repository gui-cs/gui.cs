#!/bin/bash

# This script runs the tests in a loop until they all pass.
# It will exit if any test run fails.

dotnet build -c Debug

iterationCount=1

while true; do
    echo "Starting iteration $iterationCount..."

    dotnet test --no-build --diag:TestResults/log.txt

    if [ $? -ne 0 ]; then
        echo "Test run failed on iteration $iterationCount. Exiting."
        exit 1
    fi

    # Clean up the log files
    rm log*

    # Increment the iteration counter
    ((iterationCount++))
done