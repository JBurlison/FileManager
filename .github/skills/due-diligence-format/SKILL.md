---
name: due-diligence-format
description: Defines the due diligence findings document format and analysis process for C# features. Use when performing due diligence analysis, documenting findings, or reviewing integration points and risks.
---

# Due Diligence Format

Findings are stored at `.memory_bank/features/<feature-name>/findings.md`.

## Analysis Process

1. **Review the spec** — identify ambiguities, gaps, and assumptions
2. **Analyze the codebase** — find integration points, affected modules, existing patterns
3. **Assess risks** — technical debt, breaking changes, performance implications
4. **Identify dependencies** — NuGet packages, external services, team dependencies
5. **Update the spec** — incorporate findings, resolve ambiguities, add missing requirements

## Findings Document Template

```markdown
# Due Diligence Findings: <Feature Name>

## Date
<date>

## Codebase Analysis

### Affected Files & Modules
| File/Module | Impact | Notes |
|-------------|--------|-------|
| `path/to/file.cs` | Modified | Description of changes needed |

### Existing Patterns Identified
- Pattern 1: How it's currently done, how to follow it
- Pattern 2: ...

### Integration Points
- Service/module 1: How this feature connects
- Service/module 2: ...

## Risk Assessment

### High Risk
- **Risk:** Description
  - **Impact:** What happens if this goes wrong
  - **Mitigation:** How to address it

### Medium Risk
- ...

### Low Risk
- ...

## Dependency Analysis

### NuGet Packages
| Package | Current Version | Action Needed |
|---------|----------------|---------------|
| Package.Name | x.y.z | Add / Update / None |

### External Services
- Service name: dependency description

## Spec Gaps Found
- [ ] Gap 1: Description — resolution/recommendation
- [ ] Gap 2: ...

## Assumptions
- Assumption 1: Documentation of what was assumed
- ...

## Recommendations
1. Recommendation with rationale
2. ...

## Spec Updates Made
- List of changes made to spec.md as a result of this analysis
```

## Rules

1. **Every integration point must be documented** — no surprise breaking changes
2. **Risks must have mitigations** — identify the problem AND the solution
3. **Spec updates are mandatory** — if you found gaps, update the spec
4. **Search the codebase thoroughly** — use search tools to find all affected code
5. **Document assumptions explicitly** — assumptions are risks in disguise
