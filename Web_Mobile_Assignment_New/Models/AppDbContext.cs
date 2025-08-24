using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Web_Mobile_Assignment_New.Models;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<DB> options) : base(options) { }

    public DbSet<House> Houses { get; set; }
}
