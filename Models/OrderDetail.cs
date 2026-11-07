using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLSupermarket.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    [Display(Name = "Mã đơn hàng")]
    public int? OrderId { get; set; }

    [Display(Name = "Mã sản phẩm")]
    public int? ProductId { get; set; }

    [Display(Name = "Số lượng")]
    public int Quantity { get; set; }

    [Display(Name = "Đơn giá")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Đơn hàng")]
    public virtual Order? Order { get; set; }

    [Display(Name = "Sản phẩm")]
    public virtual Product? Product { get; set; }
}