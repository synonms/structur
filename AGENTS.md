# Structur Framework

## Purpose
This file is the entry point for agents working in this repository. Use it to find the right workflow, rule, skill, or reference doc instead of carrying all guidance in one place.

## Where to look
- **Rules**: `.agents/rules/conventions.md` and `.agents/rules/structur-coding-rules.md`
- **Workflows**: `.agents/workflows/feature-implementation.md`
- **Skills**: `.agents/skills/`
- **Resources**: `.agents/resources/`

## Agent directory
- **framework-developer** - Maintains and enhances the Structur framework.
- **implementation-validator** - Checks implementations against specs and finds gaps.
- **integration-test-writer** - Writes integration tests for integrated behaviour.
- **readme-specialist** - Improves README files and documentation.
- **sample-api-developer** - Implements Sample API features.
- **sample-ui-developer** - Implements Sample UI features.
- **sample-integration-test-writer** - Writes Sample project integration tests.
- **spec-writer** - Writes technical specifications.
- **unit-test-writer** - Writes unit tests from user stories.
- **user-story-writer** - Writes user stories from feature requests.

## When to use the main workflows
- For new feature work, start with `.agents/workflows/feature-implementation.md`.
- For implementation validation, use `implementation-validator` after each meaningful step.
- For documentation tasks, route to `readme-specialist`.

## Skills
Use the skill docs when the task matches a reusable implementation pattern:
- `new-aggregate-root`
- `new-aggregate-member`
- `new-domain-event-handlers`
- `new-integration-tests`
- `new-projection`
- `new-sample-feature`
- `new-value-object`

## Reference resources
- Architecture and request flow: `.agents/resources/structur-overview.md`
- Build and test commands: `.agents/resources/developer-commands.md`
- Coding and domain rules: `.agents/rules/structur-coding-rules.md`

## Routing rule
Keep this file short. Put durable rules in rule docs, step-by-step procedures in skills or workflows, and descriptive background material in resources.
