using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QLSupermarket.Models;

public partial class QlsupermarketDbContext : DbContext
{
    public QlsupermarketDbContext()
    {
    }

    public QlsupermarketDbContext(DbContextOptions<QlsupermarketDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }
}
