#!/bin/bash
# Generates API reference docs for Claude Code skills by copying XML doc files
# Run after build: ./generate-skill-docs.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REFS_DIR="$SCRIPT_DIR/plugins/rxblazor-guide/skills/rxblazor-expert/references"

# Build both projects to generate XML docs
echo "Building projects..."
dotnet build "$SCRIPT_DIR/RxBlazorV2/RxBlazorV2.csproj" -c Debug -q
dotnet build "$SCRIPT_DIR/RxBlazorV2.MudBlazor/RxBlazorV2.MudBlazor.csproj" -c Debug -q

# Copy XML doc files
echo "Copying API docs..."
cp "$SCRIPT_DIR/RxBlazorV2/bin/Debug/net10.0/RxBlazorV2.xml" "$REFS_DIR/RxBlazorV2-api.xml"
cp "$SCRIPT_DIR/RxBlazorV2.MudBlazor/bin/Debug/net10.0/RxBlazorV2.MudBlazor.xml" "$REFS_DIR/RxBlazorV2.MudBlazor-api.xml"

# Copy pattern docs
echo "Copying pattern docs..."
cp "$SCRIPT_DIR/docs/REACTIVE_PATTERNS.md" "$REFS_DIR/reactive-patterns.md"

echo "Done. Skill references updated at: $REFS_DIR"
