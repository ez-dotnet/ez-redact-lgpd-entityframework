using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Core.Taxonomies;
using Microsoft.Extensions.Compliance.Classification;

namespace EZ.Redact.Lgpd.EntityFramework;

internal sealed class RedactableProperty
{
    public DadoPessoal DadoPessoal { get; }
    public Func<object, object?> Getter { get; }
    public Action<object, object?> Setter { get; }

    public RedactableProperty(DadoPessoal dadoPessoal, Func<object, object?> getter, Action<object, object?> setter)
    {
        DadoPessoal = dadoPessoal;
        Getter = getter;
        Setter = setter;
    }
}

internal static class RedactablePropertyCache
{
    private static readonly ConcurrentDictionary<Type, RedactableProperty[]> _cache = new();

    public static RedactableProperty[] GetRedactableProperties(Type type)
    {
        return _cache.GetOrAdd(type, static t =>
        {
            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<DataClassificationAttribute>()
                })
                .Where(x => x.Attribute is not null && x.Attribute.Classification.TaxonomyName == "LGPD")
                .Select(x =>
                {
                    var dadoPessoal = LGPDTaxonomy.ToDadoPessoal(x.Attribute!.Classification);
                    return new RedactableProperty(dadoPessoal, CreateGetter(x.Property), CreateSetter(x.Property));
                })
                .ToArray();
        });
    }

    private static Func<object, object?> CreateGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var expr = Expression.Lambda<Func<object, object?>>(
            Expression.Convert(
                Expression.Property(
                    Expression.Convert(instance, property.DeclaringType!),
                    property),
                typeof(object)),
            instance);
        return expr.Compile();
    }

    private static Action<object, object?> CreateSetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var expr = Expression.Lambda<Action<object, object?>>(
            Expression.Assign(
                Expression.Property(
                    Expression.Convert(instance, property.DeclaringType!),
                    property),
                Expression.Convert(value, property.PropertyType)),
            instance,
            value);
        return expr.Compile();
    }
}
