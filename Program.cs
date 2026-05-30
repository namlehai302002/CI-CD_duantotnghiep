using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using WMS.Data;
using WMS.Authorization;
using WMS.Models;
using WMS.Services;
using WMS.Common;
// P3.2: OpenTelemetry
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = Directory.GetCurrentDirectory(),
    Args = args
});
var secureCookiePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

// Add services
builder.Services.AddControllersWithViews(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));

    // CSRF protection for all unsafe HTTP methods (POST/PUT/PATCH/DELETE)
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddXmlSerializerFormatters();

// Allow JS/AJAX to send antiforgery token via header `RequestVerificationToken`
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.SecurePolicy = secureCookiePolicy;
});

// ═══ DATABASE: SQL Server with Retry Policy + Timeout ═══
// EnableRetryOnFailure: auto-retry on transient failures (deadlock, connection timeout)
// MaxRetryCount=3, MaxDelay=30s with exponential backoff
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(30);
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = secureCookiePolicy;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<MinerUOptions>(builder.Configuration.GetSection("MinerU"));

// Enterprise RBAC: Role -> Permission -> Policy
builder.Services.AddAuthorization(options =>
{
    void AddPerm(string code)
        => options.AddPolicy(code, p => p.Requirements.Add(new PermissionRequirement(code)));

    foreach (var code in WmsPermissions.All)
        AddPerm(code);
});
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

// HttpClient factory: proper connection pooling (prevents socket exhaustion vs new HttpClient())
builder.Services.AddHttpClient("AiOcr", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient("MinerU", client =>
{
    var timeoutSeconds = builder.Configuration.GetValue<int?>("MinerU:TimeoutSeconds") ?? 180;
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : 180);
});
// P1.2: Integration reliability HttpClient
builder.Services.AddHttpClient("Integration", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "WMS-Integration/1.0");
});
builder.Services.AddScoped<IIntegrationService, IntegrationService>();
builder.Services.AddScoped<IInventoryBalanceService, InventoryBalanceService>();
builder.Services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();
builder.Services.AddScoped<InventorySnapshotService>();
builder.Services.AddScoped<IInventorySnapshotService>(sp => sp.GetRequiredService<InventorySnapshotService>());
builder.Services.AddScoped<IInventoryReconciliationService>(sp => sp.GetRequiredService<InventorySnapshotService>());
builder.Services.AddScoped<IInventoryReservationService, InventoryReservationService>();
builder.Services.AddScoped<ISerialInventoryService, SerialInventoryService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<ICrossDockService, CrossDockService>();
builder.Services.AddScoped<IOutboundExecutionService, OutboundExecutionService>();
builder.Services.AddScoped<IInboundExecutionService, InboundExecutionService>();
builder.Services.AddScoped<IVoucherCancellationService, VoucherCancellationService>();
builder.Services.AddScoped<IYardManagementService, YardManagementService>();
builder.Services.AddScoped<IYardBillingService, YardBillingService>();
builder.Services.AddScoped<IMovementTaskService, MovementTaskService>();
builder.Services.AddScoped<IKittingWorkOrderService, KittingWorkOrderService>();
builder.Services.AddScoped<ILabelPrintService, LabelPrintService>();
builder.Services.AddScoped<IShippingDocumentService, ShippingDocumentService>();
builder.Services.AddScoped<IShippingReconciliationService, ShippingReconciliationService>();
builder.Services.AddScoped<IVasWorkOrderService, VasWorkOrderService>();
builder.Services.AddScoped<IOrderStreamingService, OrderStreamingService>();
builder.Services.AddScoped<ILpnHierarchyService, LpnHierarchyService>();
builder.Services.AddScoped<IReplenishmentAutomationService, ReplenishmentAutomationService>();
builder.Services.AddScoped<IDirectedPutawayService, DirectedPutawayService>();
builder.Services.AddScoped<IAdvancedAllocationService, AdvancedAllocationService>();
builder.Services.AddScoped<ICycleCountPlanningService, CycleCountPlanningService>();
builder.Services.AddScoped<IReturnRmaService, ReturnRmaService>();
builder.Services.AddScoped<ICartonizationService, CartonizationService>();
builder.Services.AddScoped<IRfWorkflowBuilderService, RfWorkflowBuilderService>();
builder.Services.AddScoped<IOfflineQueuePolicyService, OfflineQueuePolicyService>();
builder.Services.AddScoped<IMobileDeviceManagementService, MobileDeviceManagementService>();
builder.Services.AddScoped<IVoicePickingAdapter, VoicePickingSimulatorAdapter>();
builder.Services.AddScoped<IAdvancedBarcodeParser, AdvancedBarcodeParser>();
builder.Services.AddScoped<IExternalIdentityMappingService, ExternalIdentityMappingService>();
builder.Services.AddScoped<IProductionMfaLockoutService, ProductionMfaLockoutService>();
builder.Services.AddScoped<ISegregationOfDutiesService, SegregationOfDutiesService>();
builder.Services.AddScoped<ISecurityScopeAuditService, SecurityScopeAuditService>();
builder.Services.AddScoped<ISecurityEventCenterService, SecurityEventCenterService>();
builder.Services.AddScoped<ISecretReadinessService, SecretReadinessService>();
builder.Services.AddScoped<ICatchWeightService, CatchWeightService>();
builder.Services.AddScoped<IShipmentLoadService, ShipmentLoadService>();
builder.Services.AddScoped<ITenantScopeService, TenantScopeService>();
builder.Services.AddScoped<IThreePlBillingService, ThreePlBillingService>();
builder.Services.AddScoped<IDockAppointmentService, DockAppointmentService>();
builder.Services.AddScoped<IThreePlEnterpriseBillingService, ThreePlEnterpriseBillingService>();
builder.Services.AddScoped<ILaborManagementService, LaborManagementService>();
builder.Services.AddScoped<IOptimizationEnterpriseService, OptimizationEnterpriseService>();
builder.Services.AddScoped<IAutomationEnterpriseService, AutomationEnterpriseService>();
builder.Services.AddScoped<IEnterpriseIntegrationService, EnterpriseIntegrationService>();
builder.Services.AddScoped<IEnterpriseAnalyticsService, EnterpriseAnalyticsService>();
builder.Services.AddScoped<IRoleWorkspaceService, RoleWorkspaceService>();
builder.Services.AddScoped<IProductionSreService, ProductionSreService>();
builder.Services.AddScoped<IMheIntegrationService, MheIntegrationService>();
builder.Services.AddScoped<ICarrierIntegrationService, CarrierIntegrationService>();
builder.Services.AddScoped<IOperationsScopeQueryService, OperationsScopeQueryService>();
builder.Services.AddScoped<ISlottingPlanningService, SlottingPlanningService>();
builder.Services.AddScoped<IOperationExceptionQueryService, OperationExceptionQueryService>();
builder.Services.AddScoped<IYardBillingQueryService, YardBillingQueryService>();
builder.Services.AddScoped<IVoucherCreateWorkflowService, VoucherCreateWorkflowService>();
builder.Services.AddScoped<IVoucherDetailQueryService, VoucherDetailQueryService>();
builder.Services.AddScoped<IVoucherImportQueryService, VoucherImportQueryService>();
builder.Services.AddScoped<IVoucherSharedRuleService, VoucherSharedRuleService>();
builder.Services.AddScoped<IMineruDocumentParserClient, MineruDocumentParserClient>();
builder.Services.AddScoped<IVoucherDocumentIntakeService, VoucherDocumentIntakeService>();
builder.Services.AddScoped<TaskInterleavingService>();
builder.Services.AddScoped<LocalVerificationSeeder>();

