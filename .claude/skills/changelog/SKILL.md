---
name: changelog
description: >-
  Decide whether a change needs a CHANGELOG entry and write it under ## [Unreleased].
  Use when finishing any distinct unit of work that touched shipped code - a feature,
  a bug fix, a behaviour change - before committing it, opening a PR, or reporting the
  task done; also when asked to add, reword or check an entry. The changelog is kept in
  lockstep with the code, NOT written up at release time. Do NOT use to cut a release or
  promote the Unreleased section, which is a release skill's job.
user-invocable: true
auto-trigger: true
trigger_keywords:
  - changelog
  - changelog entry
  - unreleased
  - keep a changelog
  - note the change
  - record the change
  - finished the feature
  - fixed the bug
  - open a pr
  - ready to commit
  - task complete
---

# /changelog — Write an Unreleased Entry

Project-agnostic. Everything specific to a repo is derived in step 1, never hardcoded here.

## Orientation

The changelog is maintained **in lockstep with the code**, not reconstructed at release time. By the time
a release is cut, `## [Unreleased]` should already be complete and merely need promoting. Backfilling a
release's worth of entries from a diff is the failure mode this skill exists to prevent: it is done from
memory of what the commits meant rather than from what actually shipped, and it is where entries get
missed.

**Use when:**
- Finishing a distinct unit of work that touched shipped code, before you commit it, open the PR, or
  report the task done. A feature, a bug fix, a behaviour change, a dependency bump.
- Asked to add, reword, or review an entry
- A breaking change needs recording

**Do NOT use when:**
- Cutting a release, promoting `## [Unreleased]`, or picking a version number. That is the release skill.
- The change is still being designed. Write the entry from what shipped, not from what was planned.
- The work touched nothing consumers receive. Step 1 settles this, and "no entry" is a valid outcome.

**When in the task to run it:** at the end, once the change is settled and verified, and in the same
commit as the code where practical. At the latest, in the final commit of the task, before pushing or
opening the PR. Not partway through, when the shape may still change.

IF the repo has no `CHANGELOG.md`, do not create one as a side effect of an unrelated task. Say it is
missing and ask whether to start one.

## Step 1: Work out what ships, then whether this change touches it

An entry records what reaches **consumers of the released artifact**. Nothing else. What counts as
"shipped" differs per repo, so derive it rather than assuming.

**Read the repo's own instructions first.** `CLAUDE.md`, `AGENTS.md`, or `CONTRIBUTING.md` often state
what is consumer-facing and what is repo-only. That statement wins over anything inferred below.

Otherwise, work it out from the build. What you are looking for is the boundary between what gets
packaged and what stays in the repo:

```bash
ls CHANGELOG.md CONTRIBUTING.md AGENTS.md CLAUDE.md 2>/dev/null
```

| Ecosystem | Where the shipped set is declared |
|---|---|
| .NET | `IsPackable`/`PackageId` in `*.csproj`, plus `Directory.Build.props`; `PackageReadmeFile` means the README ships |
| Node | `files`, `main`, `exports`, `bin` in `package.json` |
| Python | `packages`/`include` in `pyproject.toml` or `setup.cfg` |
| Go | Every non-`internal/` package in the module |
| Container or app | The paths the Dockerfile or build script copies into the artifact |

The general rule, whatever the ecosystem:

| Category | Entry? |
|---|---|
| Source that compiles into the artifact | Yes, if the public API or its observable behaviour changed |
| Packaging metadata: dependency versions, supported runtimes, description | Yes. These ship even when no source file moved. |
| Docs bundled into the artifact | Only if they correct something wrong. Documenting existing behaviour is not a change to the software. |
| Tests, CI config, lockfiles, editor and agent config, build scripts | No. Repo-only. |

Three traps that catch people out:

- **A refactor with no signature change can still be consumer-facing.** Swapping the exception type a
  method throws, or changing an error message people match on, is a behaviour change.
- **Dynamic and reflective surfaces hide breaks from the compiler.** Anything bound at runtime (a
  `dynamic` member, a serialised name, a DI registration key, a CLI flag) can break consumers with no
  build error anywhere. Judge from the source, not from whether the build went red.
