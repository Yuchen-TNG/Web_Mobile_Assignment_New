using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;


namespace Web_Mobile_Assignment_New.Models;

public class DB : DbContext
{
    public DB(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Owner> Owners { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<House> Houses { get; set; }


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

public class Photo : User
{
    [MaxLength(100)]
    public string PhotoURL { get; set; }
}

public class Tenant : Photo
{
 
}

public class Owner : Photo
{
   
}

public class Admin : User
{
    
}