// In-memory cache: reduces DB load for frequently accessed master data (Items, UOMs, Categories)
builder.Services.AddMemoryCache();

// ═══ OPEN TELEMETRY (P3.2) ═══════════════════════════════════════════════════════
// Distributed tracing: trace every voucher lifecycle operation
// Metrics: dock-to-stock time, pick SLA, ship SLA
var otelServiceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "WMS-Enterprise";
var otelEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

// Trace provider: capture spans for every HTTP request and DB operation
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: otelServiceName, serviceVersion: "2.0.0")
        .AddAttributes(new[]
        {
            new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
            new KeyValuePair<string, object>("host.name", Environment.MachineName)
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = builder.Environment.IsDevelopment();
            });

        // Only add OTLP exporter if endpoint is a valid absolute URI (not a placeholder like ${OtlpEndpoint})
        if (!string.IsNullOrWhiteSpace(otelEndpoint)
            && (otelEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || otelEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            && Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var tracingUri))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = tracingUri;
            });
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation();

        // Only add OTLP exporter if endpoint is a valid absolute URI (not a placeholder like ${OtlpEndpoint})
        if (!string.IsNullOrWhiteSpace(otelEndpoint)
            && (otelEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || otelEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            && Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var metricsUri))
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = metricsUri;
            });
        }
    });

// Custom WMS metrics (complement EF core auto-collection)
builder.Services.AddSingleton<WmsMetrics>();

// ═══ RESPONSE COMPRESSION: Reduce bandwidth 60-80% for HTML/JSON/CSS ═══
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json", "text/css", "application/javascript", "text/html"
    });
});
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);

