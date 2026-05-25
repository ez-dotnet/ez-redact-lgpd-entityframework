using Microsoft.EntityFrameworkCore;

namespace EZ.Redact.Lgpd.EntityFramework.Tests.Models;

public class TestDbContext : DbContext
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}

public class OtherDbContext : DbContext
{
    public DbSet<TestEntity> Items => Set<TestEntity>();

    public OtherDbContext(DbContextOptions<OtherDbContext> options) : base(options) { }
}

public class NonDbContext
{
}
