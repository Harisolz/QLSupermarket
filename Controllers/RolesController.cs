
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSupermarket.Models;

public class RolesController : Controller
{
    private readonly QlsupermarketDbContext _context;

    public RolesController(QlsupermarketDbContext context)
    {
        _context = context;
    }

    // GET: ROLES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Roles.ToListAsync());
    }

    // GET: ROLES/Details/5
    public async Task<IActionResult> Details(int? roleid)
    {
        if (roleid == null)
        {
            return NotFound();
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(m => m.RoleId == roleid);
        if (role == null)
        {
            return NotFound();
        }

        return View(role);
    }

    // GET: ROLES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ROLES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("RoleId,RoleName,Users")] Role role)
    {
        if (ModelState.IsValid)
        {
            _context.Add(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(role);
    }

    // GET: ROLES/Edit/5
    public async Task<IActionResult> Edit(int? roleid)
    {
        if (roleid == null)
        {
            return NotFound();
        }

        var role = await _context.Roles.FindAsync(roleid);
        if (role == null)
        {
            return NotFound();
        }
        return View(role);
    }

    // POST: ROLES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? roleid, [Bind("RoleId,RoleName,Users")] Role role)
    {
        if (roleid != role.RoleId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(role);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(role.RoleId))
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
        return View(role);
    }

    // GET: ROLES/Delete/5
    public async Task<IActionResult> Delete(int? roleid)
    {
        if (roleid == null)
        {
            return NotFound();
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(m => m.RoleId == roleid);
        if (role == null)
        {
            return NotFound();
        }

        return View(role);
    }

    // POST: ROLES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? roleid)
    {
        var role = await _context.Roles.FindAsync(roleid);
        if (role != null)
        {
            _context.Roles.Remove(role);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool RoleExists(int? roleid)
    {
        return _context.Roles.Any(e => e.RoleId == roleid);
    }
}
