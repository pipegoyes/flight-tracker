# Travel Date Management - Feature Summary

## 📋 What You'll Get

### 1. Add Travel Dates
- New "Manage Dates" page in the UI
- Form to add date ranges with a name (e.g., "Easter Weekend")
- Automatically starts tracking prices for new dates

### 2. Remove Travel Dates
- Delete button for each date
- **Soft delete** - data is hidden, not destroyed
- All historical price data is preserved
- Stops tracking prices for deleted dates

### 3. Reactivate Deleted Dates
- View list of deleted dates
- "Restore" button to bring them back
- Resume price tracking when restored

---

## 🎨 UI Preview

```
┌─────────────────────────────────────────────────┐
│ Manage Travel Dates                             │
├─────────────────────────────────────────────────┤
│ [+ Add New Date] [🗑️ View Deleted (2)]         │
├─────────────────────────────────────────────────┤
│                                                 │
│ ┌─────────────────────────────────────────────┐ │
│ │ Easter Weekend (Karfreitag-Ostermontag)     │ │
│ │ 2026-04-17 - 2026-04-20 (3 days)            │ │
│ │                            [✏️ Edit] [🗑️ Del]│ │
│ └─────────────────────────────────────────────┘ │
│                                                 │
│ ┌─────────────────────────────────────────────┐ │
│ │ Pfingsten (Pentecost)                       │ │
│ │ 2026-06-05 - 2026-06-08 (3 days)            │ │
│ │                            [✏️ Edit] [🗑️ Del]│ │
│ └─────────────────────────────────────────────┘ │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## 🗄️ Database Changes

### New Fields in TargetDates Table
```
- IsDeleted (bool, default: false)
- DeletedAt (datetime, nullable)
- CreatedAt (datetime, auto-set)
- UpdatedAt (datetime, nullable)
```

**Impact:**
- ✅ Backwards compatible - existing data will work
- ✅ No data loss - soft delete preserves everything
- ✅ Simple migration - adds 4 columns

---

## 🔧 Technical Implementation

### 3-Layer Architecture

**1. Database Layer** (TargetDateRepository)
```csharp
- CreateAsync() - Add new date
- UpdateAsync() - Modify existing date
- SoftDeleteAsync() - Mark as deleted
- RestoreAsync() - Undelete
- GetDeletedAsync() - List deleted dates
```

**2. Service Layer** (ConfigurationService)
- Auto-excludes deleted dates from price checks
- Validates date logic (return > outbound)

**3. UI Layer** (ManageDates.razor)
- Blazor page with forms
- List view with edit/delete buttons
- Separate view for deleted dates

---

## ✅ Benefits

**For You:**
- ✅ No more editing config files
- ✅ Add dates on the fly (e.g., "Found a cheap holiday!")
- ✅ Remove dates without losing history
- ✅ Undo mistakes easily (restore deleted)

**For the App:**
- ✅ Clean code (soft delete pattern)
- ✅ Data integrity (no orphaned price checks)
- ✅ Audit trail (who deleted what when)

---

## 📅 Implementation Timeline

### Phase 1: Database (Day 1, ~4 hours)
- Update entity model
- Create migration
- Update repository
- Unit tests

### Phase 2: UI (Day 2, ~4 hours)
- Create ManageDates page
- Add forms and buttons
- Hook up to repository
- Test interactions

### Total: **2 days** (8 hours work)

---

## 🎯 What Happens Next?

### If You Approve:

1. **I'll create a feature branch** (`feature/manage-travel-dates`)
2. **Implement Phase 1** (database layer)
3. **Show you a demo** (you can test it)
4. **Implement Phase 2** (UI)
5. **Create PR** for you to review
6. **Merge to main** and deploy

### Testing Plan:

- ✅ Add a new date
- ✅ Edit existing date
- ✅ Delete a date (check it disappears)
- ✅ View deleted dates
- ✅ Restore a date
- ✅ Verify price checks skip deleted dates

---

## ❓ Questions to Decide

### 1. Date Validation
Should we prevent adding dates in the past?
- **Option A:** Yes, only future dates (default)
- **Option B:** Allow past dates (for historical tracking)

**Recommendation:** Option A (future only)

### 2. Duplicate Detection
What if you try to add "Easter 2026" twice?
- **Option A:** Allow duplicates (current behavior)
- **Option B:** Warn if overlapping dates exist

**Recommendation:** Option B (warn on overlap)

### 3. Permanent Delete
Should there be a way to permanently delete (not just soft)?
- **Option A:** Soft delete only (safer)
- **Option B:** Add "Delete Forever" button (advanced users)

**Recommendation:** Option A for now (we can add B later if needed)

---

## 💰 Cost Impact

**Zero cost increase:**
- ✅ No new Azure services needed
- ✅ SQLite database (same as now)
- ✅ Runs in existing App Service

---

## 🚀 Ready to Start?

**Approval needed for:**
- ✅ Overall feature design
- ✅ Soft delete approach
- ✅ UI placement (new "Manage Dates" page)
- ✅ Database migration (4 new columns)

**Questions:**
1. Does the UI design look good to you?
2. Any other fields you want (e.g., notes, budget)?
3. Should I start with Phase 1 (database)?

---

**Once approved, I can have Phase 1 (database) ready in a few hours!** 🐸

Let me know if you want any changes to the design or have questions!
