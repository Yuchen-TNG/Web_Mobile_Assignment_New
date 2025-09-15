using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Web_Mobile_Assignment_New.Models
{
    public class DB : DbContext
    {
        public DB(DbContextOptions options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<House> Houses { get; set; }
        public DbSet<HouseReview> HouseReviews { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<HouseImage> HouseImages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // HouseReview → User (外键是 Email)
            modelBuilder.Entity<HouseReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserEmail)
                .HasPrincipalKey(u => u.Email);

            // HouseReview → House (外键是 HouseId)
            modelBuilder.Entity<HouseReview>()
                .HasOne(r => r.House)
                .WithMany(h => h.Reviews)
                .HasForeignKey(r => r.HouseId);
        }
    }

#nullable disable warnings

    public class User
    {
        [Key, MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Hash { get; set; }

        public DateOnly Birthday { get; set; }

        public string Role => GetType().Name;
    }

    public class OwnerTenant : User
    {
        [MaxLength(100)]
        public string PhotoURL { get; set; }

        [MaxLength(100)]
        public string Status { get; set; }
    }

    public class Tenant : OwnerTenant { }
    public class Owner : OwnerTenant { }
    public class Admin : User { }
}