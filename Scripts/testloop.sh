#!/bin/bash

iterationCount=1

while true; do
    echo "Starting iteration $iterationCount..."
    
    dotnet test --no-build --diag:log.txt

    if [ $? -ne 0 ]; then
        echo "Test run failed on iteration $iterationCount. Exiting."
        exit 1
    fi

    # Clean up the log files
    rm log*
    
    # Increment the iteration counter
    ((iterationCount++))
done 