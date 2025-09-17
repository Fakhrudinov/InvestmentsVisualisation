using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataBaseRepository;
using HttpDataRepository;
using UserInputService;
using InMemoryRepository;
using InvestmentVisualisation.AuthUserDataRepo;
using InvestmentVisualisation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IMySqlCommonRepository, CommonRepository>();
builder.Services.AddTransient<IMySqlIncomingRepository, MySqlIncomingRepository>();
builder.Services.AddTransient<IMySqlDealsRepository, MySqlDealsRepository>();
builder.Services.AddTransient<IMySqlMoneyRepository, MySqlMoneyRepository>(); 
builder.Services.AddTransient<IMySqlYearViewRepository, MySqlYearViewRepository>();
builder.Services.AddTransient<IMySqlSecCodesRepository, MySqlSecCodesRepository>();
builder.Services.AddTransient<IMySqlSecVolumeRepository, MySqlSecVolumeRepository>();
builder.Services.AddTransient<IMySqlWishListRepository, MySqlWishListRepository>();
builder.Services.AddTransient<IMySqlBankDepositsRepository, MySqlBankDepositsRepository>();
builder.Services.AddTransient<IMySqlMoneySpentRepository, MySqlMoneySpentRepository>();

builder.Services.AddTransient<IWebData, WebData>();

builder.Services.Configure<DataBaseConnectionSettings>(builder.Configuration.GetSection("DataBaseConnectionSettings"));
Console.WriteLine($"Connected to " +
	$"{builder.Configuration.GetSection("DataBaseConnectionSettings:Server").Value} " +
	$"{builder.Configuration.GetSection("DataBaseConnectionSettings:Database").Value}");
builder.Services.Configure<PaginationSettings>(builder.Configuration.GetSection("Pagination"));

builder.Services
    .Configure<WebDiviPageSettings>("SmLab", builder.Configuration.GetSection("WebPageDividentInfo:SmartLabDiviPageSettings"));
builder.Services
    .Configure<WebDiviPageSettings>("Dohod", builder.Configuration.GetSection("WebPageDividentInfo:DohodDiviPageSettings"));
builder.Services
    .Configure<WebDiviPageSettings>("Vsdelke", builder.Configuration.GetSection("WebPageDividentInfo:VsdelkeDiviPageSettings"));

builder.Services.AddTransient<InputHelper>();

builder.Services.AddSingleton<IInMemoryRepository, Repository>();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(config =>
{
	// for in memory database  
	config.UseInMemoryDatabase("MemoryBaseDataBase");
});

// AddIdentity :-  Registers the services
builder.Services.AddIdentity<AppUser, IdentityRole>(config =>
{
	// User defined password policy settings.  
	config.Password.RequiredLength = 4;
	config.Password.RequireDigit = false;
	config.Password.RequireNonAlphanumeric = false;
	config.Password.RequireUppercase = false;
})
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();
builder.Services.TryAddScoped<SignInManager<AppUser>>();//for @inject SignInManager<AppUser>

// Cookie settings
builder.Services.ConfigureApplicationCookie(config =>
{
	config.Cookie.Name = "InvestmentVisualisationCookie";
	config.LoginPath = "/Home/Index"; // User defined login path  
	config.ExpireTimeSpan = TimeSpan.FromDays(7);// <-------------------------------
});


WebApplication app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Incoming/Error");
    // The default HSTS value is 30 days.
    // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");
//pattern: "{controller=Incoming}/{action=Incoming}/{id?}");
//pattern: "{controller=Incoming}/{action=CreateIncoming}");
//pattern: "{controller=Deals}/{action=CreateNewDeals}");


// set users and roles
List<AppUser> users = app.Configuration.GetSection("UserList").Get<List<AppUser>>();
if (users is null || users.Count == 0)
{
	Console.WriteLine($"Users list is null or empty! Check appsettings section 'UserList'");
}
else
{
	//seedeng roles
	List<string> roles = new List<string>();
	foreach (AppUser user in users)
	{
		if (user.Role is not null && !roles.Contains(user.Role))
		{
			roles.Add(user.Role);
		}
	}
	using (IServiceScope scope = app.Services.CreateScope())
	{
		RoleManager<IdentityRole> roleManager = scope
			.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
		foreach (string role in roles)
		{
			IdentityResult roleAdd = await roleManager.CreateAsync(new IdentityRole(role));
			if (!roleAdd.Succeeded)
			{
				Console.WriteLine($"Create role {role} faulted, errors:");
				IEnumerable<IdentityError> errors = roleAdd.Errors;
				foreach (IdentityError? err in errors)
				{
					if (err != null)
					{
						Console.WriteLine(err);
					}
				}
			}
		}
	}
	// seedeng users
	using (IServiceScope scope = app.Services.CreateScope())
	{
		UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
		int id = 1;
		foreach (AppUser user in users)
		{
			user.Id = id.ToString();
			user.UserName = user.Name;
			IdentityResult result = await userManager.CreateAsync(user, user.Password);
			if (!result.Succeeded)
			{
				Console.WriteLine($"Create user id={user.Id} name={user.Name} faulted, errors:");
				IEnumerable<IdentityError> errors = result.Errors;
				foreach (IdentityError? err in errors)
				{
					if (err != null)
					{
						Console.WriteLine(err);
					}
				}
			}
			// add role to user
			if (user.Role is not null)
			{
				IdentityResult roleAddResult = await userManager.AddToRoleAsync(user, user.Role);
				if (!roleAddResult.Succeeded)
				{
					Console.WriteLine($"Add role {user.Role} to user id={user.Id} name={user.Name} faulted, errors:");
					IEnumerable<IdentityError> errors = roleAddResult.Errors;
					foreach (IdentityError? err in errors)
					{
						if (err != null)
						{
							Console.WriteLine(err);
						}
					}
				}
			}
			id++;
		}
	}
}


app.Run();

/*
 * 22 december(12) 2024 - self signed sertificate will be out of date
 * error on start is NET::ERR_CERT_INVALID
 * 
 * Do this in Package Manager console:
 *  dotnet dev-certs https --clean
 *  dotnet dev-certs https --trust
 *  restart brawser
 * 
 * How to see certificates:
 * win+r and run certmgr.msc
 */
