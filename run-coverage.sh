#!/bin/bash
# Coverage workflow script
# Usage: ./run-coverage.sh

set -e  # Exit on error

echo "==================================="
echo "Running Coverage Workflow"
echo "==================================="
echo ""

echo "Step 1/3: Running tests with coverage..."
# xunit.v3 4.x runs on Microsoft.Testing.Platform (see global.json), so coverage is collected
# by Microsoft.Testing.Extensions.CodeCoverage rather than by the coverlet MSBuild targets.
# Each test project writes its own uniquely named report into ./TestResults, so the output name
# is left to the platform - forcing one via --coverage-output makes the projects overwrite each other.
rm -rf ./TestResults
dotnet test --coverage --coverage-output-format cobertura

echo ""
echo "Step 2/3: Generating HTML report..."
reportgenerator -reports:"./TestResults/*.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:Html

echo ""
echo "Step 3/3: Opening report in browser..."
open CoverageReport/index.html

echo ""
echo "==================================="
echo "Coverage workflow completed!"
echo "==================================="
