using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Documents;
using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Services.Documents;

// Mints (or reuses) a stable public link for a document, called from the already-authenticated
// staff Detail pages that build WhatsApp/email share content (Quote/Order/Invoice/PurchaseOrder/
// Proposal Detail/Edit pages). Deliberately not a full CrudAppService - there's nothing here for a
// user to browse or edit, just one idempotent "get or create" operation.
public class DocumentShareLinkService : ITransientDependency
{
    private readonly IRepository<DocumentShareLink, Guid> _repository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IConfiguration _configuration;

    public DocumentShareLinkService(
        IRepository<DocumentShareLink, Guid> repository,
        IGuidGenerator guidGenerator,
        IConfiguration configuration)
    {
        _repository = repository;
        _guidGenerator = guidGenerator;
        _configuration = configuration;
    }

    // Same (documentType, entityId) always resolves to the same link - a customer who's sent this
    // quote's link twice (e.g. re-shared after a follow-up) gets the identical URL both times,
    // rather than accumulating a fresh token (and a fresh DB row) per share.
    public async Task<string> GetOrCreateUrlAsync(string documentType, Guid entityId)
    {
        var link = (await _repository.GetListAsync(x => x.DocumentType == documentType && x.EntityId == entityId))
            .FirstOrDefault();

        if (link == null)
        {
            link = new DocumentShareLink(_guidGenerator.Create(), documentType, entityId);
            await _repository.InsertAsync(link, autoSave: true);
        }

        var baseUrl = (_configuration["App:SelfUrl"] ?? string.Empty).TrimEnd('/');
        return $"{baseUrl}/d/{link.Id}";
    }
}
