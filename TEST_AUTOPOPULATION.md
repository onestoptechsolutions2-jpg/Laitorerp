# Testing Auto-Population and Product Creation

## Deployment Steps (Run Locally First)

1. **Build the solution**
   ```powershell
   cd C:\Users\Billions\source\repos\Laitorerp\Leitor.Erp
   dotnet build -c Release
   ```

2. **Verify no build errors** - should see "Build succeeded"

3. **Deploy via Coolify**
   - Open Coolify dashboard
   - Redeploy the application
   - Wait for deployment to complete
   - Check logs for "Successfully completed all database migrations"

## Test Auto-Population

### Setup
1. Open Quote Detail page (e.g., http://erp.laitor.co.ke/Sales/Quotes/Detail/{quoteId})
2. Open **Browser DevTools** → **Console** tab (F12)
3. You should see console logs appear as you interact

### Test Steps
1. **Select a product** from the "Product" dropdown
   - Look in Console for: `Product selected: {productId}`
   - Look for: `Fetching from: ?handler=ProductDetails&productId=...`
   - Look for: `Response status: 200`
   - Look for: `Product details received: {details}`

2. **Verify fields populate**
   - Description field should auto-fill
   - UnitPrice field should auto-fill with price or price-list price
   - Cost field should auto-fill
   - Tax Rate should auto-select if available

### If it doesn't work
Check console for:
- `"Product select element not found"` → HTML selector issue
- `"Response status: 404"` → Handler name wrong (but should be fixed now)
- `"Error fetching product details"` → Server error
- No logs at all → JavaScript not running

## Test Product Creation

1. Click **"+"** button next to product dropdown
2. **Modal dialog opens** with form fields
3. Fill in:
   - Name: "Test Product"
   - Description: "A test product"
   - UnitPrice: 99.99
   - Cost: 50.00
   - TaxRate: (leave as default or select one)

4. Click **"Create Product"** button
5. Should see success alert: "Product created successfully"
6. Modal closes automatically
7. New product appears in dropdown (selected automatically)
8. Fields auto-populate with new product data

### If creation fails
Check console for error messages showing:
- Antiforgery token validation errors
- Server validation errors
- Network errors

## Key Handler Names (Razor Pages)
- `OnGetProductDetailsAsync` → URL: `?handler=ProductDetails` ✅ (FIXED)
- `OnPostCreateProductAsync` → URL: `?handler=CreateProduct` ✅
- Product dropdown selector: `select[asp-for="NewLine.ProductId"]` ✅

## What Was Fixed
1. ✅ Product creation modal now includes antiforgery token
2. ✅ JavaScript safely looks up token from DOM
3. ✅ Auto-population now uses correct handler name (ProductDetails, not GetProductDetails)
4. ✅ Comprehensive browser console logging for debugging
5. ✅ Better error handling and fallbacks
