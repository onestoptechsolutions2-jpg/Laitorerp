using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi;
using Leitor.Erp.BackgroundWorkers;
using Leitor.Erp.Data;
using Leitor.Erp.Documents;
using Leitor.Erp.Localization;
using Leitor.Erp.Menus;
using Leitor.Erp.Pages.Shared.Components.FormOverlay;
using Leitor.Erp.Pages.Shared.Components.GlobalSearch;
using Leitor.Erp.Pages.Shared.Components.MyActionItems;
using Leitor.Erp.Pages.Shared.Components.ThemeFonts;
using Leitor.Erp.Services.Governance;
using QuestPDF.Infrastructure;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Uow;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Mapperly;
using Volo.Abp.Emailing;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.FeatureManagement;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.Identity.Web;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.PermissionManagement.OpenIddict;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.Swashbuckle;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.Web;
using Volo.Abp.OpenIddict;
using Volo.Abp.Security.Claims;
using Volo.Abp.AspNetCore.Mvc.UI.Theming;
using Volo.Abp.Ui.LayoutHooks;
using Volo.Abp.UI.Navigation;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace Leitor.Erp;

[DependsOn(
    // ABP Framework packages
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpMapperlyModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),

    // Account module packages
    typeof(AbpAccountApplicationModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpAccountWebOpenIddictModule),

    // Identity module packages
    typeof(AbpPermissionManagementDomainIdentityModule),
    typeof(AbpPermissionManagementDomainOpenIddictModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpIdentityWebModule),

    // Audit logging module packages
    typeof(AbpAuditLoggingEntityFrameworkCoreModule),

    // Permission Management module packages
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),

    // Tenant Management module packages
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(AbpTenantManagementWebModule),

    // Feature Management module packages
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpFeatureManagementEntityFrameworkCoreModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpFeatureManagementWebModule),

    // Setting Management module packages
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpSettingManagementWebModule)
)]
public class ErpModule : AbpModule
{
    /* Single point to enable/disable multi-tenancy */
    public const bool IsMultiTenant = false;

    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(
                typeof(ErpResource)
            );
        });

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("Erp");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        if (!hostingEnvironment.IsDevelopment())
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = false;
            });

            PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
            {
                // The certificate file itself is written by entrypoint.sh from the
                // OPENIDDICT_CERT_BASE64 secret at container startup - it's never committed to
                // git. The pass phrase is likewise supplied via configuration/environment
                // (OpenIddict__CertificatePassPhrase), not hardcoded, since this repo is public.
                var certificatePassPhrase = configuration["OpenIddict:CertificatePassPhrase"];
                if (string.IsNullOrEmpty(certificatePassPhrase))
                {
                    throw new AbpException(
                        "OpenIddict:CertificatePassPhrase configuration is required outside the Development environment. " +
                        "Set the OpenIddict__CertificatePassPhrase environment variable.");
                }

                serverBuilder.AddProductionEncryptionAndSigningCertificate("openiddict.pfx", certificatePassPhrase);
            });
        }
        
        ErpGlobalFeatureConfigurator.Configure();
        ErpModuleExtensionConfigurator.Configure();
        ErpEfCoreEntityExtensionMappings.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        if (hostingEnvironment.IsDevelopment())
        {
            context.Services.Replace(ServiceDescriptor.Singleton<IEmailSender, NullEmailSender>());
        }

        QuestPDF.Settings.License = LicenseType.Community;
        context.Services.Configure<ErpCompanyOptions>(configuration.GetSection("Company"));
        context.Services.Configure<OpenExchangeRatesOptions>(configuration.GetSection("OpenExchangeRates"));
        context.Services.Configure<DataRetentionOptions>(configuration.GetSection("DataRetention"));
        context.Services.AddHttpClient("OpenExchangeRates");

        // Explicit registration rather than relying on ITransientDependency convention -
        // IEnumerable<IEscalationActionHandler> resolution needs every implementation registered
        // under this exact interface, and empirically the conventional registrar didn't expose it
        // (confirmed via EscalationItemTests failing with "No handler registered" until this was
        // added). One line per new escalation action type, alongside a new handler class.
        context.Services.AddTransient<IEscalationActionHandler, QuoteMarginOverrideEscalationHandler>();
        context.Services.AddTransient<IEscalationActionHandler, OrderMarginOverrideEscalationHandler>();

        ConfigureAuthentication(context);
        ConfigureIdentityOptions();
        ConfigureMultiTenancy();
        ConfigureUrls(configuration);
        ConfigureBundles();
        ConfigureLayoutHooks();
        ConfigureMapperly(context);
        ConfigureSwagger(context.Services);
        ConfigureNavigationServices();
        ConfigureAutoApiControllers();
        ConfigureVirtualFiles(hostingEnvironment);
        ConfigureLocalization();
        ConfigureEfCore(context);
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    // Reasonable named defaults for a business-data ERP, not tuned to any specific compliance
    // regime - MFA enrollment is a separate, much larger effort left out of this pass.
    private void ConfigureIdentityOptions()
    {
        Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        });
    }

    private void ConfigureMultiTenancy()
    {
        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = IsMultiTenant;
        });
    }


    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css", "/leitor-tokens.css", "/leitor-theme.css");
                }
            );

            options.ScriptBundles.Configure(
                LeptonXLiteThemeBundles.Scripts.Global,
                bundle =>
                {
                    bundle.AddFiles("/leitor-layout.js");
                }
            );
        });
    }

    // Renders MyActionItemsViewComponent (the right-hand "action items" rail) at the end of
    // every page's <body> - LeptonXLite's own layout is a precompiled Razor Class Library with
    // no source to safely override, so LayoutHooks is the supported extension point for adding
    // new UI without touching it. StandardLayouts.Application excludes the Account (login) layout,
    // so no anonymous-user handling is needed here (MyActionItemsViewComponent still checks
    // authentication defensively - see its own comment).
    private void ConfigureLayoutHooks()
    {
        Configure<AbpLayoutHookOptions>(options =>
        {
            options.Add(
                LayoutHooks.Body.Last,
                typeof(MyActionItemsViewComponent),
                layout: StandardLayouts.Application);

            // The overlay-form modal shell (see Pages/Shared/Components/FormOverlay and
            // wwwroot/leitor-layout.js) - one instance per page, fetched into on demand.
            options.Add(
                LayoutHooks.Body.Last,
                typeof(FormOverlayViewComponent),
                layout: StandardLayouts.Application);

            // Google Fonts <link> tags for Inter (see ThemeFontsViewComponent's own comment for
            // why this is a real <head> link rather than a CSS @import). Account (login) layout
            // is excluded same as the rest of this method - it loads the same fonts directly via
            // a <link> in Login.cshtml's own styles section instead.
            options.Add(
                LayoutHooks.Head.Last,
                typeof(ThemeFontsViewComponent),
                layout: StandardLayouts.Application);

            // Floating global search trigger + panel (see GlobalSearchViewComponent's own
            // comment) - added 2026-08-17 after a usability audit flagged "no way to find a
            // record without already knowing which module owns it" as the highest-friction gap
            // in the app.
            options.Add(
                LayoutHooks.Body.Last,
                typeof(GlobalSearchViewComponent),
                layout: StandardLayouts.Application);
        });
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<ErpResource>("en")
                .AddBaseTypes(typeof(AbpValidationResource))
                .AddVirtualJson("/Localization/Erp");

            options.DefaultResourceType = typeof(ErpResource);

            options.Languages.Add(new LanguageInfo("en", "en", "English"));
            options.Languages.Add(new LanguageInfo("tr", "tr", "Türkçe"));
            options.Languages.Add(new LanguageInfo("ar", "ar", "العربية"));
            options.Languages.Add(new LanguageInfo("cs", "cs", "Čeština"));
            options.Languages.Add(new LanguageInfo("en-GB", "en-GB", "English (UK)"));
            options.Languages.Add(new LanguageInfo("hu", "hu", "Magyar"));
            options.Languages.Add(new LanguageInfo("fi", "fi", "Finnish"));
            options.Languages.Add(new LanguageInfo("fr", "fr", "Français"));
            options.Languages.Add(new LanguageInfo("hi", "hi", "Hindi"));
            options.Languages.Add(new LanguageInfo("is", "is", "Icelandic"));
            options.Languages.Add(new LanguageInfo("it", "it", "Italiano"));
            options.Languages.Add(new LanguageInfo("pt-BR", "pt-BR", "Português"));
            options.Languages.Add(new LanguageInfo("ro-RO", "ro-RO", "Română"));
            options.Languages.Add(new LanguageInfo("ru", "ru", "Русский"));
            options.Languages.Add(new LanguageInfo("sk", "sk", "Slovak"));
            options.Languages.Add(new LanguageInfo("zh-Hans", "zh-Hans", "简体中文"));
            options.Languages.Add(new LanguageInfo("zh-Hant", "zh-Hant", "繁體中文"));
            options.Languages.Add(new LanguageInfo("de-DE", "de-DE", "Deutsch"));
            options.Languages.Add(new LanguageInfo("es", "es", "Español"));
            options.Languages.Add(new LanguageInfo("el", "el", "Ελληνικά"));
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace("Erp", typeof(ErpResource));
        });
    }

    private void ConfigureVirtualFiles(IWebHostEnvironment hostingEnvironment)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<ErpModule>();
            if (hostingEnvironment.IsDevelopment())
            {
                /* Using physical files in development, so we don't need to recompile on changes */
                options.FileSets.ReplaceEmbeddedByPhysical<ErpModule>(hostingEnvironment.ContentRootPath);
            }
        });
    }

    private void ConfigureNavigationServices()
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new ErpMenuContributor());
        });
    }

    private void ConfigureAutoApiControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(ErpModule).Assembly);
        });
    }

    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Leitor ERP API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            }
        );
    }

    private void ConfigureMapperly(ServiceConfigurationContext context)
    {
        context.Services.AddMapperlyObjectMapper<ErpModule>();
    }

    private void ConfigureEfCore(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<ErpDbContext>(options =>
        {
            /* You can remove "includeAllEntities: true" to create
             * default repositories only for aggregate roots
             * Documentation: https://docs.abp.io/en/abp/latest/Entity-Framework-Core#add-default-repositories
             */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(configurationContext =>
            {
                configurationContext.UseNpgsql();
            });
        });

    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        // Coolify/Traefik terminates TLS and proxies to this container over plain HTTP on its
        // internal docker network - without this, ASP.NET Core (and ABP's antiforgery cookie)
        // thinks every request is HTTP, which makes it emit "SameSite=None" without "Secure" on
        // the XSRF-TOKEN cookie and browsers silently drop it. KnownNetworks/KnownProxies are
        // cleared because the proxy's container IP isn't fixed/known in advance; safe here since
        // only the reverse proxy - not this container - is exposed to the internet.
        if (!env.IsDevelopment())
        {
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);
        }

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.MapAbpStaticAssets();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        // IsMultiTenant is a compile-time const (false) - CS0162 flags this branch as
        // unreachable, but that's the point: flipping the const is the documented single point
        // to re-enable multi-tenancy (see its own declaration), which brings this branch back to
        // life. Suppressed rather than removed so the toggle keeps working.
#pragma warning disable CS0162
        if (IsMultiTenant)
        {
            app.UseMultiTenancy();
        }
#pragma warning restore CS0162

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Leitor ERP API");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await base.OnApplicationInitializationAsync(context);

        await context.AddBackgroundWorkerAsync<ContractExpiryAlertWorker>();
        await context.AddBackgroundWorkerAsync<ExchangeRateSyncWorker>();
        await context.AddBackgroundWorkerAsync<DataRetentionPurgeWorker>();
        await context.AddBackgroundWorkerAsync<RecurringJournalWorker>();
    }
}
