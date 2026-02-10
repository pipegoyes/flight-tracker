#!/bin/bash

# UI Validation Script
# Tests the Flight Tracker application after deployment

set -e

BASE_URL="${1:-http://localhost:8080}"
TIMEOUT=30

echo "🧪 Flight Tracker UI Validation"
echo "================================"
echo "Testing: $BASE_URL"
echo ""

# Wait for app to be ready
echo "⏳ Waiting for application to start..."
for i in {1..30}; do
    if curl -sf "$BASE_URL" > /dev/null 2>&1; then
        echo "✅ Application is responding"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "❌ Application failed to start within ${TIMEOUT}s"
        exit 1
    fi
    sleep 1
done

echo ""
echo "📋 Running validation checks..."
echo ""

# Test 1: Home page loads
echo "1️⃣  Testing home page..."
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL")
if [ "$RESPONSE" == "200" ]; then
    echo "   ✅ Home page loads (HTTP $RESPONSE)"
else
    echo "   ❌ Home page failed (HTTP $RESPONSE)"
    exit 1
fi

# Test 2: Manage Dates page loads
echo "2️⃣  Testing Manage Dates page..."
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/manage-dates")
if [ "$RESPONSE" == "200" ]; then
    echo "   ✅ Manage Dates page loads (HTTP $RESPONSE)"
else
    echo "   ❌ Manage Dates page failed (HTTP $RESPONSE)"
    exit 1
fi

# Test 3: Check for Blazor errors in HTML
echo "3️⃣  Checking for Blazor errors..."
HTML=$(curl -s "$BASE_URL")
if echo "$HTML" | grep -q "blazor.web.js"; then
    echo "   ✅ Blazor framework loaded"
else
    echo "   ❌ Blazor framework not found"
    exit 1
fi

# Test 4: Check for essential UI elements
echo "4️⃣  Checking UI elements..."
if echo "$HTML" | grep -q "Flight Tracker"; then
    echo "   ✅ Page title present"
else
    echo "   ❌ Page title missing"
    exit 1
fi

# Test 5: Check Manage Dates page content
echo "5️⃣  Checking Manage Dates content..."
MANAGE_HTML=$(curl -s "$BASE_URL/manage-dates")
if echo "$MANAGE_HTML" | grep -q "Add New Date"; then
    echo "   ✅ 'Add New Date' button found"
else
    echo "   ❌ 'Add New Date' button missing"
    exit 1
fi

if echo "$MANAGE_HTML" | grep -q "Active Travel Dates"; then
    echo "   ✅ 'Active Travel Dates' section found"
else
    echo "   ❌ 'Active Travel Dates' section missing"
    exit 1
fi

# Test 6: Check for compilation errors in logs
echo "6️⃣  Checking Docker logs for errors..."
if docker logs flight-tracker 2>&1 | grep -i "error\|exception\|failed" | grep -v "HSTS\|DataProtection" | grep -q .; then
    echo "   ⚠️  Warnings found in logs:"
    docker logs flight-tracker 2>&1 | grep -i "error\|exception\|failed" | grep -v "HSTS\|DataProtection" | tail -5
else
    echo "   ✅ No critical errors in logs"
fi

echo ""
echo "================================"
echo "✅ All validation checks passed!"
echo ""
echo "💡 Next: Use browser tool for interactive testing"
echo "   - Test autocomplete functionality"
echo "   - Verify button clicks"
echo "   - Check for JavaScript errors"
