
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Il2CppDumper;
using System.Text;
using Web;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

await builder.Build().RunAsync();
