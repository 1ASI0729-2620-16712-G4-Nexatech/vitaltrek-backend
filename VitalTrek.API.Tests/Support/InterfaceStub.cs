using System.Reflection;

namespace VitalTrek.API.Tests.Support;

/// <summary>Small interface test double that fails on unexpected calls, without extra test dependencies.</summary>
public class InterfaceStub<T> : DispatchProxy where T : class
{
    private Func<MethodInfo, object?[], object?> _handler = null!;

    public static T Create(Func<MethodInfo, object?[], object?>? handler = null)
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub<T>>();
        ((InterfaceStub<T>)(object)proxy)._handler = handler ?? ((method, _) =>
            throw new InvalidOperationException($"Unexpected call to {typeof(T).Name}.{method.Name}"));
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        _handler(targetMethod!, args ?? []);
}
