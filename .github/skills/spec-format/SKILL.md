---
name: spec-format
description: Defines the C# feature specification document format and template. Use when writing, updating, or reviewing a feature spec. Includes sections for requirements, acceptance criteria, constraints, and dependencies.
---

# Feature Specification Format

All specs are stored at `.memory_bank/features/<feature-name>/spec.md`.

## Template

```markdown
# Feature: <Feature Name>

## Status
- **Created:** <date>
- **Last Updated:** <date>
- **Status:** Draft | In Review | Approved | In Progress | Complete

## Overview
Brief description of the feature and its purpose.

## Problem Statement
What problem does this feature solve? Why is it needed?

## Goals
- [ ] Goal 1
- [ ] Goal 2

## Non-Goals
- What this feature explicitly does NOT address

## Functional Requirements

### FR-1: <Requirement Name>
- **Description:** Detailed description
- **Acceptance Criteria:**
  - [ ] AC-1.1: Criteria description
  - [ ] AC-1.2: Criteria description
- **Priority:** Must Have | Should Have | Nice to Have

### FR-2: <Requirement Name>
...

## Non-Functional Requirements

### NFR-1: Performance
- Description and measurable criteria

### NFR-2: Security
- Security considerations

### NFR-3: Scalability
- Scalability requirements

## Technical Constraints
- Framework/platform constraints
- Dependency constraints
- Compatibility requirements

## Dependencies
- External services or libraries
- Other features or modules

## Data Model Changes
- New entities, modified entities, migrations needed

## API Changes
- New endpoints, modified endpoints, contract changes

## UI/UX Changes
- If applicable

## Edge Cases
- Known edge cases to handle

## Open Questions
- [ ] Question 1
- [ ] Question 2

## Revision History
| Date | Author | Changes |
|------|--------|---------|
| <date> | <agent> | Initial draft |
```

## Guidelines

1. **Every requirement must have acceptance criteria** — no vague requirements
2. **Requirements must be testable** — if you can't write a test for it, rewrite it
3. **Use MoSCoW prioritization** — Must Have, Should Have, Nice to Have
4. **Number all requirements** — FR-1, FR-2, NFR-1 for traceability
5. **Flag open questions** — don't leave ambiguity hidden
6. **Update revision history** on every modification
