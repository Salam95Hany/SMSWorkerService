using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Data
{
    public static class SeedData
    {
        public static async Task Initialize(AgentDbContext context)
        {
            if (!await context.DeviceSyncVersions.AnyAsync())
            {
                context.DeviceSyncVersions.Add(new DeviceSyncVersion
                {
                    CurrentVersion = 0,
                    UpdatedAt = DateTime.Now,
                });

                await context.SaveChangesAsync();
            }
        }
    }
}
