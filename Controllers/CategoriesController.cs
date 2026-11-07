
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSupermarket.Models;

namespace QLSupermarket.Controllers;

public class CategoriesController : Controller
{
    private readonly QlsupermarketDbContext _context;

    public CategoriesController(QlsupermarketDbContext context)
    {
        _context = context;
    }

    // GET: Categories
    // Hiển thị danh mục và số lượng sản phẩm
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return View(categories);
    }

    // GET: Categories/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound();

        return View(category);
    }

    // GET: Categories/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CategoryName,Description")] Category category)
    {
        ModelState.Remove(nameof(Category.Products));

        if (!ModelState.IsValid)
            return View(category);

        // Kiểm tra trùng tên danh mục
        bool exists = await _context.Categories
            .AnyAsync(c => c.CategoryName == category.CategoryName);

        if (exists)
        {
            ModelState.AddModelError(
                nameof(Category.CategoryName),
                "Tên danh mục đã tồn tại.");

            return View(category);
        }

        try
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công!";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "Không thể thêm danh mục. Vui lòng kiểm tra dữ liệu.");
        }

        return View(category);
    }

    // GET: Categories/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        return View(category);
    }

    // POST: Categories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("CategoryId,CategoryName,Description")] Category input)
    {
        if (id != input.CategoryId)
            return NotFound();

        ModelState.Remove(nameof(Category.Products));

        if (!ModelState.IsValid)
            return View(input);

        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        // Kiểm tra trùng tên với danh mục khác
        bool exists = await _context.Categories
            .AnyAsync(c =>
                c.CategoryName == input.CategoryName &&
                c.CategoryId != id);

        if (exists)
        {
            ModelState.AddModelError(
                nameof(Category.CategoryName),
                "Tên danh mục đã được sử dụng.");

            return View(input);
        }

        category.CategoryName = input.CategoryName;
        category.Description = input.Description;

        try
        {
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật danh mục thành công!";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(
                "",
                "Danh mục đã bị thay đổi hoặc xóa.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "Không thể cập nhật danh mục trong database.");
        }

        return View(input);
    }

    // GET: Categories/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound();

        return View(category);
    }

    // POST: Categories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        // Không cho xóa danh mục đang chứa sản phẩm
        bool hasProducts = await _context.Products
            .AnyAsync(p => p.CategoryId == id);

        if (hasProducts)
        {
            TempData["Error"] =
                "Không thể xóa danh mục vì vẫn còn sản phẩm thuộc danh mục này.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công!";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Error"] =
                "Không thể xóa danh mục vì dữ liệu đang được sử dụng.";
        }

        return RedirectToAction(nameof(Index));
    }
}
