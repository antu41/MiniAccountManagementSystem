using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace MiniAccountManagementSystem.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageUsersModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        public ManageUsersModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public List<UserViewModel> Users { get; set; }
        public List<string> Roles { get; set; }

        public async Task OnGetAsync()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                Users = (await connection.QueryAsync<UserViewModel>(
                    "sp_ManageUsers",
                    new { Action = "List" },
                    commandType: CommandType.StoredProcedure)).ToList();
            }
            Roles = _roleManager.Roles.Select(r => r.Name).ToList();
        }

        public async Task<IActionResult> OnPostAssignRoleAsync(string userId, string roleName)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.ExecuteAsync(
                    "sp_ManageUsers",
                    new { Action = "AssignRole", UserId = userId, RoleName = roleName },
                    commandType: CommandType.StoredProcedure);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveRoleAsync(string userId, string roleName)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.ExecuteAsync(
                    "sp_ManageUsers",
                    new { Action = "RemoveRole", UserId = userId, RoleName = roleName },
                    commandType: CommandType.StoredProcedure);
            }
            return RedirectToPage();
        }
    }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}