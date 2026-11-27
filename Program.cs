using System.Net.Http.Headers;
using FabricaWebRender.Services;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços ao container (MVC)
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<SupabaseEstoqueService>();

var supabaseUrl = builder.Configuration["Supabase:Url"];
if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var supabaseBaseUri) ||
    supabaseBaseUri.Scheme != Uri.UriSchemeHttps)
{
    throw new InvalidOperationException("Supabase:Url must be an absolute HTTPS URL.");
}

var supabasePublishableKey = builder.Configuration["Supabase:PublishableKey"];
if (string.IsNullOrWhiteSpace(supabasePublishableKey))
{
    throw new InvalidOperationException("Supabase:PublishableKey is not configured.");
}

builder.Services.AddHttpClient("Supabase", client =>
{
    client.BaseAddress = new Uri(supabaseBaseUri, "rest/v1/");
    client.DefaultRequestHeaders.Add("apikey", supabasePublishableKey);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabasePublishableKey);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

// Configurar o pipeline de requisições HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Substituído para compatibilidade total com .NET 8 no Render
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();