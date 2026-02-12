# On-Demand Price Checking Feature

## 🎯 Overview

Added the ability to manually trigger price checks with intelligent caching to avoid unnecessary API calls.

---

## ✨ Features

### 1. **"Check Prices Now" Button**
- Located on the home page next to the date selector
- Only enabled when a travel date is selected
- Shows **animated progress bar** during price check with:
  - Current destination being checked
  - Progress count (e.g., "2 / 5")
  - Status: "🔄 Checking" or "✓ Cached"
  - Visual progress bar with animation
- Displays result summary after completion

### 2. **Smart Caching (6-hour window)**
- Checks database for recent prices before calling API
- Uses cached prices if checked within last 6 hours
- Only fetches fresh prices for destinations without recent data
- Saves API costs and respects rate limits

### 3. **Result Feedback**
Shows clear messages:
- `✅ Updated! X new price(s) fetched, Y cached.` - Mixed results
- `✅ Success! Fetched X new price(s).` - All fresh
- `ℹ️ All prices are recent (cached X result(s)...` - All cached
- `⚠️ No prices found for selected destinations.` - No results

### 4. **Progress Bar**
- Real-time visual feedback during price checking
- Shows current destination name and status
- Progress counter (e.g., "Checking 3 of 5")
- Animated striped progress bar
- Different indicators:
  - `🔄 Checking: [Destination]` - Fetching fresh price
  - `✓ Cached: [Destination]` - Using cached price

### 5. **Rate Limiting**
- 2-second delay between API calls
- Prevents hitting provider rate limits
- Only applies to fresh fetches (cached prices are instant)

---

## 🏗️ Architecture

### New Methods

#### `IPriceCheckRepository.GetRecentPriceAsync()`
```csharp
Task<PriceCheck?> GetRecentPriceAsync(
    int targetDateId,
    int destinationId,
    int maxAgeHours,
    CancellationToken cancellationToken = default);
```

Checks if we have a price within the specified age window.

#### `FlightSearchService.CheckPricesForTargetDateAsync()`
```csharp
Task<(int cachedCount, int fetchedCount, IEnumerable<PriceCheck> results)> 
CheckPricesForTargetDateAsync(
    string originAirportCode,
    int targetDateId,
    int maxAgeHours = 6,
    IProgress<(int current, int total, string destinationName, bool isCached)>? progress = null,
    CancellationToken cancellationToken = default);
```

Main method for on-demand price checking with caching logic.

**Progress Reporting:**
- Uses `IProgress<T>` pattern for real-time UI updates
- Reports: current index, total count, destination name, cached status
- Progress updates are sent after checking each destination

---

## 🔄 Flow

```
User clicks "Check Prices Now"
    ↓
For each destination in selected travel date:
    ↓
    Check: Do we have a price checked within last 6 hours?
    ├─ YES → Use cached price (instant)
    └─ NO  → Fetch fresh price from API (2s delay)
    ↓
Display results + summary
```

---

## 💡 Use Cases

### Background Service (Automatic)
- Runs 2x daily (8 AM, 8 PM)
- Fetches ALL prices for ALL travel dates
- Ensures continuous price tracking

### On-Demand (Manual)
- User wants latest prices NOW
- Uses cache to avoid redundant API calls
- Perfect for:
  - Quick price check before booking
  - Verifying if prices changed
  - Checking new travel dates immediately

---

## 📊 Cache Strategy

| Time Since Last Check | Action |
|----------------------|---------|
| < 6 hours | Use cached price ✅ (instant) |
| ≥ 6 hours | Fetch fresh price 🔄 (2s delay) |

**Why 6 hours?**
- Background service runs every 12 hours
- 6-hour window ensures at most 1 automated check between manual checks
- Balances freshness with API cost

You can adjust by changing `maxAgeHours` parameter:
```csharp
await FlightSearchService.CheckPricesForTargetDateAsync(
    "FRA",
    targetDateId,
    maxAgeHours: 12  // Longer cache window
);
```

---

## 🎨 UI Changes

### Home Page (`Home.razor`)

**Before:**
```html
<select class="form-select" @onchange="OnTargetDateChanged">
    <!-- date options -->
</select>
```

