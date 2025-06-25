using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MiniAccountManagementSystem.Pages.Admin
{
    public class SeedRolesModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public SeedRolesModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string[] roles = { "Admin", "Accountant", "Viewer" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
            return RedirectToPage("/Index");
        }
    }
}
