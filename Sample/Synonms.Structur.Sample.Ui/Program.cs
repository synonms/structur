using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Synonms.CarbonBlazor.Infrastructure.IoC;
using Synonms.Structur.Sample.ClientApi;
using Synonms.Structur.Sample.Ui;
using Synonms.Structur.Sample.Ui.Infrastructure;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Aspire Service Discovery doesn't work with Blazor WASM 
const string apiUrl = "https://localhost:7002";

builder.Services.AddScoped<TenantContextAccessor>();
builder.Services.AddTransient<IStructurSampleOpenApiClient>(sp =>
{
    TenantContextAccessor tenantContextAccessor = sp.GetRequiredService<TenantContextAccessor>();
    
    HttpClient httpClient = new() 
    {
        BaseAddress = new Uri(apiUrl)
    };
 
    httpClient.DefaultRequestHeaders.Add("X-Structur-Tenant-ID", tenantContextAccessor.SelectedTenantId.ToString());
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

    return new StructurSampleOpenApiClient(httpClient);
});

builder.Services.AddCarbonBlazor();

await builder.Build().RunAsync();