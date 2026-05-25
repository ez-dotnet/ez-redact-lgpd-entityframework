using Microsoft.EntityFrameworkCore;

namespace EZ.Redact.Lgpd.EntityFramework;

public class EntityFrameworkRedactionOptions
{
    private readonly List<Type> _dbContextTypes = [];

    /// <summary>
    /// Tipos dos DbContexts que terão o <see cref="LgpdRedactionInterceptor"/> aplicado automaticamente.
    /// </summary>
    public IReadOnlyList<Type> DbContextTypes => _dbContextTypes;

    /// <summary>
    /// Adiciona um DbContext que terá redação LGPD aplicada nas queries.
    /// </summary>
    /// <typeparam name="TContext">Tipo do DbContext.</typeparam>
    public void UseDbContext<TContext>()
        where TContext : DbContext
    {
        _dbContextTypes.Add(typeof(TContext));
    }

    /// <summary>
    /// Adiciona dois DbContexts que terão redação LGPD aplicada nas queries.
    /// </summary>
    public void UseDbContexts<T1, T2>()
        where T1 : DbContext
        where T2 : DbContext
    {
        _dbContextTypes.AddRange([typeof(T1), typeof(T2)]);
    }

    /// <summary>
    /// Adiciona três DbContexts que terão redação LGPD aplicada nas queries.
    /// </summary>
    public void UseDbContexts<T1, T2, T3>()
        where T1 : DbContext
        where T2 : DbContext
        where T3 : DbContext
    {
        _dbContextTypes.AddRange([typeof(T1), typeof(T2), typeof(T3)]);
    }

    /// <summary>
    /// Adiciona DbContexts que terão redação LGPD aplicada nas queries.
    /// Os tipos precisam herdar de <see cref="DbContext"/>.
    /// </summary>
    /// <param name="dbContextTypes">Tipos dos DbContexts.</param>
    /// <exception cref="ArgumentException">Lançado se algum tipo não herdar de <see cref="DbContext"/>.</exception>
    public void UseDbContexts(params Type[] dbContextTypes)
    {
        ArgumentNullException.ThrowIfNull(dbContextTypes);

        foreach (var type in dbContextTypes)
        {
            if (!typeof(DbContext).IsAssignableFrom(type))
                throw new ArgumentException(
                    $"Type '{type.FullName}' must inherit from '{nameof(DbContext)}'.",
                    nameof(dbContextTypes));
        }

        _dbContextTypes.AddRange(dbContextTypes);
    }
}
