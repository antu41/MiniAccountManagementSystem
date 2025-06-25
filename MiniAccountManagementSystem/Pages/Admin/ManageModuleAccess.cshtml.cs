using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace MiniAccountManagementSystem.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageModuleAccessModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public ManageModuleAccessModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<ModuleAccessViewModel> ModuleAccesses { get; set; }
        public List<string> Roles { get; set; } = new List<string> { "Admin", "Accountant", "Viewer" };
        public List<string> Modules { get; set; } = new List<string> { "ChartOfAccounts", "VoucherEntry" };

        public async Task OnGetAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    ModuleAccesses = (await connection.QueryAsync<ModuleAccessViewModel>(
                        "sp_ManageModuleAccess",
                        new { Action = "List" },
                        commandType: CommandType.StoredProcedure)).ToList();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log them)
                ModelState.AddModelError(string.Empty, "An error occurred while loading module access data.");
            }

        }

        public async Task<IActionResult> OnPostAsync(string roleName, string moduleName, bool canAccess)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.ExecuteAsync(
                    "sp_ManageModuleAccess",
                    new { Action = "Upsert", RoleName = roleName, ModuleName = moduleName, CanAccess = canAccess },
                    commandType: CommandType.StoredProcedure);
            }
            return RedirectToPage();
        }
    }

    public class ModuleAccessViewModel
    {
        public string RoleName { get; set; }
        public string ModuleName { get; set; }
        public bool CanAccess { get; set; }
    }
}