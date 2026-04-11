---
name: sdlc-workflow
description: Defines the complete C# SDLC workflow process including phase ordering, decision points, loop conditions, and artifact locations. Use when orchestrating feature development, understanding the workflow sequence, or determining which phase comes next.
---

# C# SDLC Workflow

## Artifact Location

All artifacts are stored in `.memory_bank/features/<feature-name>/` with this structure:

```
<feature-name>/
├── spec.md                          # Feature specification
├── findings.md                      # Due diligence findings
├── plan.md                          # Overall implementation plan
├── phases/
│   ├── phase-1-<name>.md            # Individual phase details
│   ├── phase-2-<name>.md
│   └── ...
├── test-results/
│   ├── phase-1-test-results.md
│   └── ...
├── code-reviews/
│   ├── phase-1-review-1.md          # Increments on re-review
│   ├── phase-1-review-2.md
│   └── ...
├── phase-reviews/
│   ├── phase-1-completion.md
│   └── ...
└── final-review.md
```

## Workflow Sequence

### Phase 1: Spec Writing
- **Agent:** `spec-writer`
- **Input:** User's feature request/description
- **Output:** `.memory_bank/features/<feature-name>/spec.md`
- **Next:** Due Diligence

### Phase 2: Due Diligence
- **Agent:** `due-diligence`
- **Input:** `spec.md` + codebase analysis
- **Output:** Updated `spec.md` + `findings.md`
- **Next:** Planning

### Phase 3: Planning
- **Agent:** `planner`
- **Input:** `spec.md` + `findings.md`
- **Output:** `plan.md` + `phases/phase-N-<name>.md` for each phase
- **Next:** Phase Implementation Loop

### Phase 4-7: Per-Phase Implementation Loop

For each phase defined in the plan, execute this loop:

```
┌─────────────────────────────────────────────┐
│           PHASE LOOP (per phase)            │
│                                             │
│  4a. Implementer                            │
│   ↓                                         │
│  4b. SQA (write & run tests)               │
│   ├─ PASS → continue                       │
│   └─ FAIL → return to 4a (test fixes)      │
│   ↓                                         │
│  4c. Code Reviewer                          │
│   ├─ APPROVED → continue                   │
│   └─ CHANGES NEEDED → return to 4a (fixes) │
│   ↓                                         │
│  4d. Spec & Phase Reviewer                  │
│   ├─ COMPLETE → next phase                 │
│   └─ INCOMPLETE → return to 4a (gaps)      │
│                                             │
└─────────────────────────────────────────────┘
```

#### 4a. Implementation
- **Agent:** `implementer`
- **Input:** Phase document + (optional: test fixes / code review fixes / gap fixes)
- **Output:** Implemented code, updated phase checklist
- **Context:** Can receive fix requests from SQA, Code Reviewer, or Phase Reviewer

#### 4b. SQA
- **Agent:** `sqa`
- **Input:** `spec.md` + phase document + implemented code
- **Output:** Tests written, `test-results/phase-N-test-results.md`
- **Loop condition:** If tests fail → return to Implementer with test failure details
- **Next on pass:** Code Review

#### 4c. Code Review
- **Agent:** `code-reviewer`
- **Input:** Phase document + implemented code + test results
- **Output:** `code-reviews/phase-N-review-M.md` (M increments per review round)
- **Loop condition:** If changes needed → return to Implementer with review findings. All findings must be addressed before next review iteration.
- **Next on approval:** Spec & Phase Review

#### 4d. Spec & Phase Review
- **Agent:** `spec-phase-reviewer`
- **Input:** `spec.md` + phase document + code + test results + code review
- **Output:** `phase-reviews/phase-N-completion.md`
- **Loop condition:** If gaps found → return to Implementer with gap details. All gaps must be addressed before next review iteration.
- **Next on complete:** Next phase (or Final Review if last phase)

### Phase 8: Final Spec Review
- **Agent:** `final-spec-reviewer`
- **Input:** `spec.md` + all phase documents + all artifacts
- **Output:** `final-review.md`
- **Result:** Feature complete or list of remaining gaps

## Orchestrator Decision Rules

1. **Never skip phases** — every phase must complete before moving to the next
2. **Track loop iterations** — if any inner loop exceeds 10 iterations, surface to user for guidance
3. **Phase checklist must be 100%** before phase review
4. **Always pass context** — each sub-agent invocation must include relevant prior artifacts
5. **Feature folder must exist** before any agent writes to it
6. **Read artifacts between steps** — after each sub-agent completes, read its output files before invoking the next sub-agent
