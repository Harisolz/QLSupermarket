
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSupermarket.Models;

namespace QLSupermarket.Controllers;

public class OrdersController : Controller
{
    private readonly QlsupermarketDbContext _context;

    public OrdersController(QlsupermarketDbContext context)
    {
        _context = context;
    }

    // ======================================
    // GET: Orders
    // ======================================
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    // ======================================
    // GET: Orders/Details
    // ======================================
    public async Task<IActionResult> Details(int? orderid)
    {
        if (orderid == null)
            return NotFound();

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderid);

        if (order == null)
            return NotFound();

        return View(order);
    }

    // ======================================
    // GET: Orders/Create
    // ======================================
    public async Task<IActionResult> Create()
    {
        await LoadCreateData();

        return View(new Order
        {
            OrderDate = DateTime.Now,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail
                {
                    Quantity = 1
                }
            }
        });
    }

    // ======================================
    // POST: Orders/Create
    // ======================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("UserId,ShippingAddress,OrderDetails")] Order input)
    {
        // Các thuộc tính không nhận trực tiếp từ form
        ModelState.Remove(nameof(Order.User));
        ModelState.Remove(nameof(Order.OrderDate));
        ModelState.Remove(nameof(Order.TotalAmount));
        ModelState.Remove(nameof(Order.Status));

        // Không nhận ID, giá hoặc navigation của chi tiết từ form
        foreach (var key in ModelState.Keys
            .Where(k =>
                k.StartsWith("OrderDetails[") &&
                (k.EndsWith(".OrderDetailId") ||
                 k.EndsWith(".OrderId") ||
                 k.EndsWith(".UnitPrice") ||
                 k.EndsWith(".Order") ||
                 k.EndsWith(".Product")))
            .ToList())
        {
            ModelState.Remove(key);
        }

        // Kiểm tra khách hàng
        if (input.UserId == null ||
            !await _context.Users.AnyAsync(
                u => u.UserId == input.UserId))
        {
            ModelState.AddModelError(
                nameof(Order.UserId),
                "Vui lòng chọn khách hàng hợp lệ.");
        }

        // Kiểm tra địa chỉ
        if (string.IsNullOrWhiteSpace(input.ShippingAddress))
        {
            ModelState.AddModelError(
                nameof(Order.ShippingAddress),
                "Vui lòng nhập địa chỉ giao hàng.");
        }

        // Kiểm tra danh sách sản phẩm
        if (input.OrderDetails == null ||
            !input.OrderDetails.Any())
        {
            ModelState.AddModelError(
                "",
                "Vui lòng thêm ít nhất một sản phẩm.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCreateData();
            return View(input);
        }

        // Gộp các dòng trùng sản phẩm
        var items = (input.OrderDetails ?? new List<OrderDetail>())
    .GroupBy(d => d.ProductId)
    .Select(g => new
    {
        ProductId = g.Key,
        Quantity = g.Sum(d => (long)d.Quantity)
    })
    .ToList();

        if (items.Any(x =>
            x.ProductId == null ||
            x.ProductId <= 0 ||
            x.Quantity <= 0 ||
            x.Quantity > int.MaxValue))
        {
            ModelState.AddModelError(
                "",
                "Sản phẩm hoặc số lượng không hợp lệ.");

            await LoadCreateData();
            return View(input);
        }

        var productIds = items
            .Select(x => x.ProductId!.Value)
            .ToList();

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        try
        {
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                ModelState.AddModelError(
                    "",
                    "Một hoặc nhiều sản phẩm không tồn tại.");

                await transaction.RollbackAsync();
                await LoadCreateData();

                return View(input);
            }

            decimal totalAmount = 0;
            var orderDetails = new List<OrderDetail>();

            foreach (var item in items)
            {
                var product = products.First(
                    p => p.ProductId == item.ProductId);

                // Kiểm tra giá
                if (product.Price < 0)
                {
                    ModelState.AddModelError(
                        "",
                        $"Giá sản phẩm {product.ProductName} không hợp lệ.");

                    await transaction.RollbackAsync();
                    await LoadCreateData();

                    return View(input);
                }

                // Kiểm tra tồn kho
                if (product.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError(
                        "",
                        $"Sản phẩm {product.ProductName} " +
                        $"chỉ còn {product.StockQuantity} sản phẩm.");

                    await transaction.RollbackAsync();
                    await LoadCreateData();

                    return View(input);
                }

                int quantity = (int)item.Quantity;

                // Tính tổng tiền trong Controller
                totalAmount = checked(
                    totalAmount + product.Price * quantity);

                // Tạo chi tiết đơn hàng
                orderDetails.Add(new OrderDetail
                {
                    ProductId = product.ProductId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });

                // Trừ tồn kho
                product.StockQuantity -= quantity;
            }

            // Tạo đơn hàng mới
            var order = new Order
            {
                UserId = input.UserId,
                OrderDate = DateTime.Now,
                ShippingAddress = input.ShippingAddress.Trim(),
                Status = "Pending",
                TotalAmount = totalAmount,
                OrderDetails = orderDetails
            };

            _context.Orders.Add(order);

            // EF Core lưu cả Orders và OrderDetails
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["Success"] =
                "Tạo đơn hàng thành công!";

            return RedirectToAction(
                nameof(Details),
                new { orderid = order.OrderId });
        }
        catch (OverflowException)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                "",
                "Tổng tiền vượt quá giới hạn cho phép.");
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();

            System.Diagnostics.Debug.WriteLine(
                "LỖI TẠO ĐƠN HÀNG: " +
                (ex.InnerException?.Message ?? ex.Message));

            ModelState.AddModelError(
                "",
                "Không thể lưu đơn hàng vào cơ sở dữ liệu.");
        }

        await LoadCreateData();
        return View(input);
    }

    // ======================================
    // GET: Orders/Edit
    // ======================================
    public async Task<IActionResult> Edit(int? orderid)
    {
        if (orderid == null)
            return NotFound();

        var order = await _context.Orders
            .FindAsync(orderid.Value);

        if (order == null)
            return NotFound();

        return View(order);
    }

    // ======================================
    // POST: Orders/Edit
    // ======================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? orderid,
        [Bind("OrderId,UserId,Status,ShippingAddress")] Order input)
    {
        if (orderid == null || orderid != input.OrderId)
            return NotFound();

        // Không yêu cầu các trường không sửa trong form
        ModelState.Remove(nameof(Order.User));
        ModelState.Remove(nameof(Order.OrderDetails));
        ModelState.Remove(nameof(Order.OrderDate));
        ModelState.Remove(nameof(Order.TotalAmount));

        if (input.UserId == null ||
            !await _context.Users.AnyAsync(
                u => u.UserId == input.UserId))
        {
            ModelState.AddModelError(
                nameof(Order.UserId),
                "Khách hàng không tồn tại.");
        }

        if (string.IsNullOrWhiteSpace(input.ShippingAddress))
        {
            ModelState.AddModelError(
                nameof(Order.ShippingAddress),
                "Vui lòng nhập địa chỉ giao hàng.");
        }

        // Chỉ chấp nhận các trạng thái được định nghĩa
        string[] validStatuses =
        {
            "Pending",
            "Processing",
            "Shipping",
            "Delivered",
            "Cancelled"
        };

        if (string.IsNullOrWhiteSpace(input.Status) ||
            !validStatuses.Contains(input.Status))
        {
            ModelState.AddModelError(
                nameof(Order.Status),
                "Trạng thái đơn hàng không hợp lệ.");
        }

        if (!ModelState.IsValid)
            return View(input);

        var existingOrder = await _context.Orders
            .FindAsync(orderid.Value);

        if (existingOrder == null)
            return NotFound();

        // Không cho sửa đơn đã giao hoặc đã hủy
        if (existingOrder.Status == "Delivered" ||
            existingOrder.Status == "Cancelled")
        {
            TempData["Error"] =
                "Không thể chỉnh sửa đơn hàng đã giao hoặc đã hủy.";

            return RedirectToAction(
                nameof(Details),
                new { orderid });
        }

        // Không cho hủy trực tiếp tại Edit vì phải hoàn tồn kho
        if (input.Status == "Cancelled")
        {
            ModelState.AddModelError(
                nameof(Order.Status),
                "Vui lòng sử dụng chức năng hủy đơn hàng.");

            return View(input);
        }

        // Chỉ cập nhật những trường được phép
        existingOrder.UserId = input.UserId;
        existingOrder.Status = input.Status;
        existingOrder.ShippingAddress =
            input.ShippingAddress.Trim();

        // Không sửa:
        // existingOrder.OrderDate
        // existingOrder.TotalAmount

        try
        {
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật đơn hàng thành công!";

            return RedirectToAction(
                nameof(Details),
                new { orderid });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrderExists(input.OrderId))
                return NotFound();

            ModelState.AddModelError(
                "",
                "Đơn hàng đã bị thay đổi hoặc xóa.");
        }
        catch (DbUpdateException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "LỖI SỬA ĐƠN HÀNG: " +
                (ex.InnerException?.Message ?? ex.Message));

            ModelState.AddModelError(
                "",
                "Không thể cập nhật đơn hàng.");
        }

        return View(input);
    }

    // ======================================
    // GET: Orders/Delete
    // ======================================
    public async Task<IActionResult> Delete(int? orderid)
    {
        if (orderid == null)
            return NotFound();

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderid);

        if (order == null)
            return NotFound();

        return View(order);
    }

    // ======================================
    // POST: Orders/Delete
    // ======================================
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? orderid)
    {
        if (orderid == null)
            return NotFound();

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderid);

            if (order == null)
                return NotFound();

            // Không xóa đơn đã giao
            if (order.Status == "Delivered")
            {
                TempData["Error"] =
                    "Không thể xóa đơn hàng đã giao.";

                return RedirectToAction(nameof(Index));
            }

            // Nếu đơn chưa hủy thì hoàn lại tồn kho
            if (order.Status != "Cancelled")
            {
                var productIds = order.OrderDetails
                    .Where(d => d.ProductId.HasValue)
                    .Select(d => d.ProductId!.Value)
                    .Distinct()
                    .ToList();

                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                foreach (var detail in order.OrderDetails)
                {
                    if (!detail.ProductId.HasValue ||
                        !products.TryGetValue(
                            detail.ProductId.Value,
                            out var product))
                    {
                        throw new InvalidOperationException(
                            "Không tìm thấy sản phẩm để hoàn tồn kho.");
                    }

                    product.StockQuantity = checked(
                        product.StockQuantity + detail.Quantity);
                }
            }

            // Xóa chi tiết trước để tránh lỗi khóa ngoại
            _context.OrderDetails.RemoveRange(order.OrderDetails);

            // Xóa đơn hàng
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                "Xóa đơn hàng thành công!";
        }
        catch (Exception ex) when (
            ex is DbUpdateException ||
            ex is InvalidOperationException ||
            ex is OverflowException)
        {
            await transaction.RollbackAsync();

            System.Diagnostics.Debug.WriteLine(
                "LỖI XÓA ĐƠN HÀNG: " + ex.Message);

            TempData["Error"] =
                "Không thể xóa đơn hàng. Vui lòng kiểm tra dữ liệu.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ======================================
    // HELPER: Nạp dữ liệu cho Create
    // ======================================
    private async Task LoadCreateData()
    {
        ViewBag.Users = await _context.Users
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Products = await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.ProductName)
            .ToListAsync();
    }

    // ======================================
    // HELPER: Kiểm tra đơn hàng tồn tại
    // ======================================
    private bool OrderExists(int? orderid)
    {
        return _context.Orders
            .Any(o => o.OrderId == orderid);
    }
}
