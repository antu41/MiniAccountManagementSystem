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
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_ManageModuleAccess",
                    new { Action = "List", RoleName = roleName, ModuleName = moduleName },
                    commandType: CommandType.StoredProcedure);
                return result?.CanAccess == true;
            }
        }
    }
}
