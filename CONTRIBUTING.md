# Contributing

Thanks for taking the time to improve this project.

## Development workflow

1. Create a focused branch from `main`.
2. Keep changes small and scoped to one purpose.
3. Do not commit secrets, real credentials, production connection strings, generated build output, or local environment files.
4. Run the relevant validation commands before opening a pull request.
5. Open a pull request and describe the behavior change, tests, and any migration or deployment impact.

## Commit guidance

Use clear, action-oriented commit messages, for example:

- `feat: add booking availability filter`
- `fix: enforce ownership on order lookup`
- `test: make date-sensitive booking tests time-independent`
- `docs: clarify production configuration`
- `ci: add dependency validation`

## Pull requests

A good pull request should include:

- a concise summary;
- why the change is needed;
- testing/validation performed;
- screenshots for UI changes;
- database migration notes when applicable;
- security implications when authentication, authorization, secrets, uploads, payments, or user data are involved.

## Security

Do not report vulnerabilities with exploit details in a public issue. Follow the repository security policy when available.
