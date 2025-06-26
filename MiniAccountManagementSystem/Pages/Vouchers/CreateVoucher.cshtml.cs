using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MiniAccountManagementSystem.Pages.Vouchers
{
    [Authorize]
    public class CreateVoucherModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ModuleAccessHelperModel _moduleAccessHelper;

        public CreateVoucherModel(IConfiguration configuration, ModuleAccessHelperModel moduleAccessHelper)
        {
            _configuration = configuration;
            _moduleAccessHelper = moduleAccessHelper;
        }

        [BindProperty]
        public VoucherCreateViewModel Voucher { get; set; }
        public List<SelectListItem> Accounts { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var roles = User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value);
            bool hasAccess = false;
            foreach (var role in roles)
            {
                if (await _moduleAccessHelper.HasAccessAsync(role, "VoucherEntry"))
                {
                    hasAccess = true;
                    break;
                }
            }
            if (!hasAccess) return Forbid();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                var accounts = await connection.QueryAsync<AccountViewModel>(
                    "sp_ManageChartofAccounts",
                    new { Action = "List" },
                    commandType: CommandType.StoredProcedure);
                Accounts = accounts.Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.AccountName }).ToList();
            }
            Voucher = new VoucherCreateViewModel
            {
                VoucherDate = DateTime.Today,
                Details = new List<VoucherDetailViewModel> { new VoucherDetailViewModel() }
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                var detailsTable = new DataTable();
                detailsTable.Columns.Add("AccountId", typeof(int));
                detailsTable.Columns.Add("Debit", typeof(decimal));
                detailsTable.Columns.Add("Credit", typeof(decimal));

                foreach (var detail in Voucher.Details)
                {
                    detailsTable.Rows.Add(detail.AccountId, detail.Debit, detail.Credit);
                }

                await connection.ExecuteAsync(
                    "sp_SaveVoucher",
                    new
                    {
                        Voucher.VoucherType,
                        Voucher.VoucherDate,
                        Voucher.ReferenceNo,
                        Details = detailsTable.AsTableValuedParameter("VoucherDetailsType")
                    },
                    commandType: CommandType.StoredProcedure);
            }
            return RedirectToPage("/Vouchers/Index");
        }
    }

    public class VoucherCreateViewModel
    {
        public string VoucherType { get; set; }
        public DateTime VoucherDate { get; set; }
        public string ReferenceNo { get; set; }
        public List<VoucherDetailViewModel> Details { get; set; }
    }

    public class VoucherDetailViewModel
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class AccountViewModel
    {
        public int Id { get; set; }
        public string AccountName { get; set; }
    }

}