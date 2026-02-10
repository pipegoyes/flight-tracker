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

### 4. **Shell Validation Script** (Post-Deployment)
**Location:** `validate-ui.sh`

**What it checks:**
- Application responds (HTTP health check)
- Home page loads
- Manage Dates page loads
- Blazor framework present
- UI elements exist ("Add New Date", etc.)
- Docker logs for errors

**Run:**
```bash
./validate-ui.sh
# Or for custom URL:
./validate-ui.sh http://localhost:8080
```

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

# 2. Run validation script
./validate-ui.sh

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
| Integration Tests | 11 | 15+ |
| UI Validation | 6 | 10+ |

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

1. **Playwright/Selenium Tests**
   - Full browser automation
   - Interactive UI testing
   - Screenshot comparison

2. **Visual Regression Testing**
   - Percy.io or similar
   - Catch unexpected UI changes

3. **Load Testing**
   - k6 or Apache Bench
   - Test under concurrent users

4. **API Integration Tests**
   - Test BookingCom provider
   - Mock API responses

---

## ✅ Pre-Deployment Checklist

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] UI validation tests pass
- [ ] `validate-ui.sh` succeeds
- [ ] No errors in Docker logs
- [ ] Manual smoke test of critical paths
- [ ] Code reviewed (self or peer)
- [ ] Documentation updated

---

**Remember:** Tests are cheaper than bugs in production! 🐛➡️✅
