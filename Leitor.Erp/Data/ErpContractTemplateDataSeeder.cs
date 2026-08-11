using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds the Managed Services Agreement contract template on first run only - transcribed from the
// real Laitor/Yogupay MSA this feature was built from, with the sample's own party names/dates/
// amounts replaced by [Placeholder] tokens (see ContractTemplateRenderer). Skipped once any
// ContractTemplate exists, same safe-to-rerun/editable-afterward convention as
// ErpChartOfAccountsDataSeeder - admins can edit or add to this via Contract Templates once seeded.
// Bank/payment details are deliberately left generic here rather than transcribed verbatim, since
// real account numbers don't belong in seed data/source control.
public class ErpContractTemplateDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<ContractTemplate, Guid> _templateRepository;
    private readonly IRepository<ContractTemplateSection, Guid> _sectionRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpContractTemplateDataSeeder(
        IRepository<ContractTemplate, Guid> templateRepository,
        IRepository<ContractTemplateSection, Guid> sectionRepository,
        IGuidGenerator guidGenerator)
    {
        _templateRepository = templateRepository;
        _sectionRepository = sectionRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _templateRepository.GetCountAsync() > 0)
        {
            return;
        }

        var template = new ContractTemplate(_guidGenerator.Create(), "Managed Services Agreement")
        {
            DefaultTermMonths = 12
        };
        await _templateRepository.InsertAsync(template, autoSave: true);

        var order = 0;
        foreach (var (heading, body) in Sections())
        {
            await _sectionRepository.InsertAsync(
                new ContractTemplateSection(_guidGenerator.Create(), template.Id, order++, body) { Heading = heading },
                autoSave: true);
        }
    }

    private static (string? Heading, string Body)[] Sections() => new (string?, string)[]
    {
        (null,
            "WHEREAS:\n\n" +
            "A. [CompanyName] carries on the business of IT consultancy and the provision of surveillance administration services, dealing with designing, developing, and maintaining applications and network infrastructure.\n\n" +
            "B. [ClientName] is a company requiring professional management, maintenance, and administration of its internal network and CCTV surveillance systems.\n\n" +
            "C. [ClientName] wishes to engage [CompanyName] for the maintenance and administration of its network and surveillance systems, and [CompanyName] has agreed to provide such services on the terms and conditions set out herein.\n\n" +
            "NOW IT IS HEREBY AGREED AS FOLLOWS:"),

        ("SECTION 1: SCOPE OF AGREEMENT",
            "1.1 This agreement shall become effective from [StartDate] (hereinafter referred to as the start date) and shall be for a period of [TermMonths] months.\n\n" +
            "1.2 This agreement grants rights to [ClientName] to utilize the administered systems during the period of this contract.\n\n" +
            "1.3 The agreement is aimed at ensuring that the coverage of the Network and CCTV surveillance system is functional at all times.\n\n" +
            "1.4 [CompanyName] shall provide maintenance and support in the areas specified in Section 2 at all times subject to the terms and conditions of this agreement.\n\n" +
            "1.5 The agreement shall be renewed subject to mutual written agreement between both parties."),

        ("SECTION 2: MAINTENANCE AND TECHNICAL SUPPORT",
            "2.1 Support and maintenance activities shall be carried out through remote access, telephonic assistance, or actual on-site visits as and when required.\n\n" +
            "2.2 Technical support shall be provided on the following scope and terms:\n\n" +
            "a) System Troubleshooting - Address and resolve software-related issues, including login difficulties, access errors, and system-wide functionality.\n" +
            "b) Network Connectivity Issues - Diagnose and troubleshoot problems related to network infrastructure, including error messages and system crashes.\n" +
            "c) User Assistance and Guidance - Provide user support on software features, guidance on software functionality, and completing specific tasks within the administered systems.\n" +
            "d) Bug Fixes and Error Resolution - Investigate and resolve software bugs or performance-impacting errors.\n" +
            "e) Software Installation and Updates - Assist with software installations, updates, and patches to ensure system efficiency and security.\n" +
            "f) Data Recovery and Backup Support - Automate backup routines; however, clients remain responsible for ensuring the backup task is functional and completed successfully."),

        ("SECTION 3: EXCLUSIONS AND SYSTEM CHANGES",
            "3.1 Non-Technical Support Exclusions: Technical support does not cover issues stemming from:\n" +
            "a) Lack of Training - Issues related to unclear or missing procedures or inadequate employee training on systems or processes.\n" +
            "b) Policy and Procedural Gaps - Issues related to internal client workflows.\n" +
            "c) Miscommunication - Any misunderstandings caused by communication breakdowns between departments or with clients.\n" +
            "d) Product/Service Limitations - Technical support cannot resolve issues arising from inherent limitations of the product or service.\n" +
            "e) Employee Negligence - Errors due to users ignoring guidelines or skipping necessary operational steps.\n" +
            "f) Hardware Failures - Operational issues such as equipment shortages, malfunctions, or failures are not considered technical support matters.\n\n" +
            "3.2 System Changes: Any major change in the system would have to be carried out by Change Control Methodology. Change Control comprises the drawing up of change specifications by [ClientName], impact analysis by [CompanyName], costing by [CompanyName], approval by [ClientName] of time and cost, development, testing, and deployment by [CompanyName] and finally sign-off from [ClientName]."),

        ("SECTION 4: CLIENT RESPONSIBILITIES",
            "In order to enable [CompanyName] to fulfill its obligations under this agreement, [ClientName] shall:\n\n" +
            "a) Nominate at least one (1) site contact person who will extract and supply information as [CompanyName] may require for the purpose of fault resolution.\n" +
            "b) Notify [CompanyName] promptly of faults requiring resolution.\n" +
            "c) Create a helpdesk for users within [ClientName]; such helpdesk is to be made available to [CompanyName] personnel upon request.\n" +
            "d) Maintain a \"Change Control Register\" detailing any alterations to the hardware, network, operating system, and application software.\n" +
            "e) Ensure that access to the hardware and operating system is made available to [CompanyName] personnel on completing a job and completing the register clearly indicating date and time.\n" +
            "f) Provide [CompanyName] personnel with necessary physical and remote access to the administered systems and environment.\n" +
            "g) Take regular redundant backups of system data."),

        ("SECTION 5: SERVICE PROVIDER OBLIGATIONS",
            "5.1 During the term, [CompanyName] shall perform all maintenance for the Network and CCTV systems in accordance with this agreement, operation manuals, and any supplements thereto.\n\n" +
            "5.2 [CompanyName] shall, as far as possible, conduct its maintenance operations in such a way as to cause minimum disruption to the business operations of [ClientName].\n\n" +
            "5.3 [CompanyName] shall update the Users' Manuals and configurations to include any changes made since the last update (if any) on a quarterly basis."),

        ("SECTION 6: FINANCIAL TERMS",
            "6.1 Charges: The maintenance fees are as follows:\n\n" +
            "Annual Maintenance Fee ([Purpose]): KShs [AnnualFee]\n" +
            "VAT (16%): KShs [VatAmount]\n" +
            "GRAND TOTAL: KShs [TotalAmount]\n\n" +
            "6.2 Payment Terms:\n" +
            "a) Maintenance fees shall be invoiced in one invoice that will be raised at the beginning of the period of contract.\n" +
            "b) All invoices and debit notes are payable within seven (7) days of their receipt by [ClientName].\n" +
            "c) Charges for work to be carried out under Change Control would be mutually agreed upon between the parties.\n\n" +
            "6.3 Payment Details: As advised separately by [CompanyName] on the invoice."),

        ("SECTION 7: SERVICE LEVEL AND RESPONSE TIMES",
            "[CompanyName] undertakes that when a fault is reported, the maximum time that will elapse before start of maintenance is:\n\n" +
            "Critical Faults: 24 hours (e.g. internet issues, CCTV offline, total system crash).\n" +
            "Non-Critical Faults: 48 hours (e.g. software reinstallation)."),

        ("SECTION 8: LEGAL STANDARDS",
            "8.1 Force Majeure: Neither party shall be liable for any failure to perform its obligations hereunder where such failures are caused by Acts of God, Acts of Government, strikes, fire, flood, or other causes beyond the reasonable control of the parties.\n\n" +
            "8.2 Termination:\n" +
            "a) This agreement may be terminated by either party giving one (1) month's prior notice in writing.\n" +
            "b) If the other party shall commit any breach of its obligations hereunder and shall continue in such breach.\n" +
            "c) If the other party shall commit an act of bankruptcy or go into liquidation.\n\n" +
            "8.3 Confidentiality:\n" +
            "a) The parties hereto agree to hold all information which is disclosed directly or indirectly to them under this agreement in secret and in confidence.\n" +
            "b) The parties undertake to bind any other party or provider to an agreement that seeks to ensure that such other party abides by the confidentiality arrangements agreed to above.\n" +
            "c) The obligation to confidentiality shall remain in force for the duration of the contract and for a period of five (5) years thereafter.\n\n" +
            "8.4 Intellectual Property Rights:\n" +
            "a) This contract does not implicitly or explicitly transfer any intellectual property rights of the software or systems under this contract to the Client.\n" +
            "b) All intellectual property rights are and continue to rest with the Provider."),

        ("SECTION 9: ADMINISTRATIVE PROVISIONS",
            "9.1 Assignment: Neither [CompanyName] nor [ClientName] may assign or otherwise transfer any of its rights under this agreement without prior written consent of the other party.\n\n" +
            "9.2 Notice: Any notice under this contract shall be in writing and shall be sufficiently served if delivered to the party's premises or sent by registered post. Notices are deemed served within seven (7) days from the date of postage.\n\n" +
            "9.3 Validity of These Conditions: These standard conditions shall constitute the entire contract between [CompanyName] and [ClientName] and shall not incorporate or be deemed to incorporate the provisions of any extraneous document.\n\n" +
            "9.4 Applicable Law: The agreement shall be governed by and construed in accordance with the laws of the Republic of Kenya.\n\n" +
            "9.5 Arbitration: Any dispute between the parties in respect of or arising from the agreement shall be resolved through negotiation, mediation, or arbitration. Notwithstanding the provisions of this paragraph, neither party shall be precluded from approaching a court of competent jurisdiction to obtain relief in situations where the party seeking the relief does so on an urgent basis.")
    };
}
