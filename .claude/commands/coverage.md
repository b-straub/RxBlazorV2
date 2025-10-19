---
description: Run tests with coverage, generate HTML report, and open in browser
---

Run the full coverage workflow:

1. Execute all tests with coverage collection
2. Generate HTML coverage report
3. Open the report in the default browser

Use the following commands:

```bash
# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/

# Generate HTML report
reportgenerator -reports:"**/TestResults/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:Html

# Open report in browser
open CoverageReport/index.html
```

Execute these commands sequentially and report the final coverage statistics to the user.
