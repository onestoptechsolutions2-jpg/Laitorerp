using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Projects;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Projects;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Projects;

[Authorize(Policy = ErpPermissions.Projects.Default)]
public class DetailModel : AbpPageModel
{
    private readonly ProjectAppService _projectAppService;
    private readonly ProjectTaskAppService _projectTaskAppService;
    private readonly ProjectReportAppService _projectReportAppService;
    private readonly OrderAppService _orderAppService;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IRepository<Partner, Guid> _partnerRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        ProjectAppService projectAppService,
        ProjectTaskAppService projectTaskAppService,
        ProjectReportAppService projectReportAppService,
        OrderAppService orderAppService,
        IRepository<Order, Guid> orderRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IRepository<Agent, Guid> agentRepository,
        IRepository<Partner, Guid> partnerRepository,
        IFeatureChecker featureChecker)
    {
        _projectAppService = projectAppService;
        _projectTaskAppService = projectTaskAppService;
        _projectReportAppService = projectReportAppService;
        _orderAppService = orderAppService;
        _orderRepository = orderRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _agentRepository = agentRepository;
        _partnerRepository = partnerRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ProjectDto Project { get; set; } = null!;
    public IReadOnlyList<ProjectTaskDto> Tasks { get; set; } = Array.Empty<ProjectTaskDto>();
    public IReadOnlyList<OrderDto> Orders { get; set; } = Array.Empty<OrderDto>();
    public ProjectPnLDto PnL { get; set; } = new();

    [BindProperty]
    public CreateUpdateProjectTaskDto NewTask { get; set; } = new();

    public List<SelectListItem> AgentOptions { get; set; } = new();
    public List<SelectListItem> PartnerOptions { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool CanConvertToContract { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Projects.Edit);
        CanConvertToContract = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "Project", Id);
        await LoadAsync();
        await LoadTaskAssigneeOptionsAsync();
        return Page();
    }

    // Queried directly via repository rather than through AgentAppService/PartnerAppService - both
    // are [RequiresFeature(ErpFeatures.PartnerCommission)], a different optional module from
    // Project Management. If that module is off, these lists are just empty (dropdown shows only
    // "None") rather than throwing - graceful degradation, same as how other cross-module
    // dropdowns in this app are populated (e.g. Customer Create's PriceList options).
    private async Task LoadTaskAssigneeOptionsAsync()
    {
        var agents = await _agentRepository.GetListAsync();
        AgentOptions = new List<SelectListItem> { new(L["None"], "") };
        AgentOptions.AddRange(agents.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));

        var partners = await _partnerRepository.GetListAsync();
        PartnerOptions = new List<SelectListItem> { new(L["None"], "") };
        PartnerOptions.AddRange(partners.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));
    }

    private async Task LoadAsync()
    {
        Project = await _projectAppService.GetAsync(Id);

        var tasks = await _projectTaskAppService.GetListAsync(new GetProjectTaskListInput
        {
            ProjectId = Id,
            MaxResultCount = 1000
        });
        Tasks = tasks.Items;

        var orders = await _orderRepository.GetListAsync(x => x.ProjectId == Id);
        var orderDtos = new List<OrderDto>();
        foreach (var order in orders.OrderByDescending(x => x.OrderDate))
        {
            orderDtos.Add(await _orderAppService.GetAsync(order.Id));
        }
        Orders = orderDtos;

        PnL = await _projectReportAppService.GetProjectPnLAsync(Id);
    }

    public async Task<IActionResult> OnPostAddTaskAsync()
    {
        NewTask.ProjectId = Id;
        if (!string.IsNullOrWhiteSpace(NewTask.Title))
        {
            await _projectTaskAppService.CreateAsync(NewTask);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostToggleTaskAsync(Guid taskId)
    {
        var task = await _projectTaskAppService.GetAsync(taskId);
        await _projectTaskAppService.UpdateAsync(taskId, new CreateUpdateProjectTaskDto
        {
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            AssignedToUserId = task.AssignedToUserId,
            AgentId = task.AgentId,
            PartnerId = task.PartnerId,
            IsCompleted = !task.IsCompleted
        });

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteTaskAsync(Guid taskId)
    {
        await _projectTaskAppService.DeleteAsync(taskId);
        return RedirectToPage(new { id = Id });
    }
}
