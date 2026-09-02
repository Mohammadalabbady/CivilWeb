using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CivilWeb.Data;
using CivilWeb.Models;
using ClosedXML.Excel;

namespace CivilWeb.Controllers
{
    [Authorize] // Require authentication for all actions
    public class CivilController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CivilController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Civil
        public async Task<IActionResult> Index(
            string searchId,
            string searchRegNumber,
            string searchFirstName,
            string searchSecondName,
            string searchThirdName,
            string searchLastName,
            string searchBirthDate,
            string searchName,
            int? pageNumber,
            int? pageSize,
            string sortOrder)
        {
            // Use AsNoTracking for read-only queries (faster performance)
            var query = _context.CivilData.AsNoTracking().AsQueryable();

            query = ApplySearchFilters(query, searchId, searchRegNumber, searchFirstName, searchSecondName, searchThirdName, searchLastName, searchBirthDate, searchName);

            // Sorting
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IdSort"] = sortOrder == "id" ? "id_desc" : "id";
            ViewData["FirstNameSort"] = sortOrder == "firstname" ? "firstname_desc" : "firstname";
            ViewData["SecondNameSort"] = sortOrder == "secondname" ? "secondname_desc" : "secondname";
            ViewData["ThirdNameSort"] = sortOrder == "thirdname" ? "thirdname_desc" : "thirdname";
            ViewData["LastNameSort"] = sortOrder == "lastname" ? "lastname_desc" : "lastname";
            ViewData["FullNameSort"] = sortOrder == "fullname" ? "fullname_desc" : "fullname";
            ViewData["GenderSort"] = sortOrder == "gender" ? "gender_desc" : "gender";
            ViewData["BirthDateSort"] = sortOrder == "birthdate" ? "birthdate_desc" : "birthdate";
            ViewData["AliveSort"] = sortOrder == "alive" ? "alive_desc" : "alive";

            query = sortOrder switch
            {
                "id" => query.OrderBy(c => c.Id),
                "id_desc" => query.OrderByDescending(c => c.Id),
                "firstname" => query.OrderBy(c => c.FirstName),
                "firstname_desc" => query.OrderByDescending(c => c.FirstName),
                "secondname" => query.OrderBy(c => c.SecondName),
                "secondname_desc" => query.OrderByDescending(c => c.SecondName),
                "thirdname" => query.OrderBy(c => c.ThirdName),
                "thirdname_desc" => query.OrderByDescending(c => c.ThirdName),
                "lastname" => query.OrderBy(c => c.LastName),
                "lastname_desc" => query.OrderByDescending(c => c.LastName),
                "fullname" => query.OrderBy(c => c.FullName),
                "fullname_desc" => query.OrderByDescending(c => c.FullName),
                "gender" => query.OrderBy(c => c.Gender),
                "gender_desc" => query.OrderByDescending(c => c.Gender),
                "birthdate" => query.OrderBy(c => c.BirhtOfDate),
                "birthdate_desc" => query.OrderByDescending(c => c.BirhtOfDate),
                "alive" => query.OrderBy(c => c.Alive),
                "alive_desc" => query.OrderByDescending(c => c.Alive),
                _ => query.OrderBy(c => c.Id)
            };

            ViewData["SearchId"] = searchId;
            ViewData["SearchRegNumber"] = searchRegNumber;
            ViewData["SearchFirstName"] = searchFirstName;
            ViewData["SearchSecondName"] = searchSecondName;
            ViewData["SearchThirdName"] = searchThirdName;
            ViewData["SearchLastName"] = searchLastName;
            ViewData["SearchBirthDate"] = searchBirthDate;
            ViewData["SearchName"] = searchName;

            int currentPageSize = pageSize ?? 10;
            ViewData["PageSize"] = currentPageSize;

            var data = await PaginatedList<CivilDatum>.CreateAsync(query, pageNumber ?? 1, currentPageSize);
            return View(data);
        }

        // GET: Civil/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var civilDatum = await _context.CivilData
                .FirstOrDefaultAsync(m => m.Id == id);
            if (civilDatum == null)
            {
                return NotFound();
            }

            return View(civilDatum);
        }

        // GET: Civil/Create - Admin only
        [Authorize(Policy = "Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Civil/Create - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Create([Bind("Id,FirstName,SecondName,ThirdName,LastName,FullName,RegistrationNumber,BirhtOfDate,Gender,Alive")] CivilDatum civilDatum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(civilDatum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(civilDatum);
        }

        // GET: Civil/Edit/5 - Admin only
        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var civilDatum = await _context.CivilData.FindAsync(id);
            if (civilDatum == null)
            {
                return NotFound();
            }
            return View(civilDatum);
        }

        // POST: Civil/Edit/5 - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Edit(string id, [Bind("Id,FirstName,SecondName,ThirdName,LastName,FullName,RegistrationNumber,BirhtOfDate,Gender,Alive")] CivilDatum civilDatum)
        {
            if (id != civilDatum.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(civilDatum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CivilDatumExists(civilDatum.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(civilDatum);
        }

        // GET: Civil/Delete/5 - Admin only
        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var civilDatum = await _context.CivilData
                .FirstOrDefaultAsync(m => m.Id == id);
            if (civilDatum == null)
            {
                return NotFound();
            }

            return View(civilDatum);
        }

        // POST: Civil/Delete/5 - Admin only
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var civilDatum = await _context.CivilData.FindAsync(id);
            if (civilDatum != null)
            {
                _context.CivilData.Remove(civilDatum);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Civil/Export - Admin only
        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Export(
            string searchId,
            string searchRegNumber,
            string searchFirstName,
            string searchSecondName,
            string searchThirdName,
            string searchLastName,
            string searchBirthDate,
            string searchName,
            string sortOrder)
        {
            // Use AsNoTracking for read-only export (faster performance)
            var query = _context.CivilData.AsNoTracking().AsQueryable();

            query = ApplySearchFilters(query, searchId, searchRegNumber, searchFirstName, searchSecondName, searchThirdName, searchLastName, searchBirthDate, searchName);

            // Apply same sorting
            query = sortOrder switch
            {
                "id" => query.OrderBy(c => c.Id),
                "id_desc" => query.OrderByDescending(c => c.Id),
                "firstname" => query.OrderBy(c => c.FirstName),
                "firstname_desc" => query.OrderByDescending(c => c.FirstName),
                "secondname" => query.OrderBy(c => c.SecondName),
                "secondname_desc" => query.OrderByDescending(c => c.SecondName),
                "thirdname" => query.OrderBy(c => c.ThirdName),
                "thirdname_desc" => query.OrderByDescending(c => c.ThirdName),
                "lastname" => query.OrderBy(c => c.LastName),
                "lastname_desc" => query.OrderByDescending(c => c.LastName),
                "fullname" => query.OrderBy(c => c.FullName),
                "fullname_desc" => query.OrderByDescending(c => c.FullName),
                "gender" => query.OrderBy(c => c.Gender),
                "gender_desc" => query.OrderByDescending(c => c.Gender),
                "birthdate" => query.OrderBy(c => c.BirhtOfDate),
                "birthdate_desc" => query.OrderByDescending(c => c.BirhtOfDate),
                "alive" => query.OrderBy(c => c.Alive),
                "alive_desc" => query.OrderByDescending(c => c.Alive),
                _ => query.OrderBy(c => c.Id)
            };

            // Limit to Excel max rows (1,048,575 data rows + 1 header = 1,048,576)
            const int maxExcelRows = 1048575;
            var data = await query.Take(maxExcelRows).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Civil Data");

            // Header row
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "First Name";
            worksheet.Cell(1, 3).Value = "Second Name";
            worksheet.Cell(1, 4).Value = "Third Name";
            worksheet.Cell(1, 5).Value = "Last Name";
            worksheet.Cell(1, 6).Value = "Full Name";
            worksheet.Cell(1, 7).Value = "Registration Number";
            worksheet.Cell(1, 8).Value = "Birth Date";
            worksheet.Cell(1, 9).Value = "Gender";
            worksheet.Cell(1, 10).Value = "Alive";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;

            // Data rows
            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Id ?? "";
                worksheet.Cell(row, 2).Value = item.FirstName ?? "";
                worksheet.Cell(row, 3).Value = item.SecondName ?? "";
                worksheet.Cell(row, 4).Value = item.ThirdName ?? "";
                worksheet.Cell(row, 5).Value = item.LastName ?? "";
                worksheet.Cell(row, 6).Value = item.FullName ?? "";
                worksheet.Cell(row, 7).Value = item.RegistrationNumber ?? "";
                worksheet.Cell(row, 8).Value = item.BirhtOfDate.HasValue ? item.BirhtOfDate.Value.ToString("yyyy-MM-dd") : "";
                worksheet.Cell(row, 9).Value = item.Gender ?? "";
                worksheet.Cell(row, 10).Value = item.Alive == true ? "Yes" : "No";
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Return file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"CivilData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Civil/Search (API for live filtering) - SUPER FAST
        [HttpGet]
        public async Task<IActionResult> Search(
            string searchId,
            string searchRegNumber,
            string searchFirstName,
            string searchSecondName,
            string searchThirdName,
            string searchLastName,
            string searchBirthDate,
            string searchName,
            int? pageNumber,
            int? pageSize,
            string sortOrder)
        {
            var query = _context.CivilData.AsNoTracking().AsQueryable();

            query = ApplySearchFilters(query, searchId, searchRegNumber, searchFirstName, searchSecondName, searchThirdName, searchLastName, searchBirthDate, searchName);

            // Sorting
            query = sortOrder switch
            {
                "id" => query.OrderBy(c => c.Id),
                "id_desc" => query.OrderByDescending(c => c.Id),
                "firstname" => query.OrderBy(c => c.FirstName),
                "firstname_desc" => query.OrderByDescending(c => c.FirstName),
                "secondname" => query.OrderBy(c => c.SecondName),
                "secondname_desc" => query.OrderByDescending(c => c.SecondName),
                "thirdname" => query.OrderBy(c => c.ThirdName),
                "thirdname_desc" => query.OrderByDescending(c => c.ThirdName),
                "lastname" => query.OrderBy(c => c.LastName),
                "lastname_desc" => query.OrderByDescending(c => c.LastName),
                "gender" => query.OrderBy(c => c.Gender),
                "gender_desc" => query.OrderByDescending(c => c.Gender),
                "birthdate" => query.OrderBy(c => c.BirhtOfDate),
                "birthdate_desc" => query.OrderByDescending(c => c.BirhtOfDate),
                "alive" => query.OrderBy(c => c.Alive),
                "alive_desc" => query.OrderByDescending(c => c.Alive),
                _ => query.OrderBy(c => c.Id)
            };

            int currentPageSize = pageSize.HasValue && pageSize.Value > 0 ? pageSize.Value : 10;
            int currentPage = pageNumber.HasValue && pageNumber.Value > 0 ? pageNumber.Value : 1;

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)currentPageSize);

            var data = await query
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .Select(c => new
                {
                    c.Id,
                    c.RegistrationNumber,
                    c.FirstName,
                    c.SecondName,
                    c.ThirdName,
                    c.LastName,
                    c.Gender,
                    BirthDate = c.BirhtOfDate.HasValue ? c.BirhtOfDate.Value.ToString("yyyy-MM-dd") : null,
                    c.Alive
                })
                .ToListAsync();

            return Json(new
            {
                data,
                totalCount,
                totalPages,
                pageIndex = currentPage,
                hasPreviousPage = currentPage > 1,
                hasNextPage = currentPage < totalPages
            });
        }

        private IQueryable<CivilDatum> ApplySearchFilters(
            IQueryable<CivilDatum> query,
            string? searchId,
            string? searchRegNumber,
            string? searchFirstName,
            string? searchSecondName,
            string? searchThirdName,
            string? searchLastName,
            string? searchBirthDate,
            string? searchName = null)
        {
            if (!string.IsNullOrEmpty(searchId))
            {
                var trimmed = searchId.Trim();
                query = query.Where(c => c.Id.StartsWith(trimmed));
            }

            if (!string.IsNullOrEmpty(searchRegNumber))
            {
                var trimmed = searchRegNumber.Trim();
                query = query.Where(c => c.RegistrationNumber != null && c.RegistrationNumber.StartsWith(trimmed));
            }

            if (!string.IsNullOrEmpty(searchFirstName))
            {
                var trimmed = searchFirstName.Trim();
                query = query.Where(c => c.FirstName != null && c.FirstName.StartsWith(trimmed));
            }

            if (!string.IsNullOrEmpty(searchSecondName))
            {
                var trimmed = searchSecondName.Trim();
                query = query.Where(c => c.SecondName != null && c.SecondName.StartsWith(trimmed));
            }

            if (!string.IsNullOrEmpty(searchThirdName))
            {
                var trimmed = searchThirdName.Trim();
                query = query.Where(c => c.ThirdName != null && c.ThirdName.StartsWith(trimmed));
            }

            if (!string.IsNullOrEmpty(searchLastName))
            {
                var trimmed = searchLastName.Trim();
                query = query.Where(c => c.LastName != null && c.LastName.StartsWith(trimmed));
            }

            if (!string.IsNullOrEmpty(searchBirthDate))
            {
                if (DateOnly.TryParse(searchBirthDate, out DateOnly birthDate))
                {
                    query = query.Where(c => c.BirhtOfDate.HasValue && c.BirhtOfDate.Value == birthDate);
                }
            }

            if (!string.IsNullOrEmpty(searchName))
            {
                var trimmedName = searchName.Trim();
                query = query.Where(c =>
                    (c.FirstName != null && c.FirstName.StartsWith(trimmedName)) ||
                    (c.SecondName != null && c.SecondName.StartsWith(trimmedName)) ||
                    (c.ThirdName != null && c.ThirdName.StartsWith(trimmedName)) ||
                    (c.LastName != null && c.LastName.StartsWith(trimmedName)) ||
                    (c.FullName != null && c.FullName.StartsWith(trimmedName)));
            }

            return query;
        }

        private bool CivilDatumExists(string id)
        {
            return _context.CivilData.Any(e => e.Id == id);
        }
    }
}
