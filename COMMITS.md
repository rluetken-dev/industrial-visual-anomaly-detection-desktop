# Commit Message Guidelines

This project follows the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification.

## Format

```text
<type>(optional scope): <short summary>

(optional body)

(optional footer)
```

## Types

- `feat` – add a capability or observable behavior;
- `fix` – correct a bug or invalid behavior;
- `docs` – change documentation only;
- `test` – add or update automated tests;
- `refactor` – restructure code without changing intended behavior;
- `perf` – improve measured performance without changing intended behavior;
- `style` – change formatting or whitespace only;
- `chore` – change tooling, dependencies, configuration, or repository support files;
- `revert` – revert an earlier commit.

## Recommended Scopes

- `app` – application startup, shutdown, and composition root;
- `ui` – main-window layout, controls, resources, and visual behavior;
- `mvvm` – view models, commands, and observable presentation state;
- `health` – backend liveness and readiness behavior;
- `analysis` – image-analysis workflow and result presentation;
- `images` – image selection, validation, preview, and local file handling;
- `api` – backend client, HTTP contracts, and response mapping;
- `errors` – Problem Details and client failure mapping;
- `config` – application configuration and options;
- `logging` – desktop logging and diagnostics;
- `security` – security and privacy controls;
- `ci` – automated workflows and repository checks;
- `deps` – dependency updates;
- `tests` – shared test infrastructure;
- `readme` – repository README;
- `docs` – documentation spanning multiple documents;
- `architecture` – architecture documentation;
- `integration` – backend integration documentation;
- `spec` – project specification;
- `status` – development-status documentation.

Scopes are optional. Use the most specific useful scope and keep each commit focused on one logical change.

## Examples

```text
feat(app): add generic host startup
```

```text
feat(health): display backend readiness
```

```text
feat(images): add image selection and preview
```

```text
feat(api): add backend analysis client
```

```text
feat(analysis): display anomaly decision and score
```

```text
fix(api): preserve backend trace identifier
```

```text
fix(mvvm): restore commands after request cancellation
```

```text
test(health): cover not-ready backend state
```

```text
docs(integration): document analysis error mapping
```

```text
chore(ci): add Windows build and test workflow
```

```text
chore(deps): update MVVM toolkit
```

## Guidelines

- Use lowercase for type and scope.
- Write the summary in imperative mood, such as `add`, not `added`.
- Do not end the summary with a period.
- Keep the summary concise, ideally no longer than 72 characters.
- Keep each commit focused on one logical change.
- Use the body for motivation, trade-offs, or migration details.
- Do not include secrets, credentials, private hosts, personal machine paths, or selected-image paths.
- Do not commit uploaded images, datasets, model artifacts, generated heatmaps, logs, or local runtime output.
- Do not describe planned UI, backend integration, or heatmap support as implemented before verification.
- Do not claim performance, reliability, accessibility, or security improvements without evidence.
- Separate behavioral changes from bulk formatting where practical.

## Breaking Changes

Mark a breaking change when a public configuration schema, backend-client contract, persisted format, or reusable application interface requires consumers to migrate.

```text
feat(api)!: replace analysis response contract
```

Alternatively, use a footer:

```text
feat(config): rename backend options

BREAKING CHANGE: deployments must replace the previous configuration keys.
```

Internal changes before a public or persisted contract exists are not automatically breaking changes.

## Documentation Commits

Use `docs` when only documentation changes:

```text
docs(architecture): document desktop dependency boundaries
```

Use `chore` when documentation is only one part of broader repository initialization:

```text
chore: initialize desktop repository
```

## Test Commits

Use the affected capability as the scope when tests belong to one area:

```text
test(api): cover Problem Details mapping
```

Use `tests` for broad fixtures or shared test infrastructure:

```text
test(tests): add controlled HTTP message handler
```

## Dependency Commits

Use `chore(deps)` when a dependency update does not directly implement product behavior.

If a dependency change affects backend compatibility, serialization, application startup, or user-visible behavior, validate the change and explain it in the commit body.

## Initial Repository Commit

Use:

```text
chore: initialize desktop repository
```

Create the initial commit after:

- the complete solution builds successfully;
- the test project executes successfully;
- generated Visual Studio and .NET output is ignored;
- line-ending and formatting policies are present;
- initial documentation is present;
- placeholder tests have been replaced or removed;
- secrets, machine-specific configuration, datasets, selected images, model artifacts, and generated outputs are excluded.
