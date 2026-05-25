using EZ.Redact.Lgpd.EntityFramework;
using EZ.Redact.Lgpd.EntityFramework.Sample.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("AppDb"));

builder.Services.AddDbContext<PublicDbContext>(options =>
    options.UseInMemoryDatabase("PublicDb"));

builder.Services.AddLGPDRedaction()
    .AddEntityFrameworkRedaction(options =>
    {
        options.UseDbContext<AppDbContext>();
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    ctx.Database.EnsureCreated();

    var pub = scope.ServiceProvider.GetRequiredService<PublicDbContext>();
    pub.Database.EnsureCreated();
}

app.MapGet("/clientes/redacted", async (AppDbContext db) =>
    await db.Clientes.UseRedaction().ToListAsync());

app.MapGet("/clientes/raw", async (AppDbContext db) =>
    await db.Clientes.ToListAsync());

app.MapGet("/publico/clientes", async (PublicDbContext db) =>
    await db.Clientes.ToListAsync());

app.Run();
