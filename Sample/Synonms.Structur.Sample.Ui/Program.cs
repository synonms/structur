using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Synonms.CarbonBlazor.Infrastructure.IoC;
using Synonms.Structur.Api.Client.Http;
using Synonms.Structur.Sample.ClientApi.Features.Employees;
using Synonms.Structur.Sample.Ui;
using Synonms.Structur.Sample.Ui.Infrastructure;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Aspire Service Discovery doesn't work with Blazor WASM 
const string apiUrl = "https://localhost:7002";

builder.Services.AddScoped<TenantContextAccessor>();

builder.Services.AddHttpClient<StructurHttpClient<EmployeeResource>>(httpClient => httpClient.BaseAddress = new Uri(apiUrl));

builder.Services.AddCarbonBlazor();

await builder.Build().RunAsync();