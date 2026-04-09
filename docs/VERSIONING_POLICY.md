# PawSharp — Versioning, Branching & Release Policy

This document defines the official rules for version numbers, branch names, commits, and releases in the PawSharp repository. All contributors are expected to follow this policy.

---

## Table of Contents

- [Version Numbering](#version-numbering)
- [Branch Strategy](#branch-strategy)
- [Commit Message Format](#commit-message-format)
- [Release Workflow](#release-workflow)
- [NuGet Package Rules](#nuget-package-rules)
- [CHANGELOG Requirements](#changelog-requirements)
- [Release Hygiene Checks](#release-hygiene-checks)
- [Quick Reference](#quick-reference)

---

## Version Numbering

PawSharp uses **Semantic Versioning 2.0.0** (`MAJOR.MINOR.PATCH[-pre-release]`).

| Segment | When to bump |
|---|---:|
| `MAJOR` | Breaking API changes that require consumer code changes |
| `MINOR` | New backwards-compatible features or significant additions |
| `PATCH` | Backwards-compatible bug fixes only |

### Pre-release identifiers (in order of maturity)

```
1.0.0-alpha.2   ← early development, APIs may change freely
6.1.0-beta-1    ← feature-complete, undergoing stabilisation
6.1.0-rc-1      ← release candidate, only critical fixes
6.1.0           ← stable release
```

- Pre-release versions are **never** pushed to NuGet.org stable feed (they land in the pre-release feed only).
- Bump the numeric suffix (`alpha.1` → `alpha.2`) for every package-bearing push within the same series.
- All nine library packages **always share the same version string** via `src/Directory.Build.props`.
- Individual `.csproj` `<Version>` entries mirror `Directory.Build.props`; update both together.

---

## Branch Strategy

### Permanent branches

| Branch | Purpose |
|---|---:|
| `main` | Integration branch — always buildable, tests always pass |

### Version branches (required for every release)

Every set of changes destined for a versioned release **must** live on a dedicated version branch before being merged to `main`.

**Naming convention:**

```
release/v<MAJOR>.<MINOR>.<PATCH>[-pre-release]
```

**Examples:**

```
release/v1.0.0-alpha.2
release/v6.1.0-beta-1
release/v6.1.0-rc-1
release/v6.1.0
release/v7.0.0
```

### Feature & fix branches (optional but encouraged)

```
feat/<short-description>          # new feature
fix/<short-description>           # bug fix
chore/<short-description>         # tooling, CI, deps, formatting
docs/<short-description>          # documentation only
refactor/<short-description>      # internal refactor without behaviour change
```

These branches are cut from `main` and merged back via Pull Request.

### Branch lifecycle

```
main ──┬──────────────────────────────────────────► main
       │
       └── release/v1.0.0-alpha.2 ──► PR ──► merge ──► tag v1.0.0-alpha.2
```

1. Cut `release/vX.Y.Z` from `main`.
2. Make all version-related changes on that branch.
3. Open a Pull Request targeting `main`.
4. After passing CI, merge (squash or merge commit — your choice).
5. On `main`, push the version tag to trigger the Release workflow.

---

## Commit Message Format

PawSharp uses **Conventional Commits** (`https://www.conventionalcommits.org`).

```
<type>(<scope>): <short summary>

[optional body]

[optional footer(s)]
```

### Types

| Type | Use for |
|---|---:|
| `feat` | New feature visible to consumers |
| `fix` | Bug fix |
| `perf` | Performance improvement |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or correcting tests |
| `docs` | Documentation only |
| `chore` | Build process, CI, dependency bumps, tooling |
| `ci` | Changes to GitHub Actions workflows |
| `release` | Version bump commits |

### Scopes (optional but useful)

`api`, `gateway`, `core`, `interactions`, `client`, `voice`, `cache`, `commands`, `interactivity`, `tests`, `ci`, `docs`

### Examples

```
feat(interactions): add GetOptionValue<T> extension for slash command options
fix(gateway): handle WebSocket reconnect race condition on RESUME
chore: bump all packages to 1.0.0-alpha.2
release: v1.0.0-alpha.2
ci: add release.yml workflow with NuGet publish step
docs: update VERSIONING_POLICY with branch naming rules
```

### Rules

- Summary line ≤ 72 characters, imperative mood, no trailing period.
- Body wrapped at 100 characters.
- Reference GitHub issues/PRs in footers: `Closes #42`, `Refs #17`.
- **Never** commit directly to `main` for anything larger than a trivial typo fix.

---

## Release Workflow

### Step-by-step

```bash
# 1. Cut version branch from main
git checkout main && git pull
git checkout -b release/v1.0.0-alpha.2

# 2. Bump version in Directory.Build.props AND all individual .csproj files
#    (also update User-Agent string, per-package READMEs, root README, CHANGELOG)

# 3. Update CHANGELOG.md
#    Move content under the new [1.0.0-alpha.2] heading, set the date.

# 4. Commit
git add -A
git commit -m "release: v1.0.0-alpha.2"

# 5. Push the version branch
git push -u origin release/v1.0.0-alpha.2

# 6. Open PR  →  review  →  merge into main

# 7. On main, push the tag  →  triggers release.yml automatically
git checkout main && git pull
git tag v1.0.0-alpha.2
git push origin v1.0.0-alpha.2
```

### What the `release.yml` workflow does automatically on tag push

1. Builds & tests the solution.
2. Packs all nine library NuGet packages at the tagged version.
3. Creates a **GitHub Release** with the CHANGELOG section as the description and the `.nupkg` files attached.
4. (When `NUGET_API_KEY` secret is set) Pushes packages to NuGet.org.

---

## NuGet Package Rules

- Only the nine `src/PawSharp.*` library projects are packable.
- `TestingBot`, `TestConsole`, and `PawSharp.Benchmarks` must remain `<IsPackable>false</IsPackable>`.
- Package versions are **always** driven by the single `<Version>` tag in `src/Directory.Build.props`.
- Never manually edit individual `.csproj` version fields — that property is inherited.
- Symbol packages (`.snupkg`) are generated automatically and uploaded alongside `.nupkg`.

---

## CHANGELOG Requirements

- Follow the [Keep a Changelog](https://keepachangelog.com) format.
- Every release section header uses the format `## [X.Y.Z-pre] - YYYY-MM-DD`.
- Unreleased changes sit under `## [X.Y.Z-pre] - Unreleased` until the release date is known.
- Sections within each release: `### New Features`, `### Bug Fixes`, `### Breaking Changes`, `### Performance`, `### Internal / Tooling`.
- **Do not delete** historical sections — the full changelog is the source-of-truth for the GitHub Release body (extracted automatically by `release.yml`).

---

## Release Hygiene Checks

Before opening or merging a release PR, verify user-facing version references are consistent with `src/Directory.Build.props`:

1. Root docs and package READMEs mention the current release version.
2. Example bots do not contain stale hardcoded historical versions.
3. Branding assets (for example `assets/pawsharp-banner.svg`) show the current version and target framework.
4. `CHANGELOG.md` includes the release section with the correct date/version format.

Recommended quick scan from repository root:

```bash
pwsh ./tools/check-release-hygiene.ps1

# Windows PowerShell 5.1
powershell -ExecutionPolicy Bypass -File .\tools\check-release-hygiene.ps1
```

The script validates that user-facing examples/assets are aligned with `src/Directory.Build.props` version and current target framework.

---

## Quick Reference

```
main               ← always green, never force-push
release/vX.Y.Z     ← one branch per version, merged via PR, then tagged
feat/*  fix/*      ← short-lived feature/fix branches

Version format:  1.0.0-alpha.2   (pre-release suffix uses dot notation, e.g. alpha.2)
Tag format:      v1.0.0-alpha.2  (no spaces, no underscores)
Branch format:   release/v1.0.0-alpha.2

Conventional commit types: feat fix perf refactor test docs chore ci release
```
