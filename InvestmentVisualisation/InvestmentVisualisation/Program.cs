using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataBaseRepository;
using HttpDataRepository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IMySqlCommonRepository, CommonRepository>();
builder.Services.AddTransient<IMySqlIncomingRepository, MySqlIncomingRepository>();
builder.Services.AddTransient<IMySqlDealsRepository, MySqlDealsRepository>();
builder.Services.AddTransient<IMySqlMoneyRepository, MySqlMoneyRepository>(); 
builder.Services.AddTransient<IMySqlYearViewRepository, MySqlYearViewRepository>();
builder.Services.AddTransient<IMySqlSecCodesRepository, MySqlSecCodesRepository>();
builder.Services.AddTransient<IMySqlSecVolumeRepository, MySqlSecVolumeRepository>();

builder.Services.AddTransient<IWebDividents, WebDividents>();

builder.Services.Configure<DataBaseConnectionSettings>(builder.Configuration.GetSection("DataBaseConnectionSettings"));
builder.Services.Configure<PaginationSettings>(builder.Configuration.GetSection("Pagination"));

builder.Services.Configure<WebDiviPageSettings>("SmLab", builder.Configuration.GetSection("WebPageDividentInfo:SmartLabDiviPageSettings"));
builder.Services.Configure<WebDiviPageSettings>("InvLab", builder.Configuration.GetSection("WebPageDividentInfo:InvLabDiviPageSettings"));
builder.Services.Configure<WebDiviPageSettings>("Dohod", builder.Configuration.GetSection("WebPageDividentInfo:DohodDiviPageSettings"));

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
    //pattern: "{controller=SecVolume}/{action=SecVolumeLast3YearsDynamic}");

app.Run();
