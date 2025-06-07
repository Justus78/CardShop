using Microsoft.AspNetCore.Identity;

namespace CardShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Order>? Orders { get; set; }
        public ICollection<CartItem>? CartItems { get; set; }
    }
}
