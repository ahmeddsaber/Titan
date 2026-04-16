using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Domain.Entities;
public class CartItem : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
