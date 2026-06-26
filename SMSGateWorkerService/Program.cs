using Microsoft.EntityFrameworkCore;
using SMSGateWorkerService;
using SMSGateWorkerService.Data;
using SMSGateWorkerService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();
builder.Services.AddDbContext<AgentDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<SmsGateService>();
builder.Services.AddHttpClient<CloudService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["Cloud:BaseUrl"]!);
});
builder.Services.AddHttpClient<ConnectivityService>((sp, client) =>
{
    client.BaseAddress = new Uri(builder.Configuration["Cloud:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<InboxService>();
builder.Services.AddScoped<SendMessageService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    db.Database.EnsureCreated();
    await SeedData.Initialize(db);
}

host.Run();