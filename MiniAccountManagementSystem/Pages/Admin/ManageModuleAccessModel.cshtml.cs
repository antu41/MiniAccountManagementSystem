using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MiniAccountManagementSystem.Pages.Admin
{
    public class ManageModuleAccessModelModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ManageModuleAccessModelModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void OnGet()
        {
        }
        public List<ModuleAccessViewModel> ModuleAccesses { get; set; }
        public List<string> Roles { get; set; } = new List<string> { "Admin", "Accountant", "Viewer" };
        public List<string> Modules { get; set; } = new List<string> { "ChartOfAccounts", "VoucherEntry" };

        public async Task OnGetAsync()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                ModuleAccesses = (await connection.QueryAsync<ModuleAccessViewModel>(
                    "sp_ManageModuleAccess",
                    new { Action = "List" },
                    commandType: CommandType.StoredProcedure)).ToList();
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