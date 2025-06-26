using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Data;

namespace MiniAccountManagementSystem.Pages.Vouchers
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration; private readonly ModuleAccessHelperModel _moduleAccessHelper;

        public IndexModel(IConfiguration configuration, ModuleAccessHelperModel moduleAccessHelper)
        {
            _configuration = configuration;
            _moduleAccessHelper = moduleAccessHelper;
        }

        public List<VoucherViewModel> Vouchers { get; set; }

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
                Vouchers = (await connection.QueryAsync<VoucherViewModel>(
                    "sp_GetVouchers",
                    commandType: CommandType.StoredProcedure)).ToList();
            }
            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                var vouchers = await connection.QueryAsync<VoucherViewModel>(
                    "sp_GetVouchers",
                    commandType: CommandType.StoredProcedure);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Vouchers");

                    // Header row
                    worksheet.Cells[1, 1].Value = "Type";
                    worksheet.Cells[1, 2].Value = "Date";
                    worksheet.Cells[1, 3].Value = "Reference No";

                    int row = 2;
                    foreach (var voucher in vouchers)
                    {
                        worksheet.Cells[row, 1].Value = voucher.VoucherType;
                        worksheet.Cells[row, 2].Value = voucher.VoucherDate.ToShortDateString();
                        worksheet.Cells[row, 3].Value = voucher.ReferenceNo;
                        row++;
                    }

                    // Optional: Format headers
                    using (var range = worksheet.Cells[1, 1, 1, 3])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Vouchers.xlsx");
                }
            }
        }
    }

    public class VoucherViewModel
    {
        public int Id { get; set; }
        public string VoucherType { get; set; }
        public DateTime VoucherDate { get; set; }
        public string ReferenceNo { get; set; }
    }

}