using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Catalog;

[Authorize(Policy = ErpPermissions.Catalog.Default)]
public class DetailModel : AbpPageModel
{
    private readonly ProductAppService _productAppService;
    private readonly ProductVendorAppService _productVendorAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;

    public DetailModel(
        ProductAppService productAppService,
        ProductVendorAppService productVendorAppService,
        IRepository<Vendor, Guid> vendorRepository)
    {
        _productAppService = productAppService;
        _productVendorAppService = productVendorAppService;
        _vendorRepository = vendorRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ProductDto Product { get; set; } = null!;
    public IReadOnlyList<ProductVendorDto> Suppliers { get; set; } = Array.Empty<ProductVendorDto>();
    public List<SelectListItem> VendorOptions { get; set; } = new();

    [BindProperty]
    public CreateUpdateProductVendorDto NewSupplier { get; set; } = new();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Product = await _productAppService.GetAsync(Id);

        var suppliers = await _productVendorAppService.GetListAsync(new GetProductVendorListInput
        {
            ProductId = Id,
            MaxResultCount = 1000
        });
        Suppliers = suppliers.Items;

        var vendors = await _vendorRepository.GetListAsync();
        VendorOptions = vendors.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    }

    public async Task<IActionResult> OnPostAddSupplierAsync()
    {
        NewSupplier.ProductId = Id;
        await _productVendorAppService.CreateAsync(NewSupplier);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteSupplierAsync(Guid supplierId)
    {
        await _productVendorAppService.DeleteAsync(supplierId);
        return RedirectToPage(new { id = Id });
    }
}
