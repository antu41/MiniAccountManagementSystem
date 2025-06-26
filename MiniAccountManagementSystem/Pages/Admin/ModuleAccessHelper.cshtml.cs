using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MiniAccountManagementSystem.Pages
{
    public class ModuleAccessHelperModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ModuleAccessHelperModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void OnGet()
        {
        }
        public async Task<bool> HasAccessAsync(string roleName, string moduleName)
        {
            if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(moduleName))
                return false;

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var result = await connection.QueryFirstOrDefaultAsync<ModuleAccessResult>(
                        "sp_ManageModuleAccess",
                        new { Action = "List", RoleName = roleName, ModuleName = moduleName },
                        commandType: CommandType.StoredProcedure);

                    return result?.CanAccess ?? false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private class ModuleAccessResult
        {
            public bool CanAccess { get; set; }
        }

    }
}
