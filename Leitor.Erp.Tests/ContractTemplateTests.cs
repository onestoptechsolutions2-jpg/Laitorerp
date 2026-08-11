using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers ContractTemplateAppService (the reusable-legal-template CRUD, mirroring
// RecurringJournalTemplateAppService's header+sections shape) and ContractTemplateRenderer (the
// [Placeholder] substitution used by Documents/ContractPdfDocument.cs) - built 2026-08-11 from a
// real Managed Services Agreement sample.
public class ContractTemplateTests : ErpTestBase
{
    [Fact]
    public async Task CreateAsync_Persists_Sections_In_Order_And_Drops_Blank_Ones()
    {
        await EnsureDatabaseCreatedAsync();

        var appService = GetRequiredService<ContractTemplateAppService>();

        var created = await appService.CreateAsync(new CreateUpdateContractTemplateDto
        {
            Name = "Test Agreement",
            IsActive = true,
            DefaultTermMonths = 12,
            Sections =
            {
                new CreateUpdateContractTemplateSectionDto { Heading = null, BodyText = "Recitals text" },
                new CreateUpdateContractTemplateSectionDto { Heading = "SECTION 1", BodyText = "First clause" },
                new CreateUpdateContractTemplateSectionDto { Heading = "Blank", BodyText = "   " },
                new CreateUpdateContractTemplateSectionDto { Heading = "SECTION 2", BodyText = "Second clause" }
            }
        });

        Assert.Equal(3, created.Sections.Count);
        Assert.Null(created.Sections[0].Heading);
        Assert.Equal("Recitals text", created.Sections[0].BodyText);
        Assert.Equal("SECTION 1", created.Sections[1].Heading);
        Assert.Equal("SECTION 2", created.Sections[2].Heading);
    }

    [Fact]
    public async Task UpdateAsync_Replaces_Sections_Rather_Than_Appending()
    {
        await EnsureDatabaseCreatedAsync();

        var appService = GetRequiredService<ContractTemplateAppService>();

        var created = await appService.CreateAsync(new CreateUpdateContractTemplateDto
        {
            Name = "Test Agreement",
            Sections = { new CreateUpdateContractTemplateSectionDto { BodyText = "Original" } }
        });

        var updated = await appService.UpdateAsync(created.Id, new CreateUpdateContractTemplateDto
        {
            Name = "Test Agreement",
            Sections = { new CreateUpdateContractTemplateSectionDto { BodyText = "Replaced" } }
        });

        Assert.Single(updated.Sections);
        Assert.Equal("Replaced", updated.Sections[0].BodyText);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Every_Section_Is_Blank()
    {
        await EnsureDatabaseCreatedAsync();

        var appService = GetRequiredService<ContractTemplateAppService>();

        await Assert.ThrowsAsync<UserFriendlyException>(() => appService.CreateAsync(new CreateUpdateContractTemplateDto
        {
            Name = "Empty Template",
            Sections = { new CreateUpdateContractTemplateSectionDto { BodyText = "" } }
        }));
    }

    [Fact]
    public async Task DeleteAsync_Blocked_When_A_CustomerContract_References_The_Template()
    {
        await EnsureDatabaseCreatedAsync();

        var templateAppService = GetRequiredService<ContractTemplateAppService>();
        var customerContractRepository = GetRequiredService<IRepository<CustomerContract, Guid>>();

        var template = await templateAppService.CreateAsync(new CreateUpdateContractTemplateDto
        {
            Name = "Referenced Template",
            Sections = { new CreateUpdateContractTemplateSectionDto { BodyText = "Body" } }
        });

        var contract = new CustomerContract(Guid.NewGuid(), Guid.NewGuid(), "C-001", "Test Contract")
        {
            ContractTemplateId = template.Id
        };
        await customerContractRepository.InsertAsync(contract, autoSave: true);

        await Assert.ThrowsAsync<UserFriendlyException>(() => templateAppService.DeleteAsync(template.Id));
    }

    [Fact]
    public void Render_Substitutes_Every_Supported_Token()
    {
        var contract = new CustomerContractDto
        {
            ContractNumber = "MSA-001",
            Title = "Network & CCTV Administration",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 8, 1),
            Value = 100000m,
            ClientSignatoryName = "Jane Client"
        };
        var customer = new CustomerDto
        {
            Name = "Yogupay Technology Ltd",
            AddressLine = "P.O. Box 466-20117",
            City = "Naivasha",
            Country = "Kenya"
        };
        var company = new ErpCompanyOptions
        {
            Name = "Laitor Investment Company Ltd",
            AddressLine = "P.O. Box 68831 - 00800",
            City = "Nairobi"
        };

        var tokens = ContractTemplateRenderer.BuildTokens(contract, customer, company, "Treazer Ominde Akombe", defaultTermMonths: 12);

        var rendered = ContractTemplateRenderer.Render(
            "[ClientName] at [ClientAddress] pays [AnnualFee] plus [VatAmount] VAT ([TotalAmount] total) for [Purpose], starting [StartDate], agreement dated [AgreementDate], term [TermMonths] months, signed by [CompanySignatoryName] and [ClientSignatoryName].",
            tokens);

        Assert.DoesNotContain('[', rendered);
        Assert.Contains("Yogupay Technology Ltd", rendered);
        Assert.Contains("100,000.00", rendered);
        Assert.Contains("16,000.00", rendered); // VAT
        Assert.Contains("116,000.00", rendered); // total
        Assert.Contains("Network & CCTV Administration", rendered);
        Assert.Contains("12", rendered); // TermMonths from explicit EndDate
        Assert.Contains("Treazer Ominde Akombe", rendered);
        Assert.Contains("Jane Client", rendered);
    }

    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(22, "22nd")]
    [InlineData(23, "23rd")]
    [InlineData(31, "31st")]
    public void BuildTokens_AgreementDate_Uses_Correct_Ordinal_Suffix(int day, string expectedPrefix)
    {
        var contract = new CustomerContractDto
        {
            ContractNumber = "MSA-001",
            Title = "Test",
            StartDate = new DateTime(2026, 8, day)
        };
        var customer = new CustomerDto { Name = "Client" };
        var company = new ErpCompanyOptions { Name = "Company" };

        var tokens = ContractTemplateRenderer.BuildTokens(contract, customer, company, null, null);

        Assert.StartsWith(expectedPrefix, tokens["[AgreementDate]"]);
    }

    [Fact]
    public void BuildTokens_Falls_Back_To_Blank_Line_When_Signatory_Names_Missing()
    {
        var contract = new CustomerContractDto { ContractNumber = "MSA-001", Title = "Test", StartDate = DateTime.Today };
        var customer = new CustomerDto { Name = "Client" };
        var company = new ErpCompanyOptions { Name = "Company" };

        var tokens = ContractTemplateRenderer.BuildTokens(contract, customer, company, companySignatoryName: null, defaultTermMonths: null);

        Assert.Contains("_", tokens["[CompanySignatoryName]"]);
        Assert.Contains("_", tokens["[ClientSignatoryName]"]);
    }

    // No *PdfDocument class in this codebase has ever been exercised by a test (confirmed during
    // design), so this is new ground: generates the real seeded MSA end-to-end through QuestPDF to
    // catch any runtime composition failure (missing font/Skia native lib, overflow, etc.) here
    // rather than in production the first time someone clicks "Pdf".
    [Fact]
    public async Task ContractPdfDocument_Generates_The_Seeded_MSA_Template_Without_Throwing()
    {
        await EnsureDatabaseCreatedAsync();

        var templateAppService = GetRequiredService<ContractTemplateAppService>();
        var templates = await templateAppService.GetListAsync();
        var seeded = templates.Single(x => x.Name == "Managed Services Agreement");
        Assert.NotEmpty(seeded.Sections);

        var contract = new CustomerContractDto
        {
            ContractNumber = "MSA-001",
            Title = "Internal Network and Surveillance (CCTV) Administration Services",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 8, 1),
            Value = 150000m,
            ClientSignatoryName = "Jane Client"
        };
        var customer = new CustomerDto
        {
            Name = "Yogupay Technology Ltd",
            AddressLine = "P.O. Box 466-20117",
            City = "Naivasha",
            Country = "Kenya"
        };
        var company = new ErpCompanyOptions
        {
            Name = "Laitor Investment Company Ltd",
            AddressLine = "P.O. Box 68831 - 00800",
            City = "Nairobi"
        };

        var pdfBytes = ContractPdfDocument.Generate(contract, seeded, customer, company, "Treazer Ominde Akombe");

        Assert.NotEmpty(pdfBytes);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }
}
