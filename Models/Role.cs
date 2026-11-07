using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLSupermarket.Models;

public partial class Role
{
    public int RoleId { get; set; }

    [Display(Name = "Tên quyền")]
    public string RoleName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}