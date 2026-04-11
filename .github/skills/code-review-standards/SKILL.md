---
name: code-review-standards
description: Defines C# code review standards, review checklist, and review document format. Use when reviewing code, documenting review findings, or understanding code quality expectations.
---

# Code Review Standards

## Review Checklist

### Correctness
- [ ] Code implements the requirements correctly
- [ ] Edge cases are handled
- [ ] Error handling is appropriate (not swallowing exceptions)
- [ ] Null checks where needed (nullable reference types preferred)
- [ ] No off-by-one errors in loops/collections
- [ ] Async/await used correctly (no async void, no .Result blocking)

### Architecture & Design
- [ ] Follows existing patterns in the codebase
- [ ] Single Responsibility Principle maintained
- [ ] Dependencies injected (not `new`-ed internally for services)
- [ ] Interfaces used for testability where appropriate
- [ ] No unnecessary abstractions or over-engineering

### Performance
- [ ] No unnecessary allocations in hot paths
- [ ] Collections sized appropriately (List capacity, Dictionary)
- [ ] LINQ queries not causing N+1 or excessive enumeration
- [ ] Async I/O for all I/O operations
- [ ] No blocking calls on async code paths
- [ ] String building uses StringBuilder for loops
- [ ] Latency prioritized over memory usage

### Security
- [ ] No SQL injection vulnerabilities (parameterized queries)
- [ ] Input validation at system boundaries
- [ ] No sensitive data in logs
- [ ] Authentication/authorization checks in place
- [ ] No hardcoded secrets or credentials

### Code Quality
- [ ] Naming follows .NET conventions (PascalCase for public, camelCase for private)
- [ ] No dead code or commented-out blocks
- [ ] Methods under 30 lines (guideline, not absolute)
- [ ] Cyclomatic complexity reasonable
- [ ] Magic numbers/strings extracted to constants
- [ ] XML doc comments on public API

### Testing
- [ ] New code has corresponding tests
- [ ] Tests follow MSTest conventions
- [ ] Test names describe behavior
- [ ] No test coupling or order dependency

## Severity Levels

- **Critical:** Must fix — security vulnerability, data loss risk, crash
- **Major:** Should fix — bug, performance issue, design violation
- **Minor:** Consider fixing — style, naming, minor optimization
- **Nit:** Optional — preference, cosmetic

## Review Document

Stored at `.memory_bank/features/<feature-name>/code-reviews/phase-N-review-M.md`.

### Template

```markdown
# Code Review: Phase N - <Phase Name> (Review #M)

## Date
<date>

## Result: APPROVED / CHANGES NEEDED

## Summary
Brief overall assessment.

## Findings

### Critical
- **File:** `path/to/file.cs` (Line N)
  - **Issue:** Description
  - **Fix:** Recommended fix

### Major
- ...

### Minor
- ...

### Nits
- ...

## Positive Notes
- Good patterns observed
- Well-handled edge cases

## Changes Required (if CHANGES NEEDED)
1. Fix description with file references
2. ...
```
