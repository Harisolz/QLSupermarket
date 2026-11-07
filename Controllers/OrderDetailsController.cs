
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSupermarket.Models;

namespace QLSupermarket.Controllers;

public class OrderDetailsController : Controller
{
    private readonly QlsupermarketDbContext _context;

    public OrderDetailsController(QlsupermarketDbContext context)
    {
        _context = context;
    }

    // ======================================
    // GET: OrderDetails
    // Danh sách tất cả chi tiết đơn hàng
    // ======================================
    public async Task<IActionResult> Index()
    {
        var orderDetails = await _context.OrderDetails
            .AsNoTracking()
            .Include(od => od.Order)
            .Include(od => od.Product)
            .OrderByDescending(od => od.OrderDetailId)
            .ToListAsync();

        return View(orderDetails);
    }

    // ======================================
    // GET: OrderDetails/Details/5
    // Xem một chi tiết đơn hàng
    // ======================================
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var orderDetail = await _context.OrderDetails
            .AsNoTracking()
            .Include(od => od.Order)
            .Include(od => od.Product)
            .FirstOrDefaultAsync(
                od => od.OrderDetailId == id.Value);

        if (orderDetail == null)
            return NotFound();

        return View(orderDetail);
    }

    // ======================================
    // GET: OrderDetails/Create
    // Chuyển sang tạo đơn hàng
    // ======================================
    [HttpGet]
    public IActionResult Create()
    {
        return RedirectToAction(
            "Create",
            "Orders");
    }

    // ======================================
    // GET: OrderDetails/Edit/5
    // Không sửa riêng chi tiết đơn hàng
    // ======================================
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var orderDetail = await _context.OrderDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(
                od => od.OrderDetailId == id.Value);

        if (orderDetail == null)
            return NotFound();

        if (orderDetail.OrderId == null)
            return RedirectToAction(nameof(Index));

        return RedirectToAction(
            "Details",
            "Orders",
            new { orderid = orderDetail.OrderId });
    }

    // ======================================
    // GET: OrderDetails/Delete/5
    // Không xóa riêng chi tiết đơn hàng
    // ======================================
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var orderDetail = await _context.OrderDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(
                od => od.OrderDetailId == id.Value);

        if (orderDetail == null)
            return NotFound();

        if (orderDetail.OrderId == null)
            return RedirectToAction(nameof(Index));

        return RedirectToAction(
            "Details",
            "Orders",
            new { orderid = orderDetail.OrderId });
    }
}
