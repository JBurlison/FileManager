---
name: plan-format
description: Defines the implementation plan and individual phase document formats for C# feature development. Use when creating plans, breaking work into phases, or defining task checklists.
---

# Plan & Phase Document Formats

## Overall Plan

Stored at `.memory_bank/features/<feature-name>/plan.md`.

### Plan Template

```markdown
# Implementation Plan: <Feature Name>

## Overview
Brief summary of the implementation approach.

## Phases

| Phase | Name | Description | Est. Complexity |
|-------|------|-------------|-----------------|
| 1 | <Phase Name> | Brief description | Low/Medium/High |
| 2 | <Phase Name> | Brief description | Low/Medium/High |
| ... | ... | ... | ... |

## Phase Order Rationale
Why phases are ordered this way. Dependencies between phases.

## Shared Considerations
- Patterns to follow across all phases
- Naming conventions specific to this feature
- Shared utilities or base classes needed

## Definition of Done
- [ ] All phases complete
- [ ] All tests passing
- [ ] All code reviews approved
- [ ] All phase reviews complete
- [ ] Final spec review approved
```

## Individual Phase Document

Stored at `.memory_bank/features/<feature-name>/phases/phase-N-<name>.md`.

### Phase Template

```markdown
# Phase N: <Phase Name>

## Status
- **Status:** Not Started | In Progress | Implementation Complete | Testing | Review | Complete
- **Implementer Iterations:** 0
- **Review Iterations:** 0

## Objective
What this phase accomplishes.

## Prerequisites
- Dependencies on prior phases
- Required setup or configuration

## Tasks

### Task 1: <Task Name>
- [ ] Step 1.1: Description
- [ ] Step 1.2: Description
- [ ] Step 1.3: Description

### Task 2: <Task Name>
- [ ] Step 2.1: Description
- [ ] Step 2.2: Description

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `path/to/file.cs` | Create | Description |
| `path/to/existing.cs` | Modify | Description |

## Acceptance Criteria (from Spec)
- [ ] AC reference: description
- [ ] AC reference: description

## Testing Notes
- Key scenarios to test in this phase
- Edge cases specific to this phase

## Implementation Log

| Date | Action | Details |
|------|--------|---------|
```

## Guidelines

1. **Phases should be independently testable** — each phase produces working, testable code
2. **Keep phases small** — prefer more smaller phases over fewer large ones
3. **Task steps should be atomic** — one step = one clear action
4. **Map acceptance criteria to phases** — every AC must be covered by at least one phase
5. **Update status fields** as the phase progresses
6. **Log implementation actions** in the Implementation Log
