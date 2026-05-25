using EZ.Redact.Lgpd.Core.Attributes;

namespace EZ.Redact.Lgpd.EntityFramework.Tests.Models;

public class TestEntity
{
    public int Id { get; set; }

    [CPFData]
    public string? Documento { get; set; }

    [EmailData]
    public string? Email { get; set; }

    [NomeData]
    public string? Nome { get; set; }

    public string? SemAtributo { get; set; }
}

public class EntitySemAtributos
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
}
