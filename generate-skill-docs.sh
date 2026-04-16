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

# Install to user skills.
# If the target is already a symlink (developer install pointing at this repo), leave it alone -
# the references it points to were updated above and are live everywhere instantly.
# Otherwise do a fresh copy install (the default for end users who don't want a symlink).
echo "Installing skills to $USER_SKILLS..."
mkdir -p "$USER_SKILLS"

install_skill() {
    local name="$1"
    local target="$USER_SKILLS/$name"
    if [ -L "$target" ]; then
        local resolved
        resolved="$(readlink "$target")"
        echo "  $name: symlink detected ($resolved) - skipping copy, source already updated"
        return
    fi
    rm -rf "$target"
    # -L resolves the rxblazor-audit/references symlink so the install is self-contained
    cp -RL "$SKILLS_SRC/$name" "$target"
    echo "  $name: copied"
}

install_skill rxblazor-expert
install_skill rxblazor-audit

echo "Done. Skills installed at: $USER_SKILLS"
