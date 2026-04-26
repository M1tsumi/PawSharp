#nullable enable
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace PawSharp.Commands.Execution;

/// <summary>
/// Factory for creating compiled delegates for command methods.
/// This provides better performance than reflection-based Method.Invoke.
/// </summary>
public static class CommandDelegateFactory
{
    /// <summary>
    /// Creates a compiled delegate for a command method.
    /// </summary>
    /// <param name="method">The method to compile.</param>
    /// <returns>A compiled delegate that can invoke the method.</returns>
    public static Func<BaseCommandModule, object?[], Task> CreateDelegate(MethodInfo method)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        
        var moduleParam = Expression.Parameter(typeof(BaseCommandModule), "module");
        var argsParam = Expression.Parameter(typeof(object?[]), "args");
        
        var parameters = method.GetParameters();
        var arguments = new Expression[parameters.Length];
        
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var argAccess = Expression.ArrayAccess(argsParam, Expression.Constant(i));
            
            // Cast the argument to the correct type
            arguments[i] = Expression.Convert(argAccess, paramType);
        }
        
        // Call the method
        var methodCall = Expression.Call(moduleParam, method, arguments);
        
        // Convert the result to Task if it's not already
        Expression resultExpression;
        if (method.ReturnType == typeof(Task))
        {
            resultExpression = methodCall;
        }
        else if (method.ReturnType == typeof(void))
        {
            // Convert void to completed Task
            resultExpression = Expression.Block(
                methodCall,
                Expression.Call(typeof(Task).GetMethod(nameof(Task.CompletedTask))!));
        }
        else
        {
            // Convert other return types to Task using Task.FromResult
            var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))?.MakeGenericMethod(method.ReturnType);
            if (fromResultMethod != null)
            {
                resultExpression = Expression.Call(fromResultMethod, methodCall);
            }
            else
            {
                resultExpression = Expression.Call(typeof(Task).GetMethod(nameof(Task.CompletedTask))!);
            }
        }
        
        var lambda = Expression.Lambda<Func<BaseCommandModule, object?[], Task>>(
            resultExpression,
            moduleParam,
            argsParam);
        
        return lambda.Compile();
    }
}
