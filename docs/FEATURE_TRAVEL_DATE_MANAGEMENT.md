# ✈️ Feature Design: Travel Date Management

**Created:** 2026-02-10  
**Status:** Design Phase  
**Priority:** High  

---

## 📋 Requirements

### 1. Add Travel Dates
- Users can create new target date ranges (outbound + return)
- Provide name/description for the date range
- Validate date logic (return after outbound)
- Automatically trigger price checks for new dates

### 2. Remove Travel Dates (Soft Delete)
- Mark dates as deleted instead of physical deletion
- Preserve historical price data
- Hide deleted dates from main UI
- Keep database integrity for reporting

### 3. Reactivate Deleted Dates
- View list of deleted dates
- Restore previously deleted dates
- Resume price tracking for reactivated dates

---

## 🏗️ Database Design

### Updated Entity: TargetDate

```csharp
public class TargetDate
{
    public int Id { get; set; }
    public DateTime OutboundDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // NEW: Soft delete support
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    // NEW: Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<PriceCheck> PriceChecks { get; set; } = new List<PriceCheck>();
}
```

### Migration Required

```csharp
// Migration: AddSoftDeleteToTargetDate
public partial class AddSoftDeleteToTargetDate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            table: "TargetDates",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "DeletedAt",
            table: "TargetDates",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedAt",
            table: "TargetDates",
            type: "TEXT",
            nullable: false,
            defaultValue: DateTime.UtcNow);

        migrationBuilder.AddColumn<string>(
            name: "UpdatedAt",
            table: "TargetDates",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsDeleted", table: "TargetDates");
        migrationBuilder.DropColumn(name: "DeletedAt", table: "TargetDates");
        migrationBuilder.DropColumn(name: "CreatedAt", table: "TargetDates");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "TargetDates");
    }
}
```

---

## 🔧 Repository Layer Updates

### ITargetDateRepository (New Methods)

```csharp
public interface ITargetDateRepository : IRepository<TargetDate>
{
    // Existing methods
    Task<TargetDate?> GetByDatesAsync(DateTime outbound, DateTime returnDate, CancellationToken ct = default);
    Task<IEnumerable<TargetDate>> GetUpcomingAsync(CancellationToken ct = default);
    
    // NEW: Soft delete methods
    Task<IEnumerable<TargetDate>> GetAllIncludingDeletedAsync(CancellationToken ct = default);
    Task<IEnumerable<TargetDate>> GetDeletedAsync(CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
    Task<bool> RestoreAsync(int id, CancellationToken ct = default);
    
    // NEW: CRUD for user management
    Task<TargetDate> CreateAsync(TargetDate targetDate, CancellationToken ct = default);
    Task<bool> UpdateAsync(TargetDate targetDate, CancellationToken ct = default);
}
```

### Implementation: TargetDateRepository

```csharp
public class TargetDateRepository : Repository<TargetDate>, ITargetDateRepository
{
    public TargetDateRepository(FlightTrackerDbContext context) : base(context) { }

    // Override to exclude soft-deleted by default
    public override async Task<IEnumerable<TargetDate>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.TargetDates
            .Where(td => !td.IsDeleted)
            .OrderBy(td => td.OutboundDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TargetDate>> GetUpcomingAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        return await _context.TargetDates
            .Where(td => !td.IsDeleted && td.OutboundDate >= today)
            .OrderBy(td => td.OutboundDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TargetDate>> GetAllIncludingDeletedAsync(CancellationToken ct = default)
    {
        return await _context.TargetDates
            .OrderBy(td => td.OutboundDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TargetDate>> GetDeletedAsync(CancellationToken ct = default)
    {
        return await _context.TargetDates
            .Where(td => td.IsDeleted)
            .OrderByDescending(td => td.DeletedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.TargetDates.FindAsync(new object[] { id }, ct);
        if (entity == null || entity.IsDeleted)
            return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RestoreAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.TargetDates
            .IgnoreQueryFilters() // Include deleted
            .FirstOrDefaultAsync(td => td.Id == id, ct);
        
        if (entity == null || !entity.IsDeleted)
            return false;

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TargetDate> CreateAsync(TargetDate targetDate, CancellationToken ct = default)
    {
        targetDate.CreatedAt = DateTime.UtcNow;
        targetDate.IsDeleted = false;

        await _context.TargetDates.AddAsync(targetDate, ct);
        await _context.SaveChangesAsync(ct);

        return targetDate;
    }

    public async Task<bool> UpdateAsync(TargetDate targetDate, CancellationToken ct = default)
    {
        var existing = await _context.TargetDates.FindAsync(new object[] { targetDate.Id }, ct);
        if (existing == null || existing.IsDeleted)
            return false;

        existing.Name = targetDate.Name;
        existing.OutboundDate = targetDate.OutboundDate;
        existing.ReturnDate = targetDate.ReturnDate;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}
```

