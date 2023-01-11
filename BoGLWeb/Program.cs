using BoGLWeb;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.StaticFiles;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient {
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddAntDesign();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings.Add(".grxml", "appliction/grxml");
provider.Mappings.Add(".rsxml", "appliction/rsxml");
provider.Mappings.Add(".bogl", "appliction/bogl");

builder.Services.Configure<StaticFileOptions>(options => {
    options.ContentTypeProvider = provider;
});

await builder.Build().RunAsync();
