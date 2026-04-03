# RxBlazorV2 Claude Code Skills

Architectural guidance and reactive pattern expertise for building maintainable Blazor applications with RxBlazorV2.

## Installation

Symlink the skills into your Claude Code user skills directory:

```bash
mkdir -p ~/.claude/skills
ln -s /path/to/RxBlazorV2/plugins/rxblazor-guide/skills/rxblazor-expert ~/.claude/skills/rxblazor-expert
ln -s /path/to/RxBlazorV2/plugins/rxblazor-guide/skills/rxblazor-audit ~/.claude/skills/rxblazor-audit
```

Replace `/path/to/RxBlazorV2` with your actual clone path. The skills will then be available across all your projects.

### Updating API References

After building, regenerate the bundled API docs:

```bash
./generate-skill-docs.sh
```

This copies XML doc files and pattern documentation into the skills' `references/` folder so they work without cross-project file access.

## Skills

### `/rxblazor-expert` (or auto-invoked)
Expert consultation on reactive patterns, architecture decisions, and best practices. Helps you choose the right pattern for your scenario and warns about anti-patterns.

### `/rxblazor-audit`
Scans a project for reactive anti-patterns and produces a TODO list with correct solutions.

## What It Covers

- **Architecture**: Models vs services, command design, multi-project boundaries
- **Pattern Selection**: Decision matrix for choosing the right reactive pattern
- **Anti-Patterns**: Property-as-event-proxy, fat models, reactive chain reactions
- **Code Review**: Automated detection of common mistakes with fixes

## Usage

```
/rxblazor-expert How should ModelB react when ModelA's save command completes?
/rxblazor-audit /path/to/my/project
```

Or just ask about reactive patterns -- the skill auto-activates when relevant.
