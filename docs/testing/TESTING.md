# Testing Strategy for Flight Tracker

## 🎯 Goal
Catch UI and functionality issues **before** they reach production.

## 📊 Testing Layers

### 1. **Unit Tests** (Fast, Isolated)
**Location:** `tests/FlightTracker.Tests/`

**Coverage:**
- Repository logic
- Service layer (FlightSearchService, PriceHistoryService)
- Provider mocks (MockFlightProvider)

**Run:**
```bash
dotnet test --filter "FullyQualifiedName~FlightTracker.Tests"
```

---

### 2. **Integration Tests** (Database + Logic)
**Location:** `tests/FlightTracker.IntegrationTests/`

**Coverage:**
- Database operations (EF Core)
- Repository patterns
- Data seeding
- Destination selection workflow
- Soft delete/restore operations

**Run:**
```bash
dotnet test --filter "FullyQualifiedName~FlightTracker.IntegrationTests"
```

---

### 3. **UI Validation Tests** (HTTP + HTML)
**Location:** `tests/FlightTracker.IntegrationTests/UIValidationTests.cs`

**Coverage:**
- Pages load successfully (200 OK)
- Blazor framework loads
- Critical UI elements present
- No compilation errors in HTML
- Static assets available

**Run:**
```bash
dotnet test --filter "FullyQualifiedName~UIValidationTests"
```

---

### 4. **Playwright UI Tests** (Browser Automation) 🎭
**Location:** `tests/FlightTracker.IntegrationTests/PlaywrightUITests.cs`

**Coverage:**
- Button clicks work
- Form interactions (typing, selecting)
- Autocomplete dropdown behavior
- Destination chip add/remove
- Navigation between pages
- JavaScript errors
- Real browser testing (Chromium)

**Setup Required:**
See [PLAYWRIGHT_SETUP.md](PLAYWRIGHT_SETUP.md) for installation instructions.

**Run:**
```bash
dotnet test --filter "FullyQualifiedName~PlaywrightUITests"
```

**Note:** Requires Chromium browser + system dependencies. Optional for development.

---

## 🔄 Recommended Workflow

### **Before Committing:**
```bash
# 1. Run all tests
dotnet test

# 2. Check test coverage
dotnet test --collect:"XPlat Code Coverage"
```

### **After Docker Build:**
```bash
# 1. Start container
docker compose up -d

# 2. Run UI validation tests
dotnet test --filter "UIValidationTests"

# 3. Check logs for errors
docker logs flight-tracker 2>&1 | grep -i "error\|exception" | grep -v "HSTS\|DataProtection"
```

### **Manual UI Testing (Critical Paths):**
1. ✅ Add new travel date with destinations
2. ✅ Edit existing date (verify destinations load)
3. ✅ Delete date (soft delete)
4. ✅ Restore deleted date
5. ✅ Autocomplete search (type "ber" → see Berlin)
6. ✅ Select/remove destination chips

---

## 🚨 Common Issues to Watch For

### **DateTime Parsing Errors**
**Symptom:** `System.FormatException: String '' was not recognized as a valid DateTime`  
**Fix:** Use `DateTime.TryParse` instead of `DateTime.Parse`

### **Blazor Interactivity Issues**
**Symptom:** Buttons don't respond to clicks  
**Fix:** Ensure `@rendermode InteractiveServer` is present

### **Autocomplete Not Working**
**Symptom:** Dropdown doesn't appear or loses focus  
**Fix:** Use `@onmousedown:preventDefault` on dropdown

### **Seed Data Missing**
**Symptom:** Empty dropdowns, no destinations when editing  
**Fix:** Check `SeedTargetDateDestinationsAsync` runs after config init

---

## 🧪 Test Coverage Goals

| Layer | Current | Target |
|-------|---------|--------|
| Unit Tests | 11 | 20+ |
| Integration Tests | 18 | 20+ |
| UI Validation (HTTP) | 7 | 10+ |
| UI Automation (Playwright) | 9 | 15+ |
| **Total** | **45** | **65+** |

---

## 📝 Adding New Tests

### **Unit Test Template:**
```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var service = new ServiceUnderTest();
    
    // Act
    var result = service.Method();
    
    // Assert
    Assert.Equal(expected, result);
}
```

### **Integration Test Template:**
```csharp
[Fact]
public async Task Feature_CompleteWorkflow_ShouldSucceed()
{
    // Arrange
    var entity = new Entity { /* ... */ };
    
    // Act
    var created = await _repository.CreateAsync(entity);
    
    // Assert
    Assert.NotNull(created);
    Assert.True(created.Id > 0);
}
```

### **UI Test Template:**
```csharp
[Fact]
public async Task Page_ShouldHave_CriticalElement()
{
    // Act
    var response = await _client.GetAsync("/page");
    var content = await response.Content.ReadAsStringAsync();
    
    // Assert
    response.EnsureSuccessStatusCode();
    Assert.Contains("Expected Text", content);
}
```

---

## 🎯 Future Enhancements

1. **Screenshot Comparison** (Using Playwright)
   - Automated visual regression testing
   - Catch unintended UI changes
   - Compare baseline screenshots

2. **Load Testing**
   - k6 or Apache Bench
   - Test under concurrent users
   - Identify performance bottlenecks

3. **API Integration Tests**
   - Test BookingCom provider with real API
   - Mock API responses for CI/CD
   - Rate limiting tests

4. **More Playwright Scenarios**
   - Complete user journeys (add → edit → delete → restore)
   - Error handling flows
   - Mobile viewport testing

---

## ✅ Pre-Deployment Checklist

- [ ] All unit tests pass (11 tests)
- [ ] All integration tests pass (18 tests)
- [ ] UI validation tests pass (7 tests)
- [ ] Playwright tests pass (9 tests) - *optional if Chromium not installed*
- [ ] No errors in Docker logs
- [ ] Manual smoke test of critical paths
- [ ] Code reviewed (self or peer)
- [ ] Documentation updated

**Quick check (without Playwright):**
```bash
dotnet test --filter "FullyQualifiedName!~PlaywrightUITests"
# Should show: 36/36 tests passed
```

---

**Remember:** Tests are cheaper than bugs in production! 🐛➡️✅
