using System.Reflection;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.EntityFramework.Tests.Models;

namespace EZ.Redact.Lgpd.EntityFramework.Tests;

public class RedactablePropertyCacheTests
{
    [Fact]
    public void GetRedactableProperties_Should_Find_Properties_With_LGPD_Attributes()
    {
        var properties = RedactablePropertyCache.GetRedactableProperties(typeof(TestEntity));

        Assert.NotNull(properties);
        Assert.Equal(3, properties.Length);

        var documento = properties.Single(p => p.DadoPessoal == DadoPessoal.CPF);
        Assert.NotNull(documento.Getter);
        Assert.NotNull(documento.Setter);

        var email = properties.Single(p => p.DadoPessoal == DadoPessoal.Email);
        Assert.NotNull(email.Getter);
        Assert.NotNull(email.Setter);

        var nome = properties.Single(p => p.DadoPessoal == DadoPessoal.Nome);
        Assert.NotNull(nome.Getter);
        Assert.NotNull(nome.Setter);
    }

    [Fact]
    public void GetRedactableProperties_Should_Create_Getter_And_Setter_That_Work()
    {
        var properties = RedactablePropertyCache.GetRedactableProperties(typeof(TestEntity));

        var entity = new TestEntity
        {
            Documento = "123.456.789-09",
            Email = "teste@email.com",
            Nome = "João Silva"
        };

        Assert.Equal("123.456.789-09", properties.Single(p => p.DadoPessoal == DadoPessoal.CPF).Getter(entity));
        Assert.Equal("teste@email.com", properties.Single(p => p.DadoPessoal == DadoPessoal.Email).Getter(entity));
        Assert.Equal("João Silva", properties.Single(p => p.DadoPessoal == DadoPessoal.Nome).Getter(entity));

        var nomeProp = properties.Single(p => p.DadoPessoal == DadoPessoal.Nome);
        nomeProp.Setter(entity, "Maria Souza");
        Assert.Equal("Maria Souza", entity.Nome);
    }

    [Fact]
    public void GetRedactableProperties_Should_Detect_Correct_DadoPessoal_Types()
    {
        var properties = RedactablePropertyCache.GetRedactableProperties(typeof(TestEntity));

        var tipos = properties.Select(p => p.DadoPessoal).ToHashSet();
        Assert.Contains(DadoPessoal.CPF, tipos);
        Assert.Contains(DadoPessoal.Email, tipos);
        Assert.Contains(DadoPessoal.Nome, tipos);
        Assert.DoesNotContain(DadoPessoal.Telefone, tipos);
    }

    [Fact]
    public void GetRedactableProperties_Should_Return_Empty_For_Type_With_No_Attributes()
    {
        var properties = RedactablePropertyCache.GetRedactableProperties(typeof(EntitySemAtributos));

        Assert.NotNull(properties);
        Assert.Empty(properties);
    }

    [Fact]
    public void GetRedactableProperties_Should_Return_Same_Cached_Instance_On_Second_Call()
    {
        var first = RedactablePropertyCache.GetRedactableProperties(typeof(TestEntity));
        var second = RedactablePropertyCache.GetRedactableProperties(typeof(TestEntity));

        Assert.Same(first, second);
    }
}