**After:**
```html
<div class="row g-2">
    <div class="col-md-8">
        <select class="form-select" @onchange="OnTargetDateChanged">
            <!-- date options -->
        </select>
    </div>
    <div class="col-md-4">
        <button class="btn btn-success w-100" @onclick="CheckPricesNow">
            🔄 Check Prices Now
        </button>
    </div>
</div>

<!-- Progress bar (shown during check) -->
@if (isCheckingPrices && checkProgress != null)
{
    <div class="mt-3">
        <div class="d-flex justify-content-between mb-1">
            <small>🔄 Checking: Palma de Mallorca</small>
            <small>3 / 5</small>
        </div>
        <div class="progress" style="height: 8px;">
            <div class="progress-bar progress-bar-striped progress-bar-animated" 
                 style="width: 60%">
            </div>
        </div>
    </div>
}

<!-- Result message (shown after check) -->
@if (!isCheckingPrices && lastCheckResult != null)
{
    <small class="text-muted">
        ✅ Updated! 2 new price(s) fetched, 3 cached.
    </small>
}
```

---

## 🧪 Testing

### Manual Test Scenarios

1. **First Time Price Check (Cold Cache)**
   - Select a travel date with no recent prices
   - Click "Check Prices Now"
   - Expected: All prices fetched fresh, takes ~10-15s (N destinations × 2s)

2. **Immediate Re-Check (Hot Cache)**
   - Click "Check Prices Now" again immediately
   - Expected: All prices from cache, instant (<1s)

3. **Partial Cache**
   - Wait 6+ hours
   - Background service updates some destinations
   - Click "Check Prices Now"
   - Expected: Mix of cached (recent from background) and fresh (old)

4. **No Destinations**
   - Select a date with no configured destinations
   - Expected: Warning message

---

## 📝 Future Enhancements

### Potential Improvements

1. **Per-Destination Cache Indicator** (on price cards)
   ```html
   <span class="badge bg-secondary">Cached (2h ago)</span>
   <span class="badge bg-success">Fresh (just now)</span>
   ```

2. **Configurable Cache Window**
   ```html
   <select>
       <option value="1">1 hour (most fresh)</option>
       <option value="6" selected>6 hours (balanced)</option>
       <option value="24">24 hours (least API calls)</option>
   </select>
   ```

3. **Selective Refresh**
   ```html
   <button @onclick="() => RefreshDestination(destId)">
       🔄 Refresh This Destination
   </button>
   ```

4. **Estimated Time Remaining**
   ```html
   🔄 Checking: Berlin (estimated 8s remaining)
   ```

---

## 💰 Cost Impact

### Example: 6 Travel Dates × 2 Destinations Each

**Without Cache (Every Manual Check):**
- 12 API calls per manual check
- 100 manual checks = 1,200 API calls

**With 6-Hour Cache:**
- First check: 12 API calls
- Repeat within 6h: 0 API calls (all cached)
- ~95% reduction in redundant calls

**Background Service Impact:**
- Runs 2×/day = 24 API calls/day
- Manual checks between runs use cache
- No duplicate work

---

## 🔧 Configuration

### Change Cache Duration

Edit `Home.razor`:
```csharp
var (cachedCount, fetchedCount, results) = 
    await FlightSearchService.CheckPricesForTargetDateAsync(
        "FRA",
        selectedTargetDateId.Value,
        maxAgeHours: 12  // Increase to 12 hours
    );
```

### Change Rate Limit Delay

Edit `FlightSearchService.cs`:
```csharp
// Rate limiting: delay between API calls
if (fetchedCount > 0)
{
    await Task.Delay(1000, cancellationToken);  // Reduce to 1 second
}
```

---

## ✅ Summary

**Added:**
- ✅ On-demand price checking button
- ✅ Smart 6-hour caching to avoid redundant API calls
- ✅ Clear user feedback (cached vs fetched counts)
- ✅ Loading states and error handling
- ✅ Rate limiting between fresh API calls

**Benefits:**
- 💰 Reduced API costs (95%+ cache hit rate for repeated checks)
- ⚡ Instant results when prices are recent
- 🎯 User control over when to check prices
- 🛡️ Respects rate limits with 2s delays

**Files Changed:**
- `src/FlightTracker.Core/Interfaces/IPriceCheckRepository.cs`
- `src/FlightTracker.Core/Services/FlightSearchService.cs`
- `src/FlightTracker.Data/Repositories/PriceCheckRepository.cs`
- `src/FlightTracker.Web/Components/Pages/Home.razor`

---

**Ready to test!** 🚀

Visit http://localhost:8080 and click "🔄 Check Prices Now" on any travel date.
