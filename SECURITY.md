# Security Policy

## Scope

This repository is a portfolio and academic project. Security controls are implemented and tested, but any real deployment must provide production secrets, infrastructure hardening, monitoring, backups, and an operational incident-response process.

## Reporting a Vulnerability

Please do not publish exploit details, credentials, tokens, or other sensitive information in a public issue.

Use GitHub's private security reporting / security advisory flow when available. If private reporting is not available, contact the repository owner through GitHub before disclosing technical details publicly.

Include:
- the affected component or endpoint;
- steps to reproduce;
- expected and actual behavior;
- security impact;
- a minimal proof of concept with secrets removed.

## Secure Configuration

- Never commit real JWT secrets, database passwords, API keys, private keys, or production connection strings.
- Production must keep demo-data seeding disabled.
- Production database migrations should be applied as a controlled deployment step.
- Configure explicit trusted frontend origins.
- Keep HTTPS, secure cookies, rate limiting, audit logging, and security headers enabled in production.
- Review file-upload limits and content validation before exposing upload features publicly.

## Automated Evidence

The repository includes automated authentication, authorization, CORS, rate-limiting, security-header, file-upload, startup-safety, and PostgreSQL integration tests. GitHub Actions runs the core backend, PostgreSQL integration, and frontend validation pipelines on pull requests and pushes to `main`.

## Supported Versions

Only the current `main` branch is maintained for this portfolio repository.
