using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Web_Mobile_Assignment_New.Models;


public class DB : DbContext
{
    public DB(DbContextOptions<DB> options) : base(options) { }

    public DbSet<House> Houses { get; set; }
}
