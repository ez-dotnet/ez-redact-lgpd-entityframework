using System.Data.Common;
using EZ.Redact.Lgpd.Core;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EZ.Redact.Lgpd.EntityFramework;

internal sealed class LgpdRedactionInterceptor : IDbCommandInterceptor, IMaterializationInterceptor
{
    private readonly ILGPDRedactService _redactService;
    private static readonly AsyncLocal<bool> _isRedactionActive = new();

    public LgpdRedactionInterceptor(ILGPDRedactService redactService)
    {
        _redactService = redactService ?? throw new ArgumentNullException(nameof(redactService));
    }

    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (!_isRedactionActive.Value)
            return entity;

        var properties = RedactablePropertyCache.GetRedactableProperties(entity.GetType());
        if (properties.Length == 0)
            return entity;

        foreach (var prop in properties)
        {
            var value = prop.Getter(entity);
            if (value is string str)
            {
                var redacted = _redactService.Redact(prop.DadoPessoal, str);
                if (redacted != str)
                    prop.Setter(entity, redacted);
            }
        }

        return entity;
    }

    public InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (command.CommandText.Contains("-- " + QueryableRedactionExtensions.LgpdRedactTag))
            _isRedactionActive.Value = true;

        return result;
    }

    public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("-- " + QueryableRedactionExtensions.LgpdRedactTag))
            _isRedactionActive.Value = true;

        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public InterceptionResult DataReaderClosing(
        DbCommand command,
        DataReaderClosingEventData eventData,
        InterceptionResult result)
    {
        _isRedactionActive.Value = false;
        return result;
    }

    public ValueTask<InterceptionResult> DataReaderClosingAsync(
        DbCommand command,
        DataReaderClosingEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        _isRedactionActive.Value = false;
        return new ValueTask<InterceptionResult>(result);
    }

    public InterceptionResult DataReaderDisposing(
        DbCommand command,
        DataReaderDisposingEventData eventData,
        InterceptionResult result)
    {
        _isRedactionActive.Value = false;
        return result;
    }

    public DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
        => result;

    public ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
        => new(result);

    public InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
        => result;

    public ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
        => new(result);

    public object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
        => result;

    public ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
        => new(result);

    public InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
        => result;

    public ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
        => new(result);

    public int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
        => result;

    public ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
        => new(result);

    public InterceptionResult CommandCreating(
        CommandCorrelatedEventData eventData,
        InterceptionResult result)
        => result;

    public DbCommand CommandCreated(
        CommandEndEventData eventData,
        DbCommand result)
        => result;

    public DbCommand CommandInitialized(
        CommandEndEventData eventData,
        DbCommand result)
        => result;

    public void CommandCanceled(
        DbCommand command,
        CommandEndEventData eventData) { }

    public Task CommandCanceledAsync(
        DbCommand command,
        CommandEndEventData eventData,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData) { }

    public Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    InterceptionResult<object> IMaterializationInterceptor.CreatingInstance(
        MaterializationInterceptionData materializationData,
        InterceptionResult<object> result)
        => result;

    object IMaterializationInterceptor.CreatedInstance(
        MaterializationInterceptionData materializationData,
        object entity)
        => entity;

    InterceptionResult IMaterializationInterceptor.InitializingInstance(
        MaterializationInterceptionData materializationData,
        object entity,
        InterceptionResult result)
        => result;
}
