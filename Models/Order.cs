using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLSupermarket.Models;

public partial class Order
{
    public int OrderId { get; set; }

    [Display(Name = "Mã khách hàng")]
    public int? UserId { get; set; }

    [Display(Name = "Ngày đặt hàng")]
    public DateTime? OrderDate { get; set; }

    [Display(Name = "Tổng tiền")]
    public decimal TotalAmount { get; set; }

    [Display(Name = "Trạng thái đơn hàng")]
    public string? Status { get; set; }

    [Display(Name = "Địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    [Display(Name = "Khách hàng")]
    public virtual User? User { get; set; }
}