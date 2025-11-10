using api.Models;
using Microsoft.AspNetCore.Identity;

namespace CardShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Navigation Properties //
        public ICollection<Order>? Orders { get; set; }
        public ICollection<CartItem>? CartItems { get; set; }
        public ICollection<TradeIn> TradeIns { get; set; } = [];

        // Navigation Property for Store Credit //
        public StoreCredit? StoreCredit { get; set; }

    }
}
