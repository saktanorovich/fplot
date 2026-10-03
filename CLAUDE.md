# CLAUDE.md — Spec-Driven Development Protocol

This is a spec-driven project. You must follow the workflow below in strict
order for every feature, bug fix, or change. Do not skip steps. Do not
reorder steps. If you are unclear about any step, ask before proceeding.

---

## Mandatory Workflow

### Step 1 — Locate and Read the Spec

Before writing any code, tests, or configuration:

1. Identify which spec file in `specs/` applies to this task.
2. Read the entire spec file.
3. Confirm you have read it by stating:
   - The feature name
   - The tech stack specified in the spec
   - The acceptance criteria (list each one)
   - The test approach (unit / integration / e2e)

Do not proceed to Step 2 until you have stated all four items above.

### Step 2 — Determine the Tech Stack

The tech stack is defined exclusively by the spec. You must:

- Use the exact language, runtime, and frameworks listed in the spec's
  "Tech Stack" section.
- Never substitute a different language or framework without explicit
  user approval and a spec update.
- Never default to a language because it is familiar or common.
- If the spec does not specify a tech stack, STOP and ask the user to
  add one before continuing.

### Step 3 — Write Failing Tests First

Before writing any implementation code:

1. Create the test file(s) in `tests/<feature-slug>/` using the test
   framework specified in the spec.
2. Write tests that cover every acceptance criterion listed in the spec.
3. Run the tests and confirm they fail.
4. Paste the failing test output into your response as proof.

Do not proceed to Step 4 until failing test output is shown.

Tests must be:
- Named clearly after the acceptance criterion they verify
- Runnable with a single command (document that command)
- Failing for the right reason (not erroring due to syntax or config)

### Step 4 — Implement Code to Pass the Tests

Only after Step 3 is complete:

1. Create implementation files in `src/<feature-slug>/`.
2. Write the minimum code necessary to make the failing tests pass.
3. Do not add features, abstractions, or logic not required by the tests.
4. Do not modify the tests to make them pass.

### Step 5 — Verify Tests

1. Run the full test suite.
2. Confirm all tests pass.
3. State which acceptance criteria are now met.
4. If any tests still fail, return to Step 4.

### Step 5b — Smoke Test (mandatory)

After all tests pass:

1. Run the smoke test defined in the spec's "Smoke Test" section.
2. If the spec has no "Smoke Test" section, start the feature and manually confirm it runs without errors.
3. Paste the smoke test command and output as proof.
4. If the smoke test fails, return to Step 4 and fix the runtime issue — do **not** declare the task complete.

A feature that passes all unit tests but fails to run is not complete.

---

## What You Must Not Do

- Do not write implementation code before tests exist and have been run.
- Do not write tests after implementation as a retroactive step.
- Do not assume or invent a tech stack if the spec does not specify one.
- Do not generate a spec file yourself — specs are written by the user.
- Do not modify a spec to make it easier to implement.
- Do not mark a task complete if any test is failing.
- Do not skip the failing-test confirmation in Step 3.

---

## File Layout Convention

```
specs/          <- User-authored spec files (never modified by Claude)
src/            <- Implementation code (created by Claude, per-spec subdirs)
tests/          <- Test files (created by Claude, per-spec subdirs)
```

Each feature gets its own subdirectory under both `src/` and `tests/`,
named after the feature slug from the spec (e.g., `src/word-counter/`).

---

## Trigger Phrases

**Single-agent mode** — `implement this spec: specs/some-file.md`
Begin at Step 1 and execute the full workflow yourself.

**Team mode** — `implement this spec with team: specs/some-file.md`
See the Team Workflow section below.

---

## Team Workflow (Multi-Agent Mode)

Two team sizes are available. Use the one that matches the spec's risk/complexity.

**`implement this spec with team: specs/some-file.md`** → Core team (5 agents)
**`implement this spec with full team: specs/some-file.md`** → Production team (10 agents)

---

### Core Team (5 agents — fast, good for internal tools and low-risk features)