---

## 🎨 UI Components (Blazor)

### New Page: ManageDates.razor

```razor
@page "/manage-dates"
@inject ITargetDateRepository TargetDateRepository
@inject NavigationManager Navigation

<PageTitle>Manage Travel Dates</PageTitle>

<h1>Manage Travel Dates</h1>

<div class="row mb-4">
    <div class="col">
        <button class="btn btn-primary" @onclick="ShowAddForm">
            <i class="bi bi-plus-circle"></i> Add New Date
        </button>
        
        <button class="btn btn-outline-secondary" @onclick="ShowDeletedDates">
            <i class="bi bi-trash"></i> View Deleted (@deletedCount)
        </button>
    </div>
</div>

@if (showAddForm)
{
    <div class="card mb-4">
        <div class="card-header">
            <h5>@(editingDate == null ? "Add" : "Edit") Travel Date</h5>
        </div>
        <div class="card-body">
            <EditForm Model="@newDate" OnValidSubmit="SaveDate">
                <DataAnnotationsValidator />
                <ValidationSummary />

                <div class="mb-3">
                    <label class="form-label">Name</label>
                    <InputText @bind-Value="newDate.Name" class="form-control" 
                               placeholder="e.g., Easter Weekend" />
                </div>

                <div class="row">
                    <div class="col-md-6 mb-3">
                        <label class="form-label">Outbound Date</label>
                        <InputDate @bind-Value="newDate.OutboundDate" class="form-control" />
                    </div>
                    <div class="col-md-6 mb-3">
                        <label class="form-label">Return Date</label>
                        <InputDate @bind-Value="newDate.ReturnDate" class="form-control" />
                    </div>
                </div>

                <div class="d-flex gap-2">
                    <button type="submit" class="btn btn-success">
                        <i class="bi bi-check-circle"></i> Save
                    </button>
                    <button type="button" class="btn btn-secondary" @onclick="CancelEdit">
                        Cancel
                    </button>
                </div>
            </EditForm>
        </div>
    </div>
}

<div class="row">
    @if (showDeleted)
    {
        <div class="col-12">
            <h3>Deleted Travel Dates</h3>
            @if (deletedDates.Any())
            {
                <div class="alert alert-info">
                    These dates are hidden from tracking. Restore to resume price checks.
                </div>
                
                @foreach (var date in deletedDates)
                {
                    <div class="card mb-2 border-danger">
                        <div class="card-body d-flex justify-content-between align-items-center">
                            <div>
                                <h5 class="card-title mb-1">@date.Name</h5>
                                <small class="text-muted">
                                    @date.OutboundDate.ToShortDateString() - @date.ReturnDate.ToShortDateString()
                                </small>
                                <br />
                                <small class="text-danger">
                                    Deleted: @date.DeletedAt?.ToString("yyyy-MM-dd HH:mm")
                                </small>
                            </div>
                            <button class="btn btn-sm btn-success" @onclick="() => RestoreDate(date.Id)">
                                <i class="bi bi-arrow-counterclockwise"></i> Restore
                            </button>
                        </div>
                    </div>
                }
            }
            else
            {
                <p class="text-muted">No deleted dates.</p>
            }
            
            <button class="btn btn-secondary mt-3" @onclick="() => showDeleted = false">
                Back to Active Dates
            </button>
        </div>
    }
    else
    {
        <div class="col-12">
            <h3>Active Travel Dates</h3>
            
            @if (activeDates.Any())
            {
                @foreach (var date in activeDates)
                {
                    <div class="card mb-2">
                        <div class="card-body d-flex justify-content-between align-items-center">
                            <div>
                                <h5 class="card-title mb-1">@date.Name</h5>
                                <small class="text-muted">
                                    @date.OutboundDate.ToShortDateString() - @date.ReturnDate.ToShortDateString()
                                </small>
                                <br />
                                <small class="text-muted">
                                    Duration: @(date.ReturnDate - date.OutboundDate).Days days
                                </small>
                            </div>
                            <div class="d-flex gap-2">
                                <button class="btn btn-sm btn-outline-primary" 
                                        @onclick="() => EditDate(date)">
                                    <i class="bi bi-pencil"></i>
                                </button>
                                <button class="btn btn-sm btn-outline-danger" 
                                        @onclick="() => DeleteDate(date.Id)">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                }
            }
            else
            {
                <div class="alert alert-warning">
                    No active travel dates. Add one to start tracking prices!
                </div>
            }
        </div>
    }
</div>

@code {
    private List<TargetDate> activeDates = new();
    private List<TargetDate> deletedDates = new();
    private int deletedCount = 0;
    
    private bool showAddForm = false;
    private bool showDeleted = false;
    private TargetDate? editingDate = null;
    
    private TargetDateModel newDate = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDates();
    }

    private async Task LoadDates()
    {
        activeDates = (await TargetDateRepository.GetAllAsync()).ToList();
        deletedDates = (await TargetDateRepository.GetDeletedAsync()).ToList();
        deletedCount = deletedDates.Count;
    }

    private void ShowAddForm()
    {
        editingDate = null;
        newDate = new TargetDateModel
        {
            OutboundDate = DateTime.Today.AddMonths(1),
            ReturnDate = DateTime.Today.AddMonths(1).AddDays(3)
        };
        showAddForm = true;
    }

    private void EditDate(TargetDate date)
    {
        editingDate = date;
        newDate = new TargetDateModel
        {
            Name = date.Name,
            OutboundDate = date.OutboundDate,
            ReturnDate = date.ReturnDate
        };
        showAddForm = true;
    }

    private void CancelEdit()
    {
        showAddForm = false;
        editingDate = null;
    }

    private async Task SaveDate()
    {
        if (editingDate == null)
        {
            // Create new
            var targetDate = new TargetDate
            {
                Name = newDate.Name,
                OutboundDate = newDate.OutboundDate,
                ReturnDate = newDate.ReturnDate
            };

            await TargetDateRepository.CreateAsync(targetDate);
        }
        else
        {
            // Update existing
            editingDate.Name = newDate.Name;
            editingDate.OutboundDate = newDate.OutboundDate;
            editingDate.ReturnDate = newDate.ReturnDate;

            await TargetDateRepository.UpdateAsync(editingDate);
        }

        showAddForm = false;
        editingDate = null;
        await LoadDates();
    }

    private async Task DeleteDate(int id)
    {
        if (await TargetDateRepository.SoftDeleteAsync(id))
        {
            await LoadDates();
        }
    }

    private async Task RestoreDate(int id)
    {
        if (await TargetDateRepository.RestoreAsync(id))
        {
            await LoadDates();
            showDeleted = false;
        }
    }

    private void ShowDeletedDates()
    {
        showDeleted = true;
    }

    // Model for form binding
    private class TargetDateModel
    {
        public string Name { get; set; } = string.Empty;
        public DateTime OutboundDate { get; set; }
        public DateTime ReturnDate { get; set; }
    }
}
```

