# Contributing to MediSharp

Agreements about branches, pull requests, code review and the Definition of Done. Read this before you start working on a card.

## Overview

| Step                               | Who                      | Trello column                     |
| ---------------------------------- | ------------------------ | --------------------------------- |
| 1. Pick up a card                  | Developer                | To Do → **In Progress**    |
| 2. Build in your own branch        | Developer                | In Progress                       |
| 3. Open a pull request             | Developer                | → **Review**               |
| 4. Code review                     | 2 reviewers              | Review                            |
| 5. Merge into `development`      | Developer                | → **Testing / Acceptance** |
| 6. Complete the Definition of Done | Developer + Project lead | → **Done**                 |

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

| Type         | When to use                                                                                          |
| ------------ | ---------------------------------------------------------------------------------------------------- |
| `feature/` | New functionality or a user story                                                                    |
| `fix/`     | Fixing a bug on `development`                                                                      |
| `hotfix/`  | An urgent bug in production. Branch from `main`, merge back into both `main` and `development` |
| `chore/`   | Maintenance without functional changes, such as packages, config or `.gitignore`                   |
| `ci/`      | Changes to GitHub Actions, Docker or deployment                                                      |
| `docs/`    | Documentation only, such as the README                                                               |

More examples:

- `feature/PAT-02-view-own-appointments`
- `fix/AFS-03-appointment-date-format`
- `hotfix/login-fails`
- `chore/update-nuget-packages`
- `ci/docker-images-to-ghcr`
- `docs/readme-dokploy`

## Workflow

1. Assign yourself to the Trello card and move it to **In Progress**. Check that the acceptance criteria are clear; if not, ask first.
2. Get the latest version: `git checkout development && git pull`.
3. Create your branch from `development`: `git checkout -b feature/<STORY-ID>-short-description`. A `hotfix/` is the only exception; create it from `main`.
4. Commit and push your work to your own branch. Follow the coding standards in [CODE_REVIEW.md](CODE_REVIEW.md); reviewers check against them.
5. Open a pull request into `development`, add **2 reviewers** and move the Trello card to **Review**.
6. The PR needs **2 approvals** and a **green CI** (`client` and `server`) before it can be merged.
7. Merge, delete your branch and move the Trello card to **Testing / Acceptance**.

## Pull requests

- Give the PR a clear title that starts with the story ID and says what changes, for example _"AFS-03: Patient can schedule an appointment"_.
- Fill in the PR template.
- Add the link to the Trello card in the description.
- Keep PRs small: one card or one topic per PR.
- Resolve (or reply to) all review comments before merging.

## Code review

- Review with the [code review checklist](CODE_REVIEW.md) next to you. It is a reference, not a form: you don't tick anything off.
- Leave comments on the lines that need changes and mention the checklist number, for example _"2.4 – `.Result` is used here; make this method `async`."_
- Give your verdict with **Review changes**:
  - **Approve** – everything is fine (small remarks are allowed; start them with `nit:`)
  - **Request changes** – something must be changed before it can be merged
  - **Comment** – just a question, no verdict yet

## From merge to Done

A merged PR does not mean the story is done. The **Definition of Done** is checked per user story, on the Trello card, and is the gate to the **Done** column.

1. Work through the DoD checklist on the Trello card: test on `development` (end-to-end, regression, devices, accessibility), update the documentation, and create new cards for any remaining non-blocking issues.
2. The project lead reviews and accepts the story.
3. Only when **all** DoD items are ticked off does the card move to **Done**

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

| Type         | When to use                                                                                          |
| ------------ | ---------------------------------------------------------------------------------------------------- |
| `feature/` | New functionality or a user story                                                                    |
| `fix/`     | Fixing a bug on `development`                                                                      |
| `hotfix/`  | An urgent bug in production. Branch from `main`, merge back into both `main` and `development` |
| `chore/`   | Maintenance without functional changes, such as packages, config or `.gitignore`                   |
| `ci/`      | Changes to GitHub Actions, Docker or deployment                                                      |
| `docs/`    | Documentation only, such as the README                                                               |

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
