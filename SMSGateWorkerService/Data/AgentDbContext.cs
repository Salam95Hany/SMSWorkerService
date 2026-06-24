using Microsoft.EntityFrameworkCore;

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
