using Microsoft.AspNetCore.Identity;

namespace InvestmentVisualisation.Models
{
	public class AppUser : IdentityUser
	{
		public string Name { get; set; }
		public string Password { get; set; }
		public string? Role { get; set; }
	}
}
