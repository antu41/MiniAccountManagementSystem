using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MiniAccountManagementSystem.Pages.Accounts
{
    [Authorize]
    public class ManageAccountsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ModuleAccessHelperModel _moduleAccessHelper;

        public ManageAccountsModel(IConfiguration configuration, ModuleAccessHelperModel moduleAccessHelper)
        {
            _configuration = configuration;
            _moduleAccessHelper = moduleAccessHelper;
        }

        public List<AccountViewModel> Accounts { get; set; } = new List<AccountViewModel>();
        public List<AccountViewModel> RootAccounts { get; set; } = new List<AccountViewModel>();

        [BindProperty]
        public AccountViewModel Account { get; set; } = new AccountViewModel();

        public async Task<IActionResult> OnGetAsync()
        {
            var roles = User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value);
            bool hasAccess = false;

            foreach (var role in roles)
            {
                if (await _moduleAccessHelper.HasAccessAsync(role, "ChartOfAccounts"))
                {
                    hasAccess = true;
                    break;
                }
            }

            if (!hasAccess) return Forbid();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                Accounts = (await connection.QueryAsync<AccountViewModel>(
                    "sp_ManageChartofAccounts",
                    new { Action = "List" },
                    commandType: CommandType.StoredProcedure)).ToList();
            }

            // Build the tree structure - get root accounts (accounts with no parent)
            RootAccounts = Accounts.Where(a => a.ParentId == null).ToList();

            // Build children for each account
            foreach (var account in Accounts)
            {
                account.Children = Accounts.Where(a => a.ParentId == account.Id).ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload data
                return Page();
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.ExecuteAsync(
                    "sp_ManageChartofAccounts",
                    new
                    {
                        Action = "Create",
                        AccountName = Account.AccountName,
                        AccountType = Account.AccountType,
                        ParentId = Account.ParentId == 0 ? (int?)null : Account.ParentId
                    },
                    commandType: CommandType.StoredProcedure);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload data
                return Page();
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.ExecuteAsync(
                    "sp_ManageChartofAccounts",
                    new
                    {
                        Action = "Update",
                        Id = Account.Id,
                        AccountName = Account.AccountName,
                        AccountType = Account.AccountType,
                        ParentId = Account.ParentId == 0 ? (int?)null : Account.ParentId
                    },
                    commandType: CommandType.StoredProcedure);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.ExecuteAsync(
                    "sp_ManageChartofAccounts",
                    new { Action = "Delete", Id = id },
                    commandType: CommandType.StoredProcedure);
            }

            return RedirectToPage();
        }
    }

    public class AccountViewModel
    {
        public int Id { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public List<AccountViewModel> Children { get; set; } = new List<AccountViewModel>();
        public int Level { get; set; } = 0;
    }
}