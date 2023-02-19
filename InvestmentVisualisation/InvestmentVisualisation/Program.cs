using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataBaseRepository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IMySqlCommonRepository, CommonRepository>();
builder.Services.AddTransient<IMySqlIncomingRepository, MySqlIncomingRepository>();
builder.Services.AddTransient<IMySqlDealsRepository, MySqlDealsRepository>();

builder.Services.Configure<DataBaseConnectionSettings>(builder.Configuration.GetSection("DataBaseConnectionSettings"));
builder.Services.Configure<PaginationSettings>(builder.Configuration.GetSection("Pagination"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Incoming/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Incoming}/{action=Incoming}/{id?}");

app.Run();