| Teammate | Agent Definition | File Ownership |
|---|---|---|
| solution-architect | solution-architect | `specs/<slug>-adr.md` |
| application-architect | application-architect | `src/<slug>/DESIGN.md` |
| unit-test-writer | unit-test-writer | `tests/<slug>/` |
| developer | developer | `src/<slug>/` (impl files) |
| tech-lead | tech-lead | `reviews/<slug>/REVIEW.md` |

**Task chain with parallelism:**
```
Phase A — sequential:
  Task 1 → solution-architect      [no deps]     Write specs/<slug>-adr.md.
  Task 2 → application-architect   [after T1]    Write src/<slug>/DESIGN.md.

Phase B — parallel (assign Tasks 3 simultaneously after Task 2):
  Task 3 → unit-test-writer        [after T2]    Write failing tests. Paste output.

Phase C — sequential gate (developer waits for Task 3):
  Task 4 → developer               [after T3]    Implement src/<slug>/. Run smoke test. Signal unit-test-writer.
  Task 5 → unit-test-writer        [after T4]    Verify all tests pass.
  Task 6 → tech-lead               [after T5]    Review. Write REVIEW.md. Verdict.
```

---

### Production Team (10 agents — for customer-facing, security-sensitive, or critical features)

| Teammate | Agent Definition | File Ownership |
|---|---|---|
| solution-architect | solution-architect | `specs/<slug>-adr.md` |
| application-architect | application-architect | `src/<slug>/DESIGN.md` |
| security-reviewer | security-reviewer | `specs/<slug>-security.md` (Phase 1) |
| unit-test-writer | unit-test-writer | `tests/<slug>/` |
| integration-test-writer | integration-test-writer | `tests/<slug>/integration/` |
| developer | developer | `src/<slug>/` (impl files) |
| devops-engineer | devops-engineer | `deploy/<slug>/`, `.github/workflows/<slug>.yml` |
| observability-engineer | observability-engineer | `src/<slug>/observability/`, `docs/<slug>/RUNBOOK.md` |
| security-reviewer | security-reviewer | `reviews/<slug>/SECURITY.md` (Phase 2) |
| documentation-writer | documentation-writer | `docs/<slug>/` |
| tech-lead | tech-lead | `reviews/<slug>/REVIEW.md` |

**Task chain with parallelism:**
```
Phase A — sequential:
  Task 1  → solution-architect       [no deps]          Write specs/<slug>-adr.md.
  Task 2  → application-architect    [after T1]         Write src/<slug>/DESIGN.md.

Phase B — PARALLEL (assign Tasks 3a, 3b, 3c simultaneously after Task 2):
  Task 3a → security-reviewer        [after T2]         Threat model → specs/<slug>-security.md.
  Task 3b → unit-test-writer         [after T2]         Write failing unit tests.
  Task 3c → integration-test-writer  [after T2]         Write failing integration tests.

Phase C — sequential gate (developer waits for ALL of Phase B):
  Task 4  → developer                [after T3a+3b+3c]  Implement src/<slug>/. Run smoke test.

Phase D — PARALLEL (assign Tasks 5a, 5b simultaneously after Task 4):
  Task 5a → unit-test-writer         [after T4]         Verify unit tests pass.
  Task 5b → integration-test-writer  [after T4]         Verify integration tests pass.

Phase E — sequential gate:
  Task 6  → security-reviewer        [after T5a+5b]     Implementation review → SECURITY.md.

Phase F — PARALLEL (assign Tasks 7a, 7b, 7c simultaneously after Task 6 PASS):
  Task 7a → devops-engineer          [after T6]         Dockerfile, CI pipeline, ENV.md.
  Task 7b → observability-engineer   [after T6]         Logging, health, metrics, runbook.
  Task 7c → documentation-writer     [after T6]         README, API ref, architecture.

Phase G — sequential gate (final):
  Task 8  → tech-lead                [after T7a+7b+7c]  Final review → REVIEW.md. Verdict.
```

**Critical rule for all team modes**: No two teammates may edit the same file.
Ownership is strict — see each agent's definition for their files.

---

### Situational Agents (add to any team when the spec requires it)

These four agents are not in the default team. Add them based on what the spec describes:

| Agent | Add when spec involves | Slot into task chain |
|---|---|---|
| **api-designer** | A public or internal HTTP/GraphQL API | After application-architect (Phase 1), after developer (Phase 2) |
| **database-specialist** | Persistent data, schema migrations, queries | After application-architect (Phase 1), after developer (Phase 2) |
| **performance-reviewer** | Latency/throughput constraints, high-traffic systems | After developer + tests verified |
| **accessibility-reviewer** | Any UI, frontend, or user-facing component | After developer |

**How to insert them into the task chain:**

`api-designer` (insert after Task 2 and after Task 6):
```
Task 2b → api-designer:    Read spec + DESIGN.md. Write openapi.yaml or schema.graphql.
Task 11b → api-designer:   Contract compliance review. Write API.md.
```

`database-specialist` (insert after Task 2 and after Task 6):
```
Task 2c → database-specialist:  Design schema. Write DATABASE.md + migration files.
Task 11c → database-specialist: Query review. Write reviews/<slug>/DATABASE.md.
```

`performance-reviewer` (insert after tests verified):
```
Task 11d → performance-reviewer: Write benchmarks. Write PERFORMANCE.md.
```

`accessibility-reviewer` (insert after developer, before tech-lead):
```
Task 11e → accessibility-reviewer: WCAG review. Write ACCESSIBILITY.md.
```

**When adding situational agents:** Update the final tech-lead task to include their
review files: "Review REVIEW.md, SECURITY.md, API.md, DATABASE.md, PERFORMANCE.md,
ACCESSIBILITY.md as applicable. Verdict: APPROVED only if all reviewers passed."

---

### Lead Coordination Protocol

#### 1. Before Spawning — Prerequisites

Read the spec first. Confirm:
- The feature slug (used in every file path)
- Team mode: core or production
- Which situational agents are needed
- Whether any spec gaps would block solution-architect — if so, STOP and ask the user

#### 2. Create the Progress Status File

Before spawning the team, create `progress/<slug>.status` with this exact content:

```
SLUG=<slug>
PHASE=A
TASKS_DONE=0
TASKS_TOTAL=0
CURRENT=solution-architect
LAST_UPDATE=<HH:MM:SS>
LAST_EVENT=Team spawning
STATUS=running
```

Also create `progress/` directory and `progress/events.log` (empty).
This file is read by the status line every 3 seconds — keep it updated at every phase transition.

**Update PHASE and CURRENT at every gate crossing:**
```
Phase A starts  → PHASE=A, CURRENT=solution-architect
Phase A done    → PHASE=A→B (update when application-architect starts)
Phase B starts  → PHASE=B, CURRENT=security-reviewer,unit-test-writer,integration-test-writer
Phase C starts  → PHASE=C, CURRENT=developer
Phase D starts  → PHASE=D, CURRENT=unit-test-writer,integration-test-writer
Phase E starts  → PHASE=E, CURRENT=security-reviewer
Phase F starts  → PHASE=F, CURRENT=devops-engineer,observability-engineer,documentation-writer
Phase G starts  → PHASE=G, CURRENT=tech-lead
Done            → STATUS=done, PHASE=DONE
```

On completion, append a summary line to `progress/events.log`:
```
[HH:MM:SS] COMPLETE  <slug> — APPROVED — <N> tasks, $<cost>
```

#### 3. Spawn Script

**Core team:**
```
Spawn teammates:
  solution-architect (agent: solution-architect) — include Task 1 directly in spawn prompt (see below)
  application-architect (agent: application-architect)
  unit-test-writer (agent: unit-test-writer)
  developer (agent: developer)
  tech-lead (agent: tech-lead)

Broadcast to all EXCEPT solution-architect: "Feature slug: <slug>. Spec: specs/<slug>.md.
File ownership is strict — no teammate edits a file they do not own. Wait for your task
assignment. Send all status messages to the team lead."

Spawn solution-architect with this prompt (Task 1 baked in — do NOT send a separate task message):
"You are the solution-architect on team core-<slug>.
Feature slug: <slug>. Spec: specs/<slug>.md. Working directory: <path>.
Your file: specs/<slug>-adr.md only — do not touch any other file.
Task 1: Read specs/<slug>.md. Identify architectural risks, NFRs, external dependencies,
and spec gaps. Write specs/<slug>-adr.md.
When done, send the team lead: 'ADR written. Risks: [list]. Gaps: [list or none].'"
```

**Production team:**
```
Same pattern — spawn solution-architect with Task 1 baked into its prompt.
Broadcast the ownership/wait message to all other teammates.

Team name: "prod-<slug>". Teammates:
  solution-architect (Task 1 in spawn prompt — see core team pattern above)
  application-architect, security-reviewer, unit-test-writer, integration-test-writer,
  developer, devops-engineer, observability-engineer, documentation-writer, tech-lead
  (each loaded from their agent definition of the same name)
```

Add situational agents before creating the team. Their tasks slot in per the
Situational Agents section above.

#### 3. Task Assignment Template

When assigning a task, always include:
- Exact instruction with file paths
- Dependencies: which task numbers must complete first (or "none")
- File ownership: which exact paths this teammate owns for this task
- Completion signal: "When done, send a message to the team lead with: [format]"

**Example — Task 1:**
```
Task for solution-architect:
"Read specs/<slug>.md. Identify architectural risks, NFRs, external dependencies,
and spec gaps. Write specs/<slug>-adr.md. Dependencies: none.
File ownership: specs/<slug>-adr.md only.
When done, send: 'ADR written. Risks: [list]. Gaps: [list or none].'"
```

#### 4. Message-Handling State Machine

When you receive a message from a teammate, respond with the action in the right column:

| Incoming message | Lead action |
|---|---|
| `"ADR written. Risks: X. Gaps: Y."` | If gaps block implementation: STOP, ask user. Else: assign Task 2 to application-architect, attach gap list. |
| `"Design written. Interfaces: [list]."` | Assign Phase B tasks in **parallel** (all at once). |
| `"Tests written and failing. Ready for Developer."` (unit-test-writer) | Mark 3b done. Assign developer only after ALL Phase B tasks are marked done. |
| `"Integration tests written. [N] scenarios."` | Mark 3c done. Same gate check. |
| `"Phase 1 complete. specs/<slug>-security.md written."` | Mark 3a done. Same gate check. |
| `"Implementation complete. Tests: N passing. Smoke test: PASSED. [output]"` | Check: does N match the count unit-test-writer reported? If N is higher, ask developer "Did you add or modify test files? Developer must not touch tests/." and ask unit-test-writer to confirm test count. Only assign Phase D after both confirm no test files were changed. |
| `"Implementation complete but smoke test FAILED. [detail]"` | Block Phase D. Assign developer corrective task: fix the runtime issue and re-run the smoke test. |
| `"All tests passing. Criteria met: [list]. Unit test phase complete."` | Mark 5a done. When 5b also done: assign security-reviewer Phase 2. |
| `"Integration tests passing. [N] scenarios covered."` | Mark 5b done. Same gate check. |
| `"PASS"` (security-reviewer Phase 2) | Assign Phase F tasks in **parallel** (devops, observability, docs). |
| `"[Teammate] done. Artifacts: [list]."` (devops/observability/docs) | Mark that task done. When all three done: assign tech-lead. |
| `"APPROVED"` (tech-lead) | Execute Completion Ceremony (Section 6). |
| `"CHANGES REQUESTED — [teammate] must fix [issue]"` | Assign corrective task to named teammate. Re-assign reviewer after fix. |
| `"FAIL — [reason]"` (any reviewer) | Block downstream tasks. Assign corrective task. Re-assign reviewer after fix. |
| `"BLOCKED — waiting on [input]"` | Find the incomplete upstream task; re-assign it. If it's a spec gap: escalate to user. |
| `"Stuck — tests still failing after 2 attempts. [detail]"` (developer) | See Section 5b — Developer Stuck. |
| Silence (task in-progress, no message after expected time) | See Section 5d — Teammate Silent. |

#### 5. Failure Escalation

**5a. Reviewer blocks (CHANGES REQUESTED / FAIL)**

1. Parse the reviewer's required changes. Each item names a responsible teammate.
2. Assign corrective tasks: "Tech-lead/security-reviewer has requested: [exact text]. Fix only this. Signal team lead when done."
3. Re-assign the reviewer to re-review only the changed files.
4. If the reviewer blocks again on the same issue after one corrective cycle: escalate to user.

