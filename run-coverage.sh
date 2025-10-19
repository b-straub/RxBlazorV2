#!/bin/bash
# Coverage workflow script
# Usage: ./run-coverage.sh

set -e  # Exit on error

echo "==================================="
echo "Running Coverage Workflow"
echo "==================================="
echo ""

echo "Step 1/3: Running tests with coverage..."
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/

echo ""
echo "Step 2/3: Generating HTML report..."
reportgenerator -reports:"**/TestResults/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:Html

echo ""
echo "Step 3/3: Opening report in browser..."
open CoverageReport/index.html

echo ""
echo "==================================="
echo "Coverage workflow completed!"
echo "==================================="
