# Agent Conventions

This file defines shared conventions for all agents in this repository to ensure consistency and LLM-independence.

## Output Directories

| Agent | Output Location |
|---|---|
| user-story-writer | `docs/user-stories/` |
| spec-writer | `docs/specs/` |

## Frontmatter Format

All agent files (`*.agent.md`) use the following frontmatter structure:

```yaml
---
name: agent-name-kebab-case
description: A concise one-line description of the agent purpose
tools: ['read', 'search', 'edit', 'execute']
color: <color>
---
```

The `name` field must match the filename without the `.agent.md` suffix.

### Available Tools

Use the following standard, LLM-independent tool names. Do not reference IDE-specific or platform-specific tools (e.g. `vscode`, `lsp`, `WebFetch`, `TodoWrite`).

| Tool | Purpose |
|---|---|
| `read` | Read file content |
| `search` | Search file content and file names (covers glob and grep functionality) |
| `edit` | Edit existing files |
| `write` | Create new files |
| `execute` | Run shell commands and tests |
| `fetch` | Fetch remote URLs |

## Colour Reference

| Agent | Colour |
|---|---|
| framework-developer | orange |
| sample-api-developer | orange |
| sample-ui-developer | orange |
| readme-specialist | white |
| orchestrator | white |
| implementation-validator | green |
| spec-writer | purple |
| unit-test-writer | red |
| integration-test-writer | red |
| sample-integration-test-writer | red |
| user-story-writer | blue |

## Folder Structure

| Folder | Purpose |
|---|---|
| `.agents/` | Root folder for all agent definitions |
| `.agents/workflows/` | Orchestration workflows that coordinate multiple agents |
| `.agents/prompts/` | Reusable prompt templates for common tasks |
| `.agents/skills/` | Step-by-step guides for specific implementation tasks |
| `.agents/features/` | Feature-specific prompt files for Sample project features |
| `.agents/rules/` | Shared conventions and rules |
