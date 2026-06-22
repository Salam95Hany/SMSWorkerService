using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Data
{
    public class AgentDbContext : DbContext
    {
        public DbSet<SmsMessage> SmsMessages { get; set; }
        public DbSet<SmsGateDevice> SmsGateDevices { get; set; }
        public DbSet<DeviceSyncVersion> DeviceSyncVersions { get; set; }
        

        public AgentDbContext(DbContextOptions<AgentDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<SmsMessage>().HasIndex(x => x.SmsGateId).IsUnique();
        }
    }
}
