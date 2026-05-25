using Microsoft.EntityFrameworkCore;

namespace EZ.Redact.Lgpd.EntityFramework;

public static class QueryableRedactionExtensions
{
    internal const string LgpdRedactTag = "lgpd-redact";

    /// <summary>
    /// Sinaliza que a query deve ter os dados sensíveis redigidos na leitura.
    /// Internamente usa <c>TagWith("lgpd-redact")</c> para que o <see cref="LgpdRedactionInterceptor"/>
    /// identifique a query e aplique a redação nas propriedades marcadas com atributos de taxonomia LGPD.
    /// </summary>
    /// <typeparam name="T">Tipo da entidade retornada pela query.</typeparam>
    /// <param name="source">A query sobre a qual aplicar a redação.</param>
    /// <returns>A mesma query com a tag de redação adicionada.</returns>
    public static IQueryable<T> UseRedaction<T>(this IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.TagWith(LgpdRedactTag);
    }
}
