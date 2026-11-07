using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; //  thư viện  [Display]

namespace QLSupermarket.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    [Display(Name = "Tên danh mục")]
    public string CategoryName { get; set; } = null!;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}