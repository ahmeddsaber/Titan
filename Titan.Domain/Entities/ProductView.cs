using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Domain.Entities;
public class ProductView : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int ViewCount { get; set; } = 1;
    public DateTime LastViewedAt { get; set; } = DateTime.UtcNow;
}
