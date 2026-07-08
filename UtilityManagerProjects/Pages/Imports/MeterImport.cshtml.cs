using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataLibrary.Db;
using System.Globalization;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace UtilityManagerProjects.Pages.Imports
{
    public class MeterImportModel : PageModel
    {
        private readonly ConnectionStringData connection;

        public MeterImportModel(ConnectionStringData connection)
        {
            this.connection = connection;
        }

        public string Message { get; set; } = string.Empty;
        //public async Task<IActionResult> OnPostImportAsync(IFormFile file)
        //{
        //    if(file == null || file.Length ==0)
        //    {
        //        Message = "Please select a file";
        //        return Page();
        //    }

        //    using var reader = new StreamReader(file.OpenReadStream());
        //    using var csv = new CsvReader(reader ,CultureInfo.InvariantCulture);
        //}
    }
}
