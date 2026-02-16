# Playwright Setup Guide

## 🎯 What Are Playwright Tests?

Playwright tests launch a **real Chromium browser** and interact with your app:
- Click buttons
- Type in forms
- Test autocomplete dropdowns
- Verify JavaScript behavior
- Catch UI bugs before deployment

**Coverage:** 9 browser interaction tests (in addition to 7 HTTP validation tests)

---

## 📦 Installation (One-Time Setup)

### Step 1: Install Playwright CLI Tool

```bash
# Install the .NET Playwright CLI tool
cd /home/moltbot/.openclaw/workspace/dev/tests/FlightTracker.IntegrationTests
dotnet tool install --global Microsoft.Playwright.CLI

# Add to PATH
export PATH="$PATH:/home/moltbot/.dotnet/tools"
export DOTNET_ROOT="$HOME/.dotnet"

# Or add permanently to ~/.bashrc:
echo 'export PATH="$PATH:/home/moltbot/.dotnet/tools"' >> ~/.bashrc
echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> ~/.bashrc
source ~/.bashrc
```

---

### Step 2: Install Chromium Browser

```bash
cd /home/moltbot/.openclaw/workspace/dev/tests/FlightTracker.IntegrationTests

# Install Chromium (~200MB download, goes to ~/.cache/ms-playwright/)
playwright install chromium
```

**Note:** Chromium is installed to `~/.cache/ms-playwright/chromium-<version>/`

---

### Step 3: Install System Dependencies (Requires sudo)

Chromium needs system libraries to run. You need sudo access for this:

```bash
sudo apt-get install \
    libatk1.0-0t64 \
    libatk-bridge2.0-0t64 \
    libcups2t64 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2t64 \
    libatspi2.0-0t64
```

**Alternative (easier):**
```bash
# If you have PowerShell installed:
sudo snap install powershell --classic
sudo pwsh tests/FlightTracker.IntegrationTests/bin/Debug/net8.0/playwright.ps1 install-deps
```

---

## 🧪 Running Playwright Tests

### All Tests (29 unit + integration + 9 Playwright)
```bash
dotnet test
```

### Only Playwright Tests (9 browser tests)
```bash
dotnet test --filter "PlaywrightUITests"
```

### Only HTTP Validation Tests (7 tests, no browser needed)
```bash
dotnet test --filter "UIValidationTests"
```

---

## 🐛 Troubleshooting

### "Host system is missing dependencies"
**Problem:** Chromium needs system libraries  
**Solution:** Run Step 3 (install dependencies with sudo)

### "Cannot find module 'playwright'"
**Problem:** Playwright CLI not in PATH  
**Solution:** Export PATH variables (see Step 1)

### "Browser executable doesn't exist"
**Problem:** Chromium not installed  
**Solution:** Run `playwright install chromium` (Step 2)

### Tests fail in Docker
**Problem:** Docker container doesn't have Chromium  
**Solution:** Playwright tests are for local/EC2 only. Use UIValidationTests in Docker.

---

## 🎯 When to Run Which Tests

### During Development (Your Laptop)
```bash
# Quick check (7 tests, <1s)
dotnet test --filter "UIValidationTests"

# Before committing (38 tests, ~10s)
dotnet test
```

### On EC2 Server
```bash
# Full suite including browser tests
dotnet test
```

### In CI/CD Pipeline
```bash
# Only non-browser tests (faster, no dependencies)
dotnet test --filter "FullyQualifiedName!~PlaywrightUITests"
```

---

## 📊 Test Coverage Summary

| Test Suite | Count | Speed | Dependencies | Coverage |
|------------|-------|-------|--------------|----------|
| Unit Tests | 11 | ⚡ Fast | None | Logic & Services |
| Integration Tests | 18 | 🏃 Medium | SQLite | Database & Repos |
| UIValidationTests | 7 | ⚡ Fast | None | HTTP & HTML |
| PlaywrightUITests | 9 | 🐌 Slow | Chromium | Browser Interactions |
| **Total** | **45** | | | **Comprehensive** |

---

## ✅ Verification

After installation, verify everything works:

```bash
# 1. Build project
dotnet build tests/FlightTracker.IntegrationTests/

# 2. Check Chromium is installed
ls ~/.cache/ms-playwright/chromium-*/chrome-linux/chrome

# 3. Run one Playwright test
dotnet test --filter "HomePage_ShouldLoad_AndHaveTitle"
```

If the test passes, you're all set! ✅

---

## 💡 Tips

1. **Headless mode** (default): Tests run without visible browser window
2. **Headed mode** (for debugging): Change `Headless = true` to `false` in `PlaywrightUITests.cs`
3. **Screenshots**: Playwright can capture screenshots on failure (useful for CI/CD)
4. **Browser selection**: Can use Chromium, Firefox, or WebKit

---

## 🚀 Next Steps

Once Playwright is set up:
1. Run `dotnet test` to verify all 45 tests pass
2. Commit your changes with confidence
3. Consider adding more Playwright tests for critical user journeys

**Questions?** Check the [Playwright documentation](https://playwright.dev/dotnet/)
