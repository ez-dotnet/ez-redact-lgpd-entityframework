using System.Data;
using System.Data.Common;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.EntityFramework.Tests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EZ.Redact.Lgpd.EntityFramework.Tests;

public class LgpdRedactionInterceptorTests
{
    private static ILGPDRedactService CreateRedactService()
    {
        var services = new ServiceCollection();
        services.AddLGPDRedaction();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<ILGPDRedactService>();
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Service_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new LgpdRedactionInterceptor(null!));
    }

    [Fact]
    public void Constructor_Should_Create_Instance()
    {
        var service = CreateRedactService();
        var interceptor = new LgpdRedactionInterceptor(service);

        Assert.NotNull(interceptor);
    }

    [Fact]
    public void InitializedInstance_WhenNotActive_Should_Return_Entity_Unchanged()
    {
        var service = CreateRedactService();
        var interceptor = new LgpdRedactionInterceptor(service);

        var entity = new TestEntity
        {
            Id = 1,
            Documento = "123.456.789-09",
            Email = "teste@email.com",
            SemAtributo = "público"
        };

        var result = interceptor.InitializedInstance(default, entity);

        Assert.Same(entity, result);
        Assert.Equal("123.456.789-09", entity.Documento);
        Assert.Equal("teste@email.com", entity.Email);
        Assert.Equal("público", entity.SemAtributo);
    }

    [Fact]
    public void InitializedInstance_WhenActive_Should_Redact_Properties()
    {
        var service = CreateRedactService();
        var interceptor = new LgpdRedactionInterceptor(service);

        var entity = new TestEntity
        {
            Id = 1,
            Documento = "123.456.789-09",
            Email = "joao@email.com",
            Nome = "João Silva",
            SemAtributo = "público"
        };

        using var cmd = new FakeDbCommand
        {
            CommandText = "SELECT \"t\".\"Id\", \"t\".\"Documento\", \"t\".\"Email\", \"t\".\"Nome\", \"t\".\"SemAtributo\"\r\nFROM \"TestEntities\" AS \"t\"\r\n-- lgpd-redact"
        };

        interceptor.ReaderExecuting(cmd, null!, default);

        interceptor.InitializedInstance(default, entity);

        Assert.Equal("123.***.***-09", entity.Documento);
        Assert.Equal("j***@email.com", entity.Email);
        Assert.Equal("J*** S****", entity.Nome);
        Assert.Equal("público", entity.SemAtributo);

        interceptor.DataReaderClosing(cmd, null!, default);
    }

    [Fact]
    public void InitializedInstance_WhenActive_Should_Handle_Null_Properties()
    {
        var service = CreateRedactService();
        var interceptor = new LgpdRedactionInterceptor(service);

        var entity = new TestEntity
        {
            Id = 42,
            Documento = null,
            Nome = null
        };

        using var cmd = new FakeDbCommand { CommandText = "-- lgpd-redact" };
        interceptor.ReaderExecuting(cmd, null!, default);

        var result = interceptor.InitializedInstance(default, entity);

        Assert.Equal(42, entity.Id);
        Assert.Null(entity.Documento);
        Assert.Null(entity.Nome);

        interceptor.DataReaderClosing(cmd, null!, default);
    }

    [Fact]
    public void ReaderExecuting_Without_Tag_Should_Not_Activate_Redaction()
    {
        var service = CreateRedactService();
        var interceptor = new LgpdRedactionInterceptor(service);

        var entity = new TestEntity
        {
            Documento = "123.456.789-09"
        };

        using var cmd = new FakeDbCommand { CommandText = "SELECT * FROM TestEntities" };
        interceptor.ReaderExecuting(cmd, null!, default);

        interceptor.InitializedInstance(default, entity);

        Assert.Equal("123.456.789-09", entity.Documento);

        interceptor.DataReaderClosing(cmd, null!, default);
    }

    [Fact]
    public void DataReaderClosing_Should_Reset_Redaction_Flag()
    {
        var service = CreateRedactService();
        var interceptor = new LgpdRedactionInterceptor(service);

        var entity = new TestEntity
        {
            Documento = "123.456.789-09"
        };

        using var cmd = new FakeDbCommand { CommandText = "-- lgpd-redact" };
        interceptor.ReaderExecuting(cmd, null!, default);
        interceptor.DataReaderClosing(cmd, null!, default);

        interceptor.InitializedInstance(default, entity);

        Assert.Equal("123.456.789-09", entity.Documento);
    }

    [Fact]
    public void Integration_WithInMemory_Should_Materialize_Without_Redaction()
    {
        var service = CreateRedactService();
        var interceptor = new LgpdRedactionInterceptor(service);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("TestDb_NoRedact_" + Guid.NewGuid())
            .Options;

        using var context = new TestDbContext(options);
        context.TestEntities.Add(new TestEntity
        {
            Documento = "123.456.789-09",
            Email = "teste@email.com",
            Nome = "João Silva",
            SemAtributo = "público"
        });
        context.SaveChanges();

        var entity = context.TestEntities.UseRedaction().FirstOrDefault();

        Assert.NotNull(entity);
        Assert.Equal("123.456.789-09", entity.Documento);
        Assert.Equal("teste@email.com", entity.Email);
        Assert.Equal("João Silva", entity.Nome);
        Assert.Equal("público", entity.SemAtributo);
    }

    private sealed class FakeDbCommand : DbCommand
    {
#pragma warning disable CS8765
        public override string CommandText { get; set; } = "";
#pragma warning restore CS8765
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get => null!; set { } }
        protected override DbParameterCollection DbParameterCollection => null!;
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => null;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => null!;
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => null!;
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => Task.FromResult<DbDataReader>(null!);
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) => Task.FromResult<object?>(null);
        public override Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
