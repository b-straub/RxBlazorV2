# GitHub Actions Setup

## Required Secrets

To enable automatic NuGet publishing, you need to configure the following secret in your GitHub repository:

### NUGET_API_KEY

1. Go to https://www.nuget.org/account/apikeys
2. Create a new API key with "Push" permission for "RxBlazorV2" package
3. Copy the generated key
4. In your GitHub repository, go to Settings → Secrets and variables → Actions
5. Click "New repository secret"
6. Name: `NUGET_API_KEY`
7. Value: Paste your NuGet API key
8. Click "Add secret"

## GitHub Pages Setup

To enable GitHub Pages deployment:

1. Go to your repository Settings → Pages
2. Under "Build and deployment", select "GitHub Actions" as the source
3. The deploy-ghpages.yml workflow can then be triggered manually

## Workflows

### Build and Test (build.yml)
- **Triggers**: Push to `master` or `develop` branches, pull requests
- **Actions**: Restore, build, run generator tests, run core tests, pack NuGet
- **Purpose**: Continuous integration for all commits

### Publish to NuGet (publish.yml)
- **Triggers**: Push tags matching `v*.*.*` (e.g., `v1.0.0`, `v1.2.3`)
- **Actions**:
  1. Build and test the project
  2. Pack the NuGet package with version from tag
  3. Push to NuGet.org
  4. Create GitHub release with package attached
- **Purpose**: Automated releases

### Deploy to GitHub Pages (deploy-ghpages.yml)
- **Triggers**: Manual only (workflow_dispatch)
- **Actions**:
  1. Build the sample Blazor WebAssembly application
  2. Publish to GitHub Pages
- **Purpose**: Deploy interactive demo

## Creating a Release

1. Ensure all tests pass on master branch
2. Update version in `RxBlazorV2/RxBlazorV2.csproj` if needed (optional, tag version takes precedence)
3. Create and push a tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
4. GitHub Actions will automatically:
   - Run all tests (Generator + Core)
   - Build the package
   - Publish to NuGet.org
   - Create a GitHub release

## Manual Testing

To test the build process locally before tagging:

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run generator tests
dotnet test RxBlazorV2.GeneratorTests/RxBlazorV2.GeneratorTests.csproj --configuration Release

# Run core tests
dotnet test RxBlazorV2.CoreTests/RxBlazorV2.CoreTests.csproj --configuration Release

# Create NuGet package
dotnet pack RxBlazorV2/RxBlazorV2.csproj --configuration Release --output ./artifacts
```

The package will be in `./artifacts/RxBlazorV2.{version}.nupkg`

## Project Structure

- **RxBlazorV2** - Core library (packaged to NuGet)
- **RxBlazorV2Generator** - Roslyn source generator (bundled in NuGet)
- **RxBlazorV2CodeFix** - Code analyzers and fixes (bundled in NuGet)
- **RxBlazorV2Sample** - Sample Blazor WebAssembly application (deployed to GitHub Pages)
- **RxBlazorV2.GeneratorTests** - Generator unit tests
- **RxBlazorV2.CoreTests** - Core functionality tests
