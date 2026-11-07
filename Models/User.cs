using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLSupermarket.Models;

public partial class User
{
    public int UserId { get; set; }

    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = null!;

    [Display(Name = "Mật khẩu")]
    public string PasswordHash { get; set; } = null!;

    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Mã quyền")]
    public int? RoleId { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [Display(Name = "Quyền hạn")]
    public virtual Role? Role { get; set; }
}