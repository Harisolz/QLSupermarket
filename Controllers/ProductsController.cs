
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLSupermarket.Models;

namespace QLSupermarket.Controllers;

public class ProductsController : Controller
{
    private readonly QlsupermarketDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        QlsupermarketDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ==========================================
    // GET: Products
    // AJAX + SEARCH + FILTER + PAGINATION
    // ==========================================
    public async Task<IActionResult> Index(
        string? searchString,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int page = 1)
    {
        const int pageSize = 5;

        if (page < 1)
            page = 1;

        // Kiểm tra khoảng giá
        if ((minPrice.HasValue && minPrice.Value < 0) ||
            (maxPrice.HasValue && maxPrice.Value < 0) ||
            (minPrice.HasValue && maxPrice.HasValue &&
             minPrice.Value > maxPrice.Value))
        {
            ModelState.AddModelError(
                "",
                "Khoảng giá không hợp lệ. Bộ lọc giá đã được bỏ qua.");

            minPrice = null;
            maxPrice = null;
        }

        // Lấy danh mục cho bộ lọc
        ViewBag.Categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        // Tạo truy vấn sản phẩm
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        // Tìm kiếm theo tên sản phẩm
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            string keyword = searchString.Trim();

            query = query.Where(p =>
                p.ProductName.Contains(keyword));
        }

        // Lọc theo danh mục
        if (categoryId.HasValue)
        {
            query = query.Where(p =>
                p.CategoryId == categoryId.Value);
        }

        // Lọc theo giá tối thiểu
        if (minPrice.HasValue)
        {
            query = query.Where(p =>
                p.Price >= minPrice.Value);
        }

        // Lọc theo giá tối đa
        if (maxPrice.HasValue)
        {
            query = query.Where(p =>
                p.Price <= maxPrice.Value);
        }

        // Đếm tổng sản phẩm sau lọc
        int totalItems = await query.CountAsync();

