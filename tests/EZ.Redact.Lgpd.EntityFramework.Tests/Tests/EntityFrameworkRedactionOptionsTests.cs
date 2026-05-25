using EZ.Redact.Lgpd.EntityFramework.Tests.Models;
using Microsoft.EntityFrameworkCore;

namespace EZ.Redact.Lgpd.EntityFramework.Tests;

public class EntityFrameworkRedactionOptionsTests
{
    [Fact]
    public void UseDbContext_Should_Add_Type_To_DbContextTypes()
    {
        var options = new EntityFrameworkRedactionOptions();
        options.UseDbContext<TestDbContext>();

        Assert.Single(options.DbContextTypes);
        Assert.Equal(typeof(TestDbContext), options.DbContextTypes[0]);
    }

    [Fact]
    public void UseDbContexts_TwoTypes_Should_Add_Both()
    {
        var options = new EntityFrameworkRedactionOptions();
        options.UseDbContexts<TestDbContext, OtherDbContext>();

        Assert.Equal(2, options.DbContextTypes.Count);
        Assert.Contains(typeof(TestDbContext), options.DbContextTypes);
        Assert.Contains(typeof(OtherDbContext), options.DbContextTypes);
    }

    [Fact]
    public void UseDbContexts_ThreeTypes_Should_Add_All()
    {
        var options = new EntityFrameworkRedactionOptions();
        options.UseDbContexts<TestDbContext, OtherDbContext, TestDbContext>();

        Assert.Equal(3, options.DbContextTypes.Count);
    }

    [Fact]
    public void UseDbContexts_Params_Should_Add_Types()
    {
        var options = new EntityFrameworkRedactionOptions();
        options.UseDbContexts(typeof(TestDbContext), typeof(OtherDbContext));

        Assert.Equal(2, options.DbContextTypes.Count);
    }

    [Fact]
    public void UseDbContexts_Params_Should_Throw_For_Non_DbContext_Type()
    {
        var options = new EntityFrameworkRedactionOptions();

        var ex = Assert.Throws<ArgumentException>(() =>
            options.UseDbContexts(typeof(TestDbContext), typeof(NonDbContext)));

        Assert.Contains("NonDbContext", ex.Message);
        Assert.Contains("DbContext", ex.Message);
    }

    [Fact]
    public void UseDbContexts_Params_Should_Throw_ArgumentNullException_When_Null()
    {
        var options = new EntityFrameworkRedactionOptions();

        Assert.Throws<ArgumentNullException>(() => options.UseDbContexts(null!));
    }

    [Fact]
    public void DbContextTypes_Should_Be_Empty_By_Default()
    {
        var options = new EntityFrameworkRedactionOptions();

        Assert.Empty(options.DbContextTypes);
    }
}