- **Version bumps in the manifest ship.** A raised minimum dependency or a dropped target runtime changes
  what consumers can resolve against.

IF nothing shipped changed, say so and write nothing. A changelog padded with repo churn stops being
readable.

## Step 2: Pick the heading

`CHANGELOG.md` follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). Group under the
spec's six headings, in this order where present. Omit any that are empty. Never invent another heading.

| Heading | For |
|---|---|
| `### Added` | A new public type, member, overload, flag or supported runtime |
| `### Changed` | Different behaviour or a different signature for something that already existed |
| `### Deprecated` | Still present, still working, marked for removal |
| `### Removed` | Gone |
| `### Fixed` | It did the wrong thing and now does the right thing |
| `### Security` | A vulnerability was addressed |

`Deprecated` before `Removed`, in an earlier release, is what SemVer rule 9 expects. If something is being
removed with no deprecation release behind it, say so in the entry rather than hiding it.

## Step 3: Write the line

Match the surrounding entries. Read a few before writing, since an established file has a house style
that beats any example here.

```markdown
## [Unreleased]

### Added

- `Cache.GetOrAdd(key, value)`, which stores the value only when the key is absent.
```

Rules:

- **One line per change.** The heading already says what kind of change it is, so do not restate it. Write
  "`Foo.Bar()`, which does X", not "Added `Foo.Bar()` which adds the ability to do X".
- **One change, one entry.** A `Fixed` line restating a `Changed` line by its consequence is the same fact
  twice.
- **Name the thing.** Backtick the identifier, flag, or endpoint. An entry a reader cannot map onto
  something concrete is not useful.
- **No rationale, no mechanism, no migration prose.** The entry says what changed. Why belongs in the
  commit message; how to migrate belongs in the docs.
- **Mark breaking changes inline** with `**Breaking:**` at the start of the text, inside `### Changed` or
  `### Removed`. Keep a Changelog has no breaking category, so do not create one.
- **Anything removed or deprecated must point somewhere.** Name the replacement or link the doc that
  explains the move. Never log a removal with no exit route.
- **Write for someone upgrading**, who sees only the difference between two releases. They did not watch
  the work happen and do not care how it was built.

## Step 4: Check it against what is already there

`## [Unreleased]` is the diff since the last release, not the history behind it. Because entries are
written per task, this section accumulates across many commits, and successive tasks can contradict each
other.

A change both introduced and fixed inside that window gets no new entry: amend the entry it corrects. If
you added `Foo.Bar()` last week and renamed it today, the `### Added` line should just name the new
signature. A consumer upgrading from the last release never saw the old one, so a `### Changed` line about
the rename is noise to everyone except the people who wrote it.

Read the last release tag's tree to settle this, since the commit log shows in-window churn as though it
shipped. Derive the tag pattern from what the repo actually uses rather than assuming a `v` prefix:

```bash
git tag --sort=-creatordate | head -5          # see the shape first
git describe --tags --abbrev=0                 # nearest tag on this branch
git diff <lastTag>..HEAD -- <the shipped paths from step 1>
```

Never write an entry for a prerelease. Where CI publishes a prerelease per commit, those are out of scope.

## Quality Gates

- [ ] This ran at the end of the task, not at release time, and not partway through the work
- [ ] The entry is in the same commit as the code, or in the task's final commit before pushing
- [ ] The shipped set came from the repo's own instructions or its build config, not from assumption
- [ ] The change touches that shipped set, or no entry was written
- [ ] The heading is one of the spec's six, and sections stay in spec order
- [ ] The line matches the house style of the entries around it
- [ ] One line, naming the thing in backticks, with no rationale or migration prose
- [ ] Breaking changes carry `**Breaking:**` inline rather than a new heading
- [ ] Every `### Removed` and `### Deprecated` entry names a replacement or links an explanation
- [ ] The entry was checked against the diff since the last tag, not against the commit log
- [ ] An entry correcting something already in `## [Unreleased]` amended it instead of stacking a new line
- [ ] Nothing was written under a version heading; entries go under `## [Unreleased]` only
