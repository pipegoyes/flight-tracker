# GitHub Environment Secrets - Copy & Paste

## Important: Your environment is named "Prod" (not "production")

Go to: https://github.com/pipegoyes/flight-tracker/settings/environments/Prod

---

## Secret 1: FLIGHT_PROVIDER_TYPE

**Name:**
```
FLIGHT_PROVIDER_TYPE
```

**Value:**
```
BookingCom
```

---

## Secret 2: FLIGHT_PROVIDER_API_KEY

**Name:**
```
FLIGHT_PROVIDER_API_KEY
```

**Value:**
```
e4256d3703msh7da218ad93c15bep103962jsnd7a711defff2
```

---

## Secret 3: FLIGHT_PROVIDER_API_HOST

**Name:**
```
FLIGHT_PROVIDER_API_HOST
```

**Value:**
```
booking-com15.p.rapidapi.com
```

---

## ⚠️ Important: Update Workflow File

Your workflow file expects environment name `production` but GitHub has `Prod`.

**Option 1 (Recommended):** Rename environment in GitHub from "Prod" to "production"
- Go to https://github.com/pipegoyes/flight-tracker/settings/environments
- Click on "Prod"
- There should be a way to rename it to "production"

**Option 2:** Update workflow to use "Prod" instead of "production"
- This will require a code change

Which would you prefer?
