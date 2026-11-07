using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLSupermarket.Models;

public partial class Product
{
    public int ProductId { get; set; }

    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = null!;

    [Display(Name = "Danh mục")]
    public int? CategoryId { get; set; }

    [Display(Name = "Giá bán")]
    public decimal Price { get; set; }

    [Display(Name = "Tồn kho")]
    public int StockQuantity { get; set; }

    [Display(Name = "Hình ảnh")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Mô tả sản phẩm")]
    public string? Description { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "Danh mục")]
    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}