#!/bin/bash
# Generates API reference docs for Claude Code skills and installs them.
# Run after API changes: ./generate-skill-docs.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SKILLS_SRC="$SCRIPT_DIR/plugins/rxblazor-guide/skills"
REFS_DIR="$SKILLS_SRC/rxblazor-expert/references"
USER_SKILLS="$HOME/.claude/skills"

# Build both projects to generate XML docs
echo "Building projects..."
dotnet build "$SCRIPT_DIR/RxBlazorV2/RxBlazorV2.csproj" -c Debug --verbosity minimal
dotnet build "$SCRIPT_DIR/RxBlazorV2.MudBlazor/RxBlazorV2.MudBlazor.csproj" -c Debug --verbosity minimal

# Copy XML doc files into repo
echo "Updating API docs in repo..."
cp "$SCRIPT_DIR/RxBlazorV2/bin/Debug/net10.0/RxBlazorV2.xml" "$REFS_DIR/RxBlazorV2-api.xml"
cp "$SCRIPT_DIR/RxBlazorV2.MudBlazor/bin/Debug/net10.0/RxBlazorV2.MudBlazor.xml" "$REFS_DIR/RxBlazorV2.MudBlazor-api.xml"

# Copy pattern docs
cp "$SCRIPT_DIR/docs/REACTIVE_PATTERNS.md" "$REFS_DIR/reactive-patterns.md"

# Install to user skills (real copies, not symlinks, to avoid cross-project permission prompts)
echo "Installing skills to $USER_SKILLS..."
mkdir -p "$USER_SKILLS"
rm -rf "$USER_SKILLS/rxblazor-expert" "$USER_SKILLS/rxblazor-audit"
cp -R "$SKILLS_SRC/rxblazor-expert" "$USER_SKILLS/rxblazor-expert"
# Resolve audit references symlink to real copy
cp -RL "$SKILLS_SRC/rxblazor-audit" "$USER_SKILLS/rxblazor-audit"

echo "Done. Skills installed at: $USER_SKILLS"
