using System.Text.Json;

namespace Prospect.Server.Api.Services.CloudScript;

public class CloudScriptService
{
    private readonly ILogger<CloudScriptService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CloudScriptFunctionLoader _functionLoader;

    public CloudScriptService(ILogger<CloudScriptService> logger, IServiceScopeFactory serviceScopeFactory, CloudScriptFunctionLoader functionLoader)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _functionLoader = functionLoader;
    }

    public async Task<object?> ExecuteAsync(string name, string? parameter, bool generatePlayStreamEvent)
    {
        if (!_functionLoader.TryGetFunction(name, out var function))
        {
            _logger.LogWarning("Function {Function} is missing", name);
            return null;
        }

        if (parameter == null)
        {
            _logger.LogWarning("Function {Function} was called without a parameter", name);
            return null;
        }

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var functionClazz = ActivatorUtilities.CreateInstance(scope.ServiceProvider, function.Clazz);
            var functionParam = JsonSerializer.Deserialize(parameter, function.RequestType);
            if (functionParam == null)
            {
                _logger.LogWarning("Function {Function} deserialization returned null", name);
                return null;
            }

            var functionTask = function.Delegate(functionClazz, functionParam);
            
            await functionTask;
            
            return (object)((dynamic)functionTask).Result;
        }
    }
}