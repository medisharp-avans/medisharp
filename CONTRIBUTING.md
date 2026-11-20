# Contributing to MediSharp

Agreements about branches and pull requests. Read this before you start working on a card.

## Branches

### Long-lived branches

| Branch          | Purpose                                                         |
| --------------- | --------------------------------------------------------------- |
| `development` | Integration branch. All work comes together here. Deploys to T. |
| `main`        | What goes to acceptance and production (A → approval → P).    |

You cannot push directly to either branch. Everything goes through a pull request.

### Naming

Format: `type/<STORY-ID>-short-description`, in lowercase with hyphens (the story ID keeps its capitals).

Example: `feature/AFS-03-schedule-appointment`

- **`<STORY-ID>`** is the ID of the user story the branch belongs to, exactly as it appears in the user stories document (prefixes such as `AFS`, `BEH`, `DEC`, `HUI`, `PAT` and `ZKH`). It shows at a glance which story a branch is for.
- **`short-description`** keeps the branch readable without having to look the ID up.
- Branches that do not belong to a user story, such as most `chore/`, `ci/` and `docs/` branches, leave the ID out: `type/short-description`.

| Type       | When to use                                                                                    |
| ---------- | ---------------------------------------------------------------------------------------------- |
| `feature/` | New functionality or a user story                                                              |
| `fix/`     | Fixing a bug on `development`                                                                  |
| `hotfix/`  | An urgent bug in production. Branch from `main`, merge back into both `main` and `development` |
| `chore/`   | Maintenance without functional changes, such as packages, config or `.gitignore`              |
| `ci/`      | Changes to GitHub Actions, Docker or deployment                                                |
| `docs/`    | Documentation only, such as the README                                                         |

More examples:

- `feature/PAT-02-view-own-appointments`
- `fix/AFS-03-appointment-date-format`
- `hotfix/login-fails`
- `chore/update-nuget-packages`
- `ci/docker-images-to-ghcr`
- `docs/readme-dokploy`

## Workflow

1. Get the latest version: `git checkout development && git pull`.
2. Create your branch from `development`: `git checkout -b feature/<STORY-ID>-short-description`. A `hotfix/` is the only exception; create it from `main`.
3. Commit and push your work to your own branch.
4. Open a pull request into `development`.
5. The PR needs **2 approvals** and a **green CI** (`client` and `server`) before it can be merged.
6. Delete your branch after the merge.

## Pull requests

- Give the PR a clear title that starts with the story ID and says what changes, for example _"AFS-03: Patient can schedule an appointment"_.
- Fill in the PR template.
- Add the link to the Trello card in the description.
- Keep PRs small: one card or one topic per PR.
- Resolve (or reply to) all review comments before merging.
