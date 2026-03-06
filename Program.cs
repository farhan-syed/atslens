using AtsResumeOptimizer.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "5099";

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IResumeAnalyzer, ResumeAnalyzer>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
