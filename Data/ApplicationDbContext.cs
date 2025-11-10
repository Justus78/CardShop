using api.Models;
using CardShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CardShop.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IConfiguration _config;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration config) : base(options)
        {
            _config = config;
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Set> Sets { get; set; }
        public DbSet<StoreCredit> StoreCredits { get; set; }
        public DbSet<StoreCreditTransaction> StoreCreditTransactions { get; set; }
        public DbSet<TradeIn> TradeIns { get; set; }
        public DbSet<TradeInItem> TradeInItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed roles
            List<IdentityRole> roles = new List<IdentityRole>
            {
                new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Name = "User", NormalizedName = "USER" }
            };
            modelBuilder.Entity<IdentityRole>().HasData(roles);

            // Seed Admin User
            var hasher = new PasswordHasher<ApplicationUser>();
            var adminUser = new ApplicationUser
            {
                Id = "admin-user-110022554411",
                UserName = _config["AdminUser:Username"],
                NormalizedUserName = _config["AdminUser:Username"].ToUpper(),
                Email = "Stars787878@aol.com",
                NormalizedEmail = "STARS787878@AOL.COM",
                EmailConfirmed = true
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, _config["AdminUser:Password"]);
            modelBuilder.Entity<ApplicationUser>().HasData(adminUser);

            // Relationships
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.CartItems)
                .WithOne(ci => ci.User)
                .HasForeignKey(ci => ci.UserId);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.TradeIns)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);

            // One-to-one: User ↔ StoreCredit
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.StoreCredit)
                .WithOne(sc => sc.User)
                .HasForeignKey<StoreCredit>(sc => sc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: StoreCredit ↔ StoreCreditTransactions
            modelBuilder.Entity<StoreCredit>()
                .HasMany(sc => sc.Transactions)
                .WithOne(t => t.StoreCredit)
                .HasForeignKey(t => t.StoreCreditId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: TradeIn ↔ TradeInItems
            modelBuilder.Entity<TradeIn>()
                .HasMany(ti => ti.TradeInItems)
                .WithOne(i => i.TradeIn)
                .HasForeignKey(i => i.TradeInId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product relationships
            modelBuilder.Entity<Product>()
                .HasMany(p => p.CartItems)
                .WithOne(ci => ci.Product)
                .HasForeignKey(ci => ci.ProductId);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId);

            // Order relationships
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId);

            // Unique constraints
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.UserId, ci.ProductId })
                .IsUnique();

            // Decimal precision
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TradeInItem>()
                .Property(ti => ti.EstimatedUnitValue)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<StoreCredit>()
                .Property(sc => sc.CurrentBalance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<StoreCreditTransaction>()
                .Property(t => t.ChangeAmount)
                .HasColumnType("decimal(18,2)");
        }
    }
}