        // Tính tổng số trang
        int totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize);

        // Đảm bảo trang hợp lệ
        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        // Lấy dữ liệu theo trang
        var products = await query
            .OrderBy(p => p.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Lưu điều kiện tìm kiếm
        ViewBag.SearchString = searchString;
        ViewBag.CategoryId = categoryId;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;

        // Lưu thông tin phân trang
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;

        // Kiểm tra request AJAX
        bool isAjax = Request.Headers["X-Requested-With"]
            .ToString()
            .Equals(
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

        if (isAjax)
        {
            // Chỉ trả về bảng sản phẩm và phân trang
            return PartialView("_ProductTable", products);
        }

        // Request thông thường: trả về toàn bộ trang
        return View(products);
    }

    // ==========================================
    // GET: Products/Details/5
    // ==========================================
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    // ==========================================
    // GET: Products/Create
    // ==========================================
    public IActionResult Create()
    {
        ViewData["CategoryId"] = new SelectList(
            _context.Categories,
            "CategoryId",
            "CategoryName");

        return View();
    }

    // ==========================================
    // POST: Products/Create
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Product product,
        IFormFile? imageFile)
    {
        ModelState.Remove(nameof(Product.Category));

        if (!ModelState.IsValid)
        {
            ViewData["CategoryId"] = new SelectList(
                _context.Categories,
                "CategoryId",
                "CategoryName",
                product.CategoryId);

            return View(product);
        }

        // Upload ảnh nếu có
        if (imageFile != null && imageFile.Length > 0)
        {
            string? fileName = await SaveProductImage(imageFile);

            if (fileName == null)
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Ảnh không hợp lệ. Chỉ chấp nhận JPG, PNG, WEBP tối đa 5MB.");

                ViewData["CategoryId"] = new SelectList(
                    _context.Categories,
                    "CategoryId",
                    "CategoryName",
                    product.CategoryId);

                return View(product);
            }

            product.ImageUrl = fileName;
        }

        product.CreatedAt = DateTime.Now;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Thêm sản phẩm thành công!";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // GET: Products/Edit/5
    // ==========================================
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        ViewData["CategoryId"] = new SelectList(
            _context.Categories,
            "CategoryId",
            "CategoryName",
            product.CategoryId);

        return View(product);
    }

    // ==========================================
    // POST: Products/Edit/5
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        Product product,
        IFormFile? imageFile)
    {
        if (id != product.ProductId)
            return NotFound();

        ModelState.Remove(nameof(Product.Category));

        if (!ModelState.IsValid)
        {
            ViewData["CategoryId"] = new SelectList(
                _context.Categories,
                "CategoryId",
                "CategoryName",
                product.CategoryId);

            return View(product);
        }

        var existingProduct = await _context.Products.FindAsync(id);

        if (existingProduct == null)
            return NotFound();

        // Cập nhật thông tin sản phẩm
        existingProduct.ProductName = product.ProductName;
        existingProduct.CategoryId = product.CategoryId;
        existingProduct.Price = product.Price;
        existingProduct.StockQuantity = product.StockQuantity;
        existingProduct.Description = product.Description;

        // Chỉ thay ảnh khi có upload ảnh mới
        if (imageFile != null && imageFile.Length > 0)
        {
            string? fileName = await SaveProductImage(imageFile);

            if (fileName == null)
            {
                ModelState.AddModelError(
                    "imageFile",
                    "Ảnh không hợp lệ. Chỉ chấp nhận JPG, PNG, WEBP tối đa 5MB.");

                ViewData["CategoryId"] = new SelectList(
                    _context.Categories,
                    "CategoryId",
                    "CategoryName",
                    product.CategoryId);

                return View(product);
            }

            existingProduct.ImageUrl = fileName;
        }

        try
        {
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật sản phẩm thành công!";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
                return NotFound();

            ModelState.AddModelError(
                "",
                "Không thể cập nhật sản phẩm. Vui lòng thử lại.");
        }

        ViewData["CategoryId"] = new SelectList(
            _context.Categories,
            "CategoryId",
            "CategoryName",
            product.CategoryId);

        return View(product);
    }

    // ==========================================
    // GET: Products/Delete/5
    // ==========================================
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    // ==========================================
    // POST: Products/Delete/5
    // ==========================================
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        try
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa sản phẩm thành công!";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] =
                "Không thể xóa sản phẩm vì dữ liệu đang được sử dụng.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // HELPER: KIỂM TRA SẢN PHẨM TỒN TẠI
    // ==========================================
    private bool ProductExists(int id)
    {
        return _context.Products.Any(p => p.ProductId == id);
    }

    // ==========================================
    // HELPER: UPLOAD ẢNH SẢN PHẨM
    // ==========================================
    private async Task<string?> SaveProductImage(IFormFile imageFile)
    {
        const long maxFileSize = 5 * 1024 * 1024;

        if (imageFile.Length <= 0 ||
            imageFile.Length > maxFileSize)
        {
            return null;
        }

        string extension = Path
            .GetExtension(imageFile.FileName)
            .ToLowerInvariant();

        string[] allowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension))
            return null;

        // Kiểm tra chữ ký định dạng ảnh
        byte[] header = new byte[12];

        using (var input = imageFile.OpenReadStream())
        {
            int bytesRead = await input.ReadAsync(
                header.AsMemory(0, header.Length));

            bool isJpeg = bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;

            bool isPng = bytesRead >= 8 &&
                header.AsSpan(0, 8).SequenceEqual(
                    new byte[] {
                        137, 80, 78, 71, 13, 10, 26, 10
                    });

            bool isWebp = bytesRead >= 12 &&
                System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";

            bool valid = extension switch
            {
                ".jpg" or ".jpeg" => isJpeg,
                ".png" => isPng,
                ".webp" => isWebp,
                _ => false
            };

            if (!valid)
                return null;
        }

        string webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

        string folderPath = Path.Combine(webRoot, "products");

        Directory.CreateDirectory(folderPath);

        string fileName =
            Guid.NewGuid().ToString("N") + extension;

        string filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(
            filePath,
            FileMode.CreateNew))
        {
            await imageFile.CopyToAsync(stream);
        }

        return fileName;
    }
}