### Update Navigation (NavMenu.razor)

```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="manage-dates">
        <span class="bi bi-calendar-plus" aria-hidden="true"></span> Manage Dates
    </NavLink>
</div>
```

---

## 🔄 Service Layer Updates

### FlightSearchService (Updated)

```csharp
// Update SearchAllRoutesAsync to skip deleted dates
public async Task<int> SearchAllRoutesAsync(
    string originAirportCode,
    CancellationToken cancellationToken = default)
{
    var successCount = 0;

    // Get all upcoming target dates (excluding soft-deleted)
    var targetDates = await _targetDateRepository.GetUpcomingAsync(cancellationToken);

    // ... rest of implementation
}
```

---

## 🎯 Implementation Plan

### Phase 1: Database Layer (Day 1)
1. ✅ Update `TargetDate` entity with soft delete fields
2. ✅ Create EF Core migration
3. ✅ Update `ITargetDateRepository` interface
4. ✅ Implement repository methods
5. ✅ Add unit tests for repository

### Phase 2: Service Layer (Day 1)
1. ✅ Update `ConfigurationService` to handle deleted dates
2. ✅ Update `FlightSearchService` to skip deleted dates
3. ✅ Add validation for date logic (return > outbound)
4. ✅ Add unit tests for services

### Phase 3: UI (Day 2)
1. ✅ Create `ManageDates.razor` page
2. ✅ Add form for adding/editing dates
3. ✅ Add delete button with confirmation
4. ✅ Add view for deleted dates
5. ✅ Add restore functionality
6. ✅ Update navigation menu

