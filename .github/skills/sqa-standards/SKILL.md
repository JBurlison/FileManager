---
name: sqa-standards
description: Defines SQA testing standards, MSTest patterns, test writing guidelines, and test results documentation format for C# projects. Use when writing tests, documenting test results, or reviewing test quality.
---

# SQA Standards & Testing Guidelines

## Test Framework

**MSTest** is the standard test framework.

## Test Project Structure

```
<Project>.Tests/
├── Unit/
│   ├── Services/
│   ├── Models/
│   └── Utilities/
├── Integration/
│   ├── Api/
│   ├── Data/
│   └── Services/
└── TestHelpers/
    ├── Builders/
    ├── Fakes/
    └── Fixtures/
```

## MSTest Conventions

### Naming
- Test project: `<Project>.Tests`
- Test class: `<ClassUnderTest>Tests`
- Test method: `<MethodName>_<Scenario>_<ExpectedResult>`

### Structure (Arrange-Act-Assert)

```csharp
[TestClass]
public class OrderServiceTests
{
    [TestMethod]
    public void CalculateTotal_WithValidItems_ReturnsCorrectSum()
    {
        // Arrange
        var service = new OrderService();
        var items = new List<OrderItem> { /* ... */ };

        // Act
        var result = service.CalculateTotal(items);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void CalculateTotal_WithNullItems_ThrowsArgumentNull()
    {
        // Arrange
        var service = new OrderService();

        // Act
        service.CalculateTotal(null);
    }

    [DataTestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 10)]
    [DataRow(5, 50)]
    public void CalculateTotal_WithItemCount_ReturnsExpected(int count, decimal expected)
    {
        // Arrange & Act & Assert
    }
}
```

### Test Categories

```csharp
[TestCategory("Unit")]
[TestCategory("Integration")]
[TestCategory("Smoke")]
```

## What to Test

### Must Test
- All public methods
- Edge cases (null, empty, boundary values)
- Error/exception paths
- Business logic and calculations
- Data validation rules
- State transitions

### Should Test
- Integration points (with mocks/fakes)
- Configuration handling
- Mapping/transformation logic

### Test Quality Rules

1. **One assertion concept per test** — test one behavior
2. **No test interdependence** — tests must run in any order
3. **No hardcoded paths or connection strings** — use configuration
4. **Use test builders** for complex object setup
5. **Mock external dependencies** — don't call real services in unit tests
6. **Test names must describe behavior** — readable without seeing code

## Test Results Document

Stored at `.memory_bank/features/<feature-name>/test-results/phase-N-test-results.md`.

### Template

```markdown
# Test Results: Phase N - <Phase Name>

## Run Date
<date>

## Summary

| Metric | Count |
|--------|-------|
| Total Tests | N |
| Passed | N |
| Failed | N |
| Skipped | N |

## Result: PASS / FAIL

## Test Details

### Passed Tests
- `ClassName.MethodName_Scenario_Expected` ✅

### Failed Tests
- `ClassName.MethodName_Scenario_Expected` ❌
  - **Error:** Error message
  - **Root Cause:** Analysis
  - **Fix Required:** Description of fix needed

### Coverage Notes
- Areas covered by these tests
- Areas not yet covered and why

## Findings for Implementer
If FAIL, list specific fixes needed:
1. Fix description referencing the failing test
2. ...
```
