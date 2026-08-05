# Quick Rebuild & Test

## Quick Steps

1. **Build locally**
   ```powershell
   cd C:\Users\Billions\source\repos\Laitorerp\Leitor.Erp
   dotnet build -c Release
   ```

2. **Docker build & push** (or redeploy via Coolify)
   ```powershell
   cd C:\Users\Billions\source\repos\Laitorerp
   docker build -t leitorerp:latest -f Dockerfile .
   ```

3. **Wait for deployment** in Coolify dashboard

## Test Product Creation Again

1. Open Quote page: http://erp.laitor.co.ke/Sales/Quotes/Detail/{quoteId}
2. Click "+" button to open product creation modal
3. Fill form:
   - Name: "Test Product"
   - Description: "A test"
   - UnitPrice: 99.99
   - Cost: 50.00
4. Click "Create Product"

## What Changed
- Added null validation on request object with clear error message
- Added field validation (name required, price > 0)
- Better error messages if binding fails
- Proper error context in response

## If Still Getting Error
- Check browser DevTools → Network → See the CreateProduct request
- Look at Response tab to see the exact error message
- Error message will tell us if it's:
  - Binding error (will say "Request body could not be parsed")
  - Validation error (will say "name is required" or "price must be > 0")
  - Server error (will show actual exception)

The error message will help identify the root cause.
