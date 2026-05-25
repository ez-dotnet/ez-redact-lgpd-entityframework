using EZ.Redact.Lgpd.EntityFramework.Tests.Models;
using Microsoft.EntityFrameworkCore;

namespace EZ.Redact.Lgpd.EntityFramework.Tests;

public class QueryableRedactionExtensionsTests
{
    [Fact]
    public void UseRedaction_Should_Return_Query()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("TestDb_UseRedaction")
            .Options;

        using var ctx = new TestDbContext(options);
        var query = ctx.TestEntities.UseRedaction();

        Assert.NotNull(query);
    }

    [Fact]
    public void UseRedaction_Should_Throw_ArgumentNullException_When_Source_Is_Null()
    {
        IQueryable<string>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.UseRedaction());
    }

    [Fact]
    public void UseRedaction_Should_Process_Query_And_Return_Data()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("TestDb_ReturnData_" + Guid.NewGuid())
            .Options;

        using var ctx = new TestDbContext(options);
        ctx.TestEntities.Add(new TestEntity { Documento = "123.456.789-09", SemAtributo = "teste" });
        ctx.SaveChanges();

        var result = ctx.TestEntities.UseRedaction().ToList();

        Assert.Single(result);
        Assert.Equal("123.456.789-09", result[0].Documento);
    }
}
