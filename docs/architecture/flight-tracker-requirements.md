# Flight Tracker - Requirements Document

**Project Goal:** Track flight prices for German long weekends to help find cheap deals to predefined destinations.

## Scope

### In Scope (MVP)
- Manual configuration of target dates and airports
- Price tracking twice daily (6 AM & 6 PM CET)
- SQLite database for price history
- Blazor Server web UI
- Flight details: price, time, airline, stops
- Simple date selector interface
- Single cheapest flight per route
- Exact date searches only
- API abstraction layer (Skyscanner preferred, but swappable)

### Out of Scope (Future)
- Telegram notifications
- Price drop alerts
- Flight filtering (direct/budget/time)
- Auto-calculation of German holidays
- Date flexibility (±1 day search)
- Multiple flight options per route

## Functional Requirements

### FR-1: Configuration
**As a user**, I want to configure:
- Origin airport (default: FRA - Frankfurt)
- List of destination airports (PMI, ARN, TFS, LPA)
- Target travel dates (manual list)

**Format:** JSON or YAML config file
```json
{
  "origin": "FRA",
  "destinations": ["PMI", "ARN", "TFS", "LPA"],
  "targetDates": [
    {
      "outbound": "2026-04-18",
      "return": "2026-04-21",
      "name": "Easter Weekend"
    },
    {
      "outbound": "2026-06-05",
      "return": "2026-06-09",
      "name": "Pentecost"
    }
  ]
}
```

### FR-2: Price Checking
**As a system**, I need to:
- Check prices twice daily (6:00 and 18:00 CET)
- For each target date + destination combination
- Find the single cheapest flight option
- Store results in database with timestamp

**Data captured per flight:**
- Price (EUR)
- Departure time
- Arrival time
- Airline
- Number of stops
- Booking URL
- Timestamp of price check

### FR-3: Data Storage
**Database Schema (SQLite):**

```sql
-- Configuration
CREATE TABLE destinations (
    id INTEGER PRIMARY KEY,
    airport_code TEXT UNIQUE NOT NULL,
    name TEXT NOT NULL
);

CREATE TABLE target_dates (
    id INTEGER PRIMARY KEY,
    outbound_date DATE NOT NULL,
    return_date DATE NOT NULL,
    name TEXT,
    UNIQUE(outbound_date, return_date)
);

-- Price History
CREATE TABLE price_checks (
    id INTEGER PRIMARY KEY,
    target_date_id INTEGER NOT NULL,
    destination_id INTEGER NOT NULL,
    check_timestamp DATETIME NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    currency TEXT DEFAULT 'EUR',
    departure_time TIME NOT NULL,
    arrival_time TIME NOT NULL,
    airline TEXT NOT NULL,
    stops INTEGER NOT NULL,
    booking_url TEXT,
    FOREIGN KEY (target_date_id) REFERENCES target_dates(id),
    FOREIGN KEY (destination_id) REFERENCES destinations(id)
);

CREATE INDEX idx_price_checks_date_dest 
    ON price_checks(target_date_id, destination_id);
CREATE INDEX idx_price_checks_timestamp 
    ON price_checks(check_timestamp);
```

### FR-4: User Interface
**As a user**, I want to:
1. See a list of configured target dates
2. Select a date
3. View current cheapest price for each destination
4. See flight details (time, airline, stops)
5. Click through to booking site

**UI Layout:**
```
┌─────────────────────────────────────┐
│  Flight Tracker                     │
├─────────────────────────────────────┤
│                                     │
│  Select Date:                       │
│  [Easter Weekend (Apr 18-21)] ▼    │
│                                     │
│  Results:                           │
│  ┌───────────────────────────────┐ │
│  │ Mallorca (PMI)         €89    │ │
│  │ Lufthansa · Direct             │ │
│  │ 18 Apr 14:30 → 16:45          │ │
│  │ [Book Now]                    │ │
│  └───────────────────────────────┘ │
│  ┌───────────────────────────────┐ │
│  │ Stockholm (ARN)       €142    │ │
│  │ Ryanair · 1 stop              │ │
│  │ 18 Apr 06:15 → 11:20          │ │
│  │ [Book Now]                    │ │
│  └───────────────────────────────┘ │
│                                     │
└─────────────────────────────────────┘
```

## Non-Functional Requirements

### NFR-1: Performance
- Price check should complete within 5 minutes for all routes
- UI should load results in <2 seconds
- Database queries should be indexed and optimized

### NFR-2: Reliability
- Failed API calls should be retried (3 attempts)
- Missing data should be logged but not crash the system
- Background job should handle server restarts gracefully

### NFR-3: Maintainability
- **API Abstraction:** Use interface to allow swapping flight providers
  ```csharp
  public interface IFlightProvider {
      Task<FlightResult> SearchFlights(
          string origin, 
          string destination, 
          DateTime outbound, 
          DateTime return
      );
  }
  ```
- Logging for all price checks (success/failure)
- Configuration should be editable without code changes
- Docker image for easy deployment

### NFR-4: Extensibility
Design should support future additions:
- Multiple flight options per route
- Price trend charts
- Telegram notifications
- Auto-calculated holiday dates
- Date flexibility (±1 day)

## Technical Constraints

1. **Blazor Server** (not WASM)
2. **SQLite** database (embedded, no separate DB server)
3. **Skyscanner API** preferred, but via abstraction layer
4. **.NET 8+** (latest LTS)
5. **Docker** for deployment
6. **Initial deployment:** Local development
7. **Future deployment:** AWS/Azure container hosting

## User Stories

### US-1: Initial Setup
**As a user**, I want to configure my origin airport and destinations once, so I don't have to enter them repeatedly.

**Acceptance Criteria:**
- Config file in JSON format
- Changes to config require app restart
- Invalid airport codes show clear error message

### US-2: View Current Prices
**As a user**, I want to see the cheapest current flight for each destination when I select a date.

**Acceptance Criteria:**
- Price displayed in EUR
- Flight time and airline visible
- Direct link to booking site
- Data refreshed twice daily

### US-3: Background Price Tracking
**As a system user**, I want prices to be checked automatically twice per day, so data stays fresh without manual intervention.

**Acceptance Criteria:**
- Runs at 6:00 and 18:00 CET
- All configured routes checked
- Failures logged but don't crash the service
- Continues after server restart

## Success Metrics

**MVP is successful if:**
1. ✅ All configured routes are checked twice daily
2. ✅ Price history is stored in database
3. ✅ UI displays current prices clearly
4. ✅ User can click through to book flights
5. ✅ System runs reliably for 1 week without manual intervention
6. ✅ Docker image builds and runs successfully

## Future Enhancements (Post-MVP)

1. **Price Trends:** Show line chart of price history over time
2. **Alerts:** Telegram notification when price drops >10%
3. **German Holidays:** Auto-calculate long weekends from public holiday calendar
4. **Date Flexibility:** Search ±1 day for cheaper options
5. **Multiple Options:** Show top 3 flights per route, not just cheapest
6. **Export:** Download price history as CSV
7. **Mobile App:** Responsive design or native mobile app

---

**Document Version:** 1.0  
**Date:** 2026-02-08  
**Author:** Pepe (AI Assistant)  
**Reviewed By:** Felipe Goyes  
**Status:** Approved
