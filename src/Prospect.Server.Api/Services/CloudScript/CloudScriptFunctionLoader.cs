using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Prospect.Server.Api.Services.CloudScript;

public record CloudScriptFunctionData(Type Clazz, Type RequestType, bool ReturnsObject, Func<object, object, Task> Delegate);

public class CloudScriptFunctionLoader
{
    private readonly ILogger<CloudScriptFunctionLoader> _logger;
    private readonly Dictionary<string, CloudScriptFunctionData> _methods;
    
    public CloudScriptFunctionLoader(ILogger<CloudScriptFunctionLoader> logger)
    {
        _logger = logger;
        _methods = LoadMethods();
    }

    public bool TryGetFunction(string name, [MaybeNullWhen(false)] out CloudScriptFunctionData functionData)
    {
        return _methods.TryGetValue(name, out functionData);
    }

    private Dictionary<string, CloudScriptFunctionData> LoadMethods()
    {
        var result = new Dictionary<string, CloudScriptFunctionData>();

        var genericInterface = typeof(ICloudScriptFunction<,>);
        
        foreach (var clazz in typeof(CloudScriptFunctionLoader).Assembly.GetTypes())
        {
            var attribute = clazz.GetCustomAttribute<CloudScriptFunction>();
            if (attribute == null)
            {
                continue;
            }

            var clazzInterface = clazz.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterface);
            if (clazzInterface != null)
            {
                var genericTypes = clazzInterface.GetGenericArguments();
                var typeReq = genericTypes[0];
                var typeRes = genericTypes[1];
                
                var method = clazz.GetMethod("ExecuteAsync", new []{ typeReq });
                if (method == null)
                {
                    _logger.LogWarning("ExecuteAsync method not found for class {Class}", clazz.FullName);
                    continue;
                }

                // Expression.
                var argInstance = Expression.Parameter(typeof(object), "instance");
                var argRequest = Expression.Parameter(typeof(object), "request");

                var invokeReturn = Expression.Label(typeof(Task));
                
                var invokeArgs = new Expression[]
                {
                    Expression.Convert(argRequest, typeReq)
                };

                var invokeCall = Expression.Call(Expression.Convert(argInstance, clazz), method, invokeArgs);

                var invoke = Expression.Block(
                    Expression.Return(invokeReturn, invokeCall),
                    Expression.Label(invokeReturn, Expression.Default(typeof(Task)))
                );

                var lambda = Expression.Lambda<Func<object, object, Task>>(invoke, argInstance, argRequest).Compile();

                // Store lambda expression.
                result.Add(attribute.Name, new CloudScriptFunctionData(clazz, typeReq, true, lambda));
            }
        }

        return result;
    }
}