using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMIUserApp.Model
{
    public class ApplicationDBContext : DbContext
    {
        public DbSet<ModbusData> ModbusDatas { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=logdatas.db");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ModbusData>()
                .HasKey(b => b.Id)
                .HasName("PK_LogId");
        }
    }
}
