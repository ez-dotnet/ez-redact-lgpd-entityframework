using EZ.Redact.Lgpd.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EZ.Redact.Lgpd.EntityFramework;

public static class EntityFrameworkRedactionExtensions
{
    /// <summary>
    /// Registra o suporte a redação LGPD no Entity Framework Core.
    /// Habilita o uso de <c>UseRedaction()</c> em queries e configura automaticamente
    /// o <see cref="LgpdRedactionInterceptor"/> nos DbContexts especificados.
    /// </summary>
    /// <param name="builder">O builder de redação LGPD.</param>
    /// <param name="configure">Ação para configurar quais DbContexts terão redação aplicada.</param>
    /// <returns>O mesmo builder para encadeamento.</returns>
    public static ILGPDRedactionBuilder AddEntityFrameworkRedaction(
        this ILGPDRedactionBuilder builder,
        Action<EntityFrameworkRedactionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EntityFrameworkRedactionOptions();
        configure(options);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<LgpdRedactionInterceptor>();

        foreach (var ctxType in options.DbContextTypes)
        {
            var configureMethod = typeof(EntityFrameworkRedactionExtensions)
                .GetMethod(nameof(ConfigureInterceptorFor), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(ctxType);
            configureMethod.Invoke(null, [builder.Services]);
        }

        return builder;
    }

    private static void ConfigureInterceptorFor<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<DbContextOptionsBuilder<TContext>>,
            LgpdRedactionInterceptorConfigurer<TContext>>());
    }
}

internal sealed class LgpdRedactionInterceptorConfigurer<TContext> : IConfigureOptions<DbContextOptionsBuilder<TContext>>
    where TContext : DbContext
{
    private readonly LgpdRedactionInterceptor _interceptor;

    public LgpdRedactionInterceptorConfigurer(LgpdRedactionInterceptor interceptor)
    {
        _interceptor = interceptor;
    }

    public void Configure(DbContextOptionsBuilder<TContext> options)
    {
        options.AddInterceptors(_interceptor);
    }
}
