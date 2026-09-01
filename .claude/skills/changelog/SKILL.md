---
name: changelog
description: >-
  Decide whether a change needs a CHANGELOG entry and write it under ## [Unreleased].
  Use after landing a change that could reach package consumers, or when asked to add,
  reword or check a changelog entry; do NOT use to cut a release or promote the
  Unreleased section, which is the /release skill's job.
user-invocable: true
auto-trigger: false
trigger_keywords:
  - changelog
  - changelog entry
  - unreleased
  - keep a changelog
  - note the change
  - record the change
---

# /changelog — Write an Unreleased Entry

## Orientation

**Use when:**
- A change has landed and you need to decide whether it warrants a `CHANGELOG.md` entry
- Asked to add, reword, or review an entry
- A breaking change needs recording

**Do NOT use when:**
- Cutting a release, promoting `## [Unreleased]`, or picking a version number. That is `/release`.
- The change is still being designed. Write the entry from what shipped, not from what was planned.

`CHANGELOG.md` follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/).

## Step 1: Does it need an entry?

An entry records what reaches consumers of the NuGet package. Nothing else.

| Path | Entry? | Notes |
|---|---|---|
| `src/SideData/**/*.cs` | Yes, if the public API or its observable behaviour changed | Internal refactors that change nothing observable get no entry |
| `src/SideData/SideData.csproj` | Yes, for dependency versions, target frameworks, `Description`, `PackageTags` | These ship even though no `.cs` file moved |
| `README.md` | Only if it corrects something wrong | `PackageReadmeFile` is set, so it is the nuget.org landing page. Documenting existing behaviour is not a change to the software. |
| `tests/**`, `.github/**`, `CLAUDE.md`, `.claude/**`, `GitVersion.yml`, `global.json`, `*.slnx`, `.gitignore` | No | Repo-only |

Two traps specific to this library:

- **`dynamic` hides breaks from the compiler.** Members reached through `obj.SideData()` bind at runtime,
  so renaming or removing one breaks consumers with no build error anywhere. Judge from the source, not
  from whether anything failed to compile.
- **An internal refactor can still change behaviour.** Swapping the exception type a method throws is
  consumer-facing even when the signature is untouched.

IF nothing in the "Yes" rows changed, say so and write nothing. A changelog padded with repo churn stops
being readable.

## Step 2: Pick the heading

Group under the spec's six headings, in this order where present. Omit any that are empty. Never invent
another heading.

| Heading | For |
|---|---|
| `### Added` | A new public type, member, overload or target framework |
| `### Changed` | Different behaviour or a different signature for something that already existed |
| `### Deprecated` | Still present, still working, marked for removal |
| `### Removed` | Gone |
| `### Fixed` | It did the wrong thing and now does the right thing |
| `### Security` | A vulnerability was addressed |

`Deprecated` before `Removed`, in an earlier release, is what SemVer rule 9 expects. If something is being
removed with no deprecation release behind it, say so in the entry rather than hiding it.

## Step 3: Write the line

```markdown
## [Unreleased]

### Added

- `SideDataBag.GetOrAdd<T>(string, T)`, which stores the value only when the name is absent.
```

Rules:

- **One line per change.** The heading already says what kind of change it is, so do not restate it. Write
  "`Foo.Bar()`, which does X", not "Added `Foo.Bar()` which adds the ability to do X".
- **One change, one entry.** A `Fixed` line restating a `Changed` line by its consequence is the same fact
  twice.
- **Name the API.** Backtick the type and member. An entry a reader cannot map onto a symbol is not useful.
- **No rationale, no mechanism, no migration prose.** The entry says what changed. Why it changed belongs
  in the commit message, and how to migrate belongs in the README.
- **Mark breaking changes inline** with `**Breaking:**` at the start of the text, inside `### Changed` or
  `### Removed`. Keep a Changelog has no breaking category, so do not create one.
- **Anything removed or deprecated must point somewhere.** Name the replacement API or link the doc that
  explains the move. Never log a removal with no exit route.

## Step 4: Check it against what is already there

`## [Unreleased]` is the diff since the last release, not the history behind it.

A change both introduced and fixed inside that window gets no new entry: amend the entry it corrects. If
you added `Foo.Bar()` last week and renamed it today, the `### Added` line should just name the new
signature. A consumer upgrading from the last release never saw the old one, so a `### Changed` line about
the rename is noise to everyone except the people who wrote it.

Read the last release tag's tree to settle this, since the commit log shows in-window churn as though it
shipped:

```bash
git tag -l 'v*' --sort=-v:refname | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | head -1
git diff <lastTag>..HEAD -- src/ README.md
```

Never write an entry for a prerelease. The `-ci.N` builds publish per commit automatically and are out of
scope.

## Quality Gates

- [ ] The change touches something in the "Yes" rows of step 1, or no entry was written
- [ ] The heading is one of the spec's six, and sections stay in spec order
- [ ] One line, naming the API in backticks, with no rationale or migration prose
- [ ] Breaking changes carry `**Breaking:**` inline rather than a new heading
- [ ] Every `### Removed` and `### Deprecated` entry names a replacement or links an explanation
- [ ] The entry was checked against `git diff <lastTag>..HEAD`, not against the commit log
- [ ] An entry correcting something already in `## [Unreleased]` amended it instead of stacking a new line
- [ ] Nothing was written under a version heading; entries go under `## [Unreleased]` only
