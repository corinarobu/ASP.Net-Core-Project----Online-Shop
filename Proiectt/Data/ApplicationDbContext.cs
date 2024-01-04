using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proiect.Models;

namespace Proiect.Data
{

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>
        options)
        : base(options)
        {
        }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<ProductOrder> ProductOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // definire primary key compus
            modelBuilder.Entity<ProductOrder>()
            .HasKey(ab => new {
                ab.Id,
                ab.ProductId,
                ab.OrderId
            });

            
            modelBuilder.Entity<ProductOrder>()
            .HasOne(ab => ab.Product)
            .WithMany(ab => ab.ProductOrders)
            .HasForeignKey(ab => ab.ProductId);

            modelBuilder.Entity<ProductOrder>()
            .HasOne(ab => ab.Order)
            .WithMany(ab => ab.ProductOrders)
            .HasForeignKey(ab => ab.OrderId);
        }

    }

}