**5b. Developer stuck after 2 attempts**

1. Stop the developer task.
2. Assign **both** in parallel:
   - unit-test-writer: "Inspect tests/<slug>/ — are tests correct and consistent with DESIGN.md? Report to team lead."
   - application-architect: "Inspect DESIGN.md — is there ambiguity that makes correct implementation undetermined? Report to team lead."
3. Read both reports. If tests are wrong: assign unit-test-writer to correct them. If design is ambiguous: assign application-architect to amend DESIGN.md.
4. Re-assign developer once. If still stuck: stop and escalate to user with full diagnostic.

**5c. Conflicting outputs**

When two agents' outputs contradict each other:
1. Broadcast: "Conflict detected between [file A] and [file B] on [topic]. Both authors: send a resolution proposal as a message to the team lead. Do not edit files yet."
2. Read both proposals. If they converge: assign the file owner to update. If they diverge: present both to user and ask for a decision.

**5d. Teammate silent**

1. Send direct message: "Status check — Task [N] for [teammate]: are you in progress, blocked, or complete?"
2. If no response: cancel and re-assign the task with a fresh statement.
3. If still silent after re-assignment: escalate to user.

#### 6. Completion Ceremony

**Required conditions — check each explicitly:**

Core team:
- [ ] developer sent `"Implementation complete. Tests: N passing. Smoke test: PASSED."`
- [ ] unit-test-writer sent `"All tests passing. Criteria met: [list]. Unit test phase complete."`
- [ ] tech-lead ran the smoke test independently and recorded PASSED in REVIEW.md
- [ ] tech-lead sent `"APPROVED"`

Production team (all of core plus):
- [ ] integration-test-writer sent `"Integration tests passing."`
- [ ] security-reviewer Phase 2 sent `"PASS"`
- [ ] devops-engineer sent completion message
- [ ] observability-engineer sent completion message
- [ ] documentation-writer sent completion message

**When reporting Done to the user**, include smoke test proof: which command was run and that it returned the expected result.

**When all conditions met:**

1. Broadcast: `"Feature <slug> complete. Verdict: APPROVED. No further tasks will be assigned for this feature."`
2. Report to user:
   - Verdict: "Feature [name] is APPROVED and complete."
   - Test proof: "Unit: N passing. Integration: N passing." (production)
   - Review summary: one line per reviewer with their verdict
   - Artifact index: every file created, grouped by directory, one-sentence description each
3. Tell user to update `specs/<slug>.md` Status to `Done` (you must not edit the spec yourself)

---

### Full File Layout (Production Team Mode)

```
specs/
  <slug>.md                     <- user's spec (never modified)
  <slug>-adr.md                 <- solution-architect
  <slug>-security.md            <- security-reviewer (Phase 1)
src/
  <slug>/
    DESIGN.md                   <- application-architect
    <impl files>                <- developer
    observability/              <- observability-engineer specs
      LOGGING.md
      HEALTH.md
      METRICS.md
tests/
  <slug>/                       <- unit-test-writer
  <slug>/integration/           <- integration-test-writer
deploy/
  <slug>/                       <- devops-engineer
    Dockerfile
    ENV.md
    Makefile
.github/workflows/
  <slug>.yml                    <- devops-engineer
docs/
  <slug>/                       <- documentation-writer + observability-engineer
    README.md
    API.md
    ARCHITECTURE.md
    CONTRIBUTING.md
    RUNBOOK.md
reviews/
  <slug>/                       <- reviewers (each owns their file)
    REVIEW.md                   <- tech-lead
    SECURITY.md                 <- security-reviewer
    API.md                      <- api-designer (if used)
    DATABASE.md                 <- database-specialist (if used)
    PERFORMANCE.md              <- performance-reviewer (if used)
    ACCESSIBILITY.md            <- accessibility-reviewer (if used)
db/
  migrations/                   <- database-specialist (if used)
src/
  <slug>/
    openapi.yaml                <- api-designer (if used, REST)
    schema.graphql              <- api-designer (if used, GraphQL)
    DATABASE.md                 <- database-specialist schema design (if used)
tests/
  <slug>/benchmarks/            <- performance-reviewer (if used)
```
