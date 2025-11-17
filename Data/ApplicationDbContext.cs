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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration config)
            : base(options)
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

            // ---------------------------------------
            // Seed Roles with FIXED IDs
            // ---------------------------------------
            const string ADMIN_ROLE_ID = "role-admin-001";
            const string USER_ROLE_ID = "role-user-001";

            var adminRole = new IdentityRole
            {
                Id = ADMIN_ROLE_ID,
                Name = "Admin",
                NormalizedName = "ADMIN"
            };

            var userRole = new IdentityRole
            {
                Id = USER_ROLE_ID,
                Name = "User",
                NormalizedName = "USER"
            };

            modelBuilder.Entity<IdentityRole>().HasData(adminRole, userRole);

            // ---------------------------------------
            // Seed Admin User
            // ---------------------------------------
            const string ADMIN_USER_ID = "admin-user-110022554411";

            var hasher = new PasswordHasher<ApplicationUser>();
            var adminUser = new ApplicationUser
            {
                Id = ADMIN_USER_ID,
                UserName = _config["AdminUser:Username"],
                NormalizedUserName = _config["AdminUser:Username"].ToUpper(),
                Email = "Stars787878@aol.com",
                NormalizedEmail = "STARS787878@AOL.COM",
                EmailConfirmed = true
            };

            adminUser.PasswordHash = hasher.HashPassword(adminUser, _config["AdminUser:Password"]);

            modelBuilder.Entity<ApplicationUser>().HasData(adminUser);

            // ---------------------------------------
            // Seed Admin User → Admin Role Link
            // ---------------------------------------
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    UserId = ADMIN_USER_ID,
                    RoleId = ADMIN_ROLE_ID
                }
            );

            // ---------------------------------------
            // Relationships
            // ---------------------------------------
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

            // One-to-many: StoreCredit ↔ Transactions
            modelBuilder.Entity<StoreCredit>()
                .HasMany(sc => sc.Transactions)
                .WithOne(t => t.StoreCredit)
                .HasForeignKey(t => t.StoreCreditId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: TradeIn ↔ Items
            modelBuilder.Entity<TradeIn>()
                .HasMany(t => t.TradeInItems)
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

            // ---------------------------------------
            // Indexes & Constraints
            // ---------------------------------------
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.UserId, ci.ProductId })
                .IsUnique();

            // ---------------------------------------
            // Decimal precision
            // ---------------------------------------
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