### Phase 4: Testing (Day 2)
1. ✅ Test CRUD operations
2. ✅ Test soft delete
3. ✅ Test restore
4. ✅ Test that price checks skip deleted dates
5. ✅ Test UI interactions

### Phase 5: Documentation (Day 2)
1. ✅ Update README.md
2. ✅ Add user guide for managing dates
3. ✅ Update API documentation

---

## 📊 Data Migration Strategy

### Existing Data
All existing `TargetDate` records will:
- Have `IsDeleted = false` (default)
- Have `CreatedAt = migration time`
- Have `DeletedAt = null`
- Have `UpdatedAt = null`

### No Data Loss
- Soft delete preserves all historical `PriceCheck` data
- Relationships remain intact
- Reporting and analytics can still access deleted dates

---

## 🧪 Test Scenarios

### Unit Tests
```csharp
[Fact]
public async Task SoftDelete_MarksAsDeleted_AndPreservesData()
{
    // Arrange
    var targetDate = new TargetDate { Name = "Test", ... };
    await _repository.CreateAsync(targetDate);

    // Act
    var result = await _repository.SoftDeleteAsync(targetDate.Id);

    // Assert
    result.Should().BeTrue();
    var deleted = await _repository.GetAllIncludingDeletedAsync();
    deleted.Should().Contain(td => td.Id == targetDate.Id && td.IsDeleted);
}

[Fact]
public async Task Restore_ReactivatesDeletedDate()
{
    // Arrange
    var targetDate = new TargetDate { ... };
    await _repository.CreateAsync(targetDate);
    await _repository.SoftDeleteAsync(targetDate.Id);

    // Act
    var result = await _repository.RestoreAsync(targetDate.Id);

    // Assert
    result.Should().BeTrue();
    var active = await _repository.GetAllAsync();
    active.Should().Contain(td => td.Id == targetDate.Id && !td.IsDeleted);
}
```

---

## 🔐 Security Considerations

### Validation
- ✅ Return date must be after outbound date
- ✅ Dates must not be in the past (configurable)
- ✅ Name is required and max 200 characters
- ✅ Prevent duplicate date ranges

### Authorization
- Future: Add user authentication
- Future: Users can only manage their own dates

---

## 💡 Future Enhancements

### Phase 2 Features
- [ ] Bulk operations (delete/restore multiple)
- [ ] Date templates (e.g., "Long weekends 2026")
- [ ] Import/export dates
- [ ] Duplicate date detection
- [ ] Price trend analysis per date
- [ ] Email notifications for new dates

### Phase 3 Features
- [ ] Recurring dates (e.g., "every long weekend")
- [ ] Date groups/categories
- [ ] Budget per date
- [ ] Multi-user support

---

## 📈 Impact Analysis

### Benefits
✅ Users control their own travel dates  
✅ No configuration file editing needed  
✅ Preserve historical data  
✅ Flexible date management  
✅ Easy to correct mistakes (restore)  

### Risks
⚠️ Database migration required  
⚠️ Existing price checks reference old dates  
⚠️ Need to handle timezone correctly  

### Mitigation
✅ Migration is backwards compatible  
✅ Soft delete preserves relationships  
✅ All dates stored as UTC  

---

## 🚀 Deployment Checklist

- [ ] Run database migration
- [ ] Update configuration service
- [ ] Deploy new UI components
- [ ] Test on staging environment
- [ ] Update user documentation
- [ ] Deploy to production
- [ ] Monitor for errors

---

**Estimated Effort:** 2-3 days  
**Complexity:** Medium  
**Priority:** High  

**Next Steps:**
1. Review and approve design
2. Create feature branch
3. Implement Phase 1 (database layer)
4. Get feedback on Phase 1
5. Continue with Phase 2-3

---

**Designed by Pepe 🐸**  
**Date:** 2026-02-10