// ═══ RATE LIMITING: Protect login/API from brute-force & DDoS ═══
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: max 10 attempts per minute per IP
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // API: max 60 requests per minute per user
    options.AddPolicy("api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2
            }));
});

// ═══ HEALTH CHECK: /health endpoint for monitoring ═══
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// P1.2: Background outbox processor (đọc pending outbox, gửi HTTP, retry/DLQ)
builder.Services.AddHostedService<OutboxProcessorHostedService>();
builder.Services.AddHostedService<InventorySnapshotOutboxHostedService>();
builder.Services.AddHostedService<InventoryReconciliationHostedService>();
builder.Services.AddHostedService<ReplenishmentAutomationHostedService>();

// ═══ DATA PROTECTION: Persist keys to disk ═══
var keysDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(keysDir);
builder.Services.AddDataProtection()
    .SetApplicationName("WMS-Pro")
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir));

// Configure forwarded headers for Plesk/Nginx reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.RequireHeaderSymmetry = true;
    options.ForwardLimit = 1;

    var knownProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
    foreach (var proxy in knownProxies)
    {
        if (IPAddress.TryParse(proxy, out var ip))
            options.KnownProxies.Add(ip);
    }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment()
    && (app.Configuration.GetValue<bool>("LocalVerification:Enabled")
        || app.Configuration.GetValue<bool>("LocalVerification:BypassMfaForLoopback")))
{
    app.Logger.LogWarning("LOCAL_VERIFICATION_DISABLED_OUTSIDE_DEVELOPMENT: Local verification flags are ignored in {EnvironmentName}.", app.Environment.EnvironmentName);
}

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("LocalVerification:Enabled"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<LocalVerificationSeeder>().SeedAsync();
}

var mineruBaseUrl = app.Configuration["MinerU:BaseUrl"] ?? string.Empty;
var mineruUsesLoopback = Uri.TryCreate(mineruBaseUrl, UriKind.Absolute, out var mineruUri)
    && mineruUri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
    && IPAddress.TryParse(mineruUri.Host, out var mineruHostAddress)
    && IPAddress.IsLoopback(mineruHostAddress);
if (app.Configuration.GetValue<bool>("MinerU:Enabled")
    && !app.Environment.IsDevelopment()
    && mineruUsesLoopback)
{
    app.Logger.LogWarning("MINERU_LOOPBACK_PRODUCTION_WARNING: MinerU.Enabled=true nhưng MinerU:BaseUrl đang trỏ về máy cục bộ. Trên hosting, hãy cấu hình URL MinerU nội bộ thật để chức năng đọc chứng từ hoạt động ổn định.");
}

// ForwardedHeaders MUST be the FIRST middleware
app.UseForwardedHeaders();
app.UseWmsCorrelationTelemetry();

// ═══ GLOBAL EXCEPTION HANDLER (RFC 7807 ProblemDetails) ═══
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";
        var exceptionHandler = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = exceptionHandler?.Error ?? new Exception("Unknown error");

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Response.StatusCode = exception switch
        {
            BusinessRuleException => StatusCodes.Status400BadRequest,
            ConcurrencyException => StatusCodes.Status409Conflict,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            SodViolationException => StatusCodes.Status403Forbidden,
            WarehouseLockedException => StatusCodes.Status423Locked,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.Headers["X-Request-Id"] = traceId;

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = "Error",
            status = context.Response.StatusCode,
            detail = UserSafeError.From(exception, "An error occurred."),
            traceId = traceId
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = app.Environment.IsDevelopment()
        });

        await context.Response.WriteAsync(json);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

static bool IsCameraEnabledPath(PathString path)
{
    string value = path.Value ?? string.Empty;
    string[] cameraEnabledPrefixes =
    [
        "/Operations/RfReceiving",
        "/Operations/SerialReceiving",
        "/Operations/RfPicking",
        "/Operations/RfMovement",
        "/Operations/NextTask",
        "/Operations/LpnLookup",
        "/Operations/SerialLookup",
        "/Operations/ShipmentLoadDetails",
        "/Vouchers/Create"
    ];

    return cameraEnabledPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}

// ═══ SECURITY HEADERS: Prevent clickjacking, MIME sniffing, XSS ═══
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    string cameraPolicy = IsCameraEnabledPath(context.Request.Path) ? "camera=(self)" : "camera=()";
    context.Response.Headers["Permissions-Policy"] = $"{cameraPolicy}, microphone=(), geolocation=()";
    await next();
});

// Response compression BEFORE static files for maximum savings
app.UseResponseCompression();

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Health check endpoint for monitoring & load balancers
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
