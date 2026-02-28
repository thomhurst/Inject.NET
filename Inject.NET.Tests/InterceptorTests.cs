using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Services;

namespace Inject.NET.Tests;

/// <summary>
/// Tests for the interceptor/AOP infrastructure.
/// Verifies that DispatchProxy-based interceptors correctly wrap service method calls,
/// support chaining, and handle synchronous and asynchronous methods.
/// </summary>
public class InterceptorTests
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // PROXY CREATION TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Create_WithNoInterceptors_CallsTargetDirectly()
    {
        var target = new CalculatorService();
        var proxy = InterceptorProxy<ICalculator>.Create(target, Array.Empty<IInterceptor>());

        var result = proxy.Add(2, 3);

        await Assert.That(result).IsEqualTo(5);
    }

    [Test]
    public async Task Create_ReturnedProxy_ImplementsServiceInterface()
    {
        var target = new CalculatorService();
        var proxy = InterceptorProxy<ICalculator>.Create(target, Array.Empty<IInterceptor>());

        await Assert.That(proxy).IsAssignableTo<ICalculator>();
    }

    [Test]
    public async Task Create_WithNullTarget_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(InterceptorProxy<ICalculator>.Create(null!, [])));
    }

    [Test]
    public async Task Create_WithNullInterceptors_ThrowsArgumentNullException()
    {
        var target = new CalculatorService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(InterceptorProxy<ICalculator>.Create(target, null!)));
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SINGLE INTERCEPTOR TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task SingleInterceptor_IsCalledBeforeAndAfterTargetMethod()
    {
        var callLog = new List<string>();
        var target = new LoggingCalculator(callLog);
        var interceptor = new CallTrackingInterceptor(callLog);

        var proxy = InterceptorProxy<ICalculator>.Create(target, [interceptor]);
        var result = proxy.Add(10, 20);

        await Assert.That(result).IsEqualTo(30);
        await Assert.That(callLog.Count).IsEqualTo(3);
        await Assert.That(callLog[0]).IsEqualTo("Interceptor:Before");
        await Assert.That(callLog[1]).IsEqualTo("Target:Add");
        await Assert.That(callLog[2]).IsEqualTo("Interceptor:After");
    }

    [Test]
    public async Task SingleInterceptor_ReceivesCorrectMethodInfo()
    {
        MethodInfoCapturingInterceptor interceptor = new();
        var target = new CalculatorService();

        var proxy = InterceptorProxy<ICalculator>.Create(target, [interceptor]);
        proxy.Add(1, 2);

        await Assert.That(interceptor.CapturedMethodName).IsEqualTo("Add");
    }

    [Test]
    public async Task SingleInterceptor_ReceivesCorrectArguments()
    {
        var interceptor = new ArgumentCapturingInterceptor();
        var target = new CalculatorService();

        var proxy = InterceptorProxy<ICalculator>.Create(target, [interceptor]);
        proxy.Add(42, 58);

        await Assert.That(interceptor.CapturedArguments).IsNotNull();
        await Assert.That(interceptor.CapturedArguments!.Length).IsEqualTo(2);
        await Assert.That((int)interceptor.CapturedArguments[0]!).IsEqualTo(42);
        await Assert.That((int)interceptor.CapturedArguments[1]!).IsEqualTo(58);
    }

    [Test]
    public async Task SingleInterceptor_ReceivesCorrectTarget()
    {
        var interceptor = new TargetCapturingInterceptor();
        var target = new CalculatorService();

        var proxy = InterceptorProxy<ICalculator>.Create(target, [interceptor]);
        proxy.Add(1, 1);

        await Assert.That(interceptor.CapturedTarget).IsSameReferenceAs(target);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // INTERCEPTOR CHAINING TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task MultipleInterceptors_ExecuteInOrder()
    {
        var callOrder = new List<string>();
        var interceptor1 = new OrderTrackingInterceptor("First", callOrder);
        var interceptor2 = new OrderTrackingInterceptor("Second", callOrder);
        var interceptor3 = new OrderTrackingInterceptor("Third", callOrder);
        var target = new CalculatorService();

        var proxy = InterceptorProxy<ICalculator>.Create(target, [interceptor1, interceptor2, interceptor3]);
        proxy.Add(1, 1);

        await Assert.That(callOrder.Count).IsEqualTo(6);
        await Assert.That(callOrder[0]).IsEqualTo("First:Before");
        await Assert.That(callOrder[1]).IsEqualTo("Second:Before");
        await Assert.That(callOrder[2]).IsEqualTo("Third:Before");
        await Assert.That(callOrder[3]).IsEqualTo("Third:After");
        await Assert.That(callOrder[4]).IsEqualTo("Second:After");
        await Assert.That(callOrder[5]).IsEqualTo("First:After");
    }

    [Test]
    public async Task MultipleInterceptors_AllSeeReturnValue()
    {
        var interceptor1 = new ReturnValueCapturingInterceptor();
        var interceptor2 = new ReturnValueCapturingInterceptor();
        var target = new CalculatorService();

        var proxy = InterceptorProxy<ICalculator>.Create(target, [interceptor1, interceptor2]);
        var result = proxy.Add(3, 7);

        await Assert.That(result).IsEqualTo(10);
        await Assert.That((int)interceptor1.CapturedReturnValue!).IsEqualTo(10);
        await Assert.That((int)interceptor2.CapturedReturnValue!).IsEqualTo(10);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // RETURN VALUE MODIFICATION TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Interceptor_CanModifyReturnValue()
    {
        var interceptor = new DoubleReturnValueInterceptor();
        var target = new CalculatorService();

        var proxy = InterceptorProxy<ICalculator>.Create(target, [interceptor]);
        var result = proxy.Add(3, 4); // target returns 7, interceptor doubles to 14

        await Assert.That(result).IsEqualTo(14);
    }

    [Test]
    public async Task Interceptor_CanShortCircuit_WithoutCallingTarget()
    {
        var interceptor = new ShortCircuitInterceptor(99);
        var target = new CalculatorService();

        var proxy = InterceptorProxy<ICalculator>.Create(target, [interceptor]);
        var result = proxy.Add(3, 4);

        // Should return interceptor's value, not the actual sum
        await Assert.That(result).IsEqualTo(99);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // VOID METHOD TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task VoidMethod_InterceptorIsInvoked()
    {
        var callLog = new List<string>();
        var interceptor = new CallTrackingInterceptor(callLog);
        var target = new GreeterService();

        var proxy = InterceptorProxy<IGreeter>.Create(target, [interceptor]);
        proxy.Greet("World");

        await Assert.That(callLog.Count).IsEqualTo(2);
        await Assert.That(callLog[0]).IsEqualTo("Interceptor:Before");
        await Assert.That(callLog[1]).IsEqualTo("Interceptor:After");
    }

    [Test]
    public async Task VoidMethod_TargetIsCalled()
    {
        var target = new GreeterService();
        var interceptor = new PassThroughInterceptor();

        var proxy = InterceptorProxy<IGreeter>.Create(target, [interceptor]);
        proxy.Greet("World");

        await Assert.That(target.LastGreeting).IsEqualTo("Hello, World!");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ASYNC METHOD TESTS (Task, Task<T>)
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task AsyncTaskMethod_InterceptorAwaitsTarget()
    {
        var callLog = new List<string>();
        var interceptor = new CallTrackingInterceptor(callLog);
        var target = new AsyncService();

        var proxy = InterceptorProxy<IAsyncService>.Create(target, [interceptor]);
        await proxy.DoWorkAsync();

        await Assert.That(target.WorkDone).IsTrue();
        await Assert.That(callLog.Count).IsEqualTo(2);
        await Assert.That(callLog[0]).IsEqualTo("Interceptor:Before");
        await Assert.That(callLog[1]).IsEqualTo("Interceptor:After");
    }

    [Test]
    public async Task AsyncTaskOfTMethod_InterceptorReturnsCorrectResult()
    {
        var interceptor = new PassThroughInterceptor();
        var target = new AsyncService();

        var proxy = InterceptorProxy<IAsyncService>.Create(target, [interceptor]);
        var result = await proxy.GetValueAsync(42);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task AsyncTaskOfTMethod_InterceptorCanModifyResult()
    {
        var interceptor = new DoubleReturnValueInterceptor();
        var target = new AsyncService();

        var proxy = InterceptorProxy<IAsyncService>.Create(target, [interceptor]);
        var result = await proxy.GetValueAsync(21);

        await Assert.That(result).IsEqualTo(42);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // INVOCATION OBJECT TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Invocation_ProceedAsync_InvokesTargetMethod()
    {
        var target = new CalculatorService();
        var method = typeof(ICalculator).GetMethod(nameof(ICalculator.Add))!;
        var args = new object?[] { 5, 10 };
        var invocation = new Invocation(target, method, args, Array.Empty<IInterceptor>());

        var result = await invocation.ProceedAsync();

        await Assert.That(result).IsEqualTo(15);
        await Assert.That(invocation.ReturnValue).IsEqualTo(15);
    }

    [Test]
    public async Task Invocation_WithInterceptors_ChainsCorrectly()
    {
        var target = new CalculatorService();
        var method = typeof(ICalculator).GetMethod(nameof(ICalculator.Add))!;
        var args = new object?[] { 3, 7 };
        var interceptor = new PassThroughInterceptor();
        var invocation = new Invocation(target, method, args, new[] { interceptor });

        var result = await invocation.ProceedAsync();

        await Assert.That(result).IsEqualTo(10);
    }

    [Test]
    public async Task Invocation_ExposesMethodInfo()
    {
        var target = new CalculatorService();
        var method = typeof(ICalculator).GetMethod(nameof(ICalculator.Add))!;
        var args = new object?[] { 1, 2 };
        var invocation = new Invocation(target, method, args, Array.Empty<IInterceptor>());

        await Assert.That(invocation.Method).IsEqualTo(method);
        await Assert.That(invocation.Method.Name).IsEqualTo("Add");
    }

    [Test]
    public async Task Invocation_ExposesArguments()
    {
        var target = new CalculatorService();
        var method = typeof(ICalculator).GetMethod(nameof(ICalculator.Add))!;
        var args = new object?[] { 100, 200 };
        var invocation = new Invocation(target, method, args, Array.Empty<IInterceptor>());

        await Assert.That(invocation.Arguments.Length).IsEqualTo(2);
        await Assert.That((int)invocation.Arguments[0]!).IsEqualTo(100);
        await Assert.That((int)invocation.Arguments[1]!).IsEqualTo(200);
    }

    [Test]
    public async Task Invocation_ExposesTarget()
    {
        var target = new CalculatorService();
        var method = typeof(ICalculator).GetMethod(nameof(ICalculator.Add))!;
        var invocation = new Invocation(target, method, Array.Empty<object?>(), Array.Empty<IInterceptor>());

        await Assert.That(invocation.Target).IsSameReferenceAs(target);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // EXCEPTION HANDLING TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Interceptor_WhenTargetThrows_ExceptionPropagates()
    {
        var interceptor = new PassThroughInterceptor();
        var target = new ThrowingService();

        var proxy = InterceptorProxy<IThrowingService>.Create(target, [interceptor]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => { proxy.ThrowSync(); return Task.CompletedTask; });
    }

    [Test]
    public async Task Interceptor_WhenAsyncTargetThrows_ExceptionPropagates()
    {
        var interceptor = new PassThroughInterceptor();
        var target = new ThrowingService();

        var proxy = InterceptorProxy<IThrowingService>.Create(target, [interceptor]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await proxy.ThrowAsync());
    }

    [Test]
    public async Task Interceptor_CanCatchAndHandleExceptions()
    {
        var interceptor = new ExceptionSwallowingInterceptor(fallbackValue: -1);
        var target = new ThrowingService();

        var proxy = InterceptorProxy<IThrowingService>.Create(target, [interceptor]);
        var result = proxy.ThrowWithReturn();

        await Assert.That(result).IsEqualTo(-1);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // STRING RETURN TYPE TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task StringReturnType_InterceptorWorksCorrectly()
    {
        var interceptor = new PassThroughInterceptor();
        var target = new GreeterService();

        var proxy = InterceptorProxy<IGreeter>.Create(target, [interceptor]);
        var result = proxy.GetGreeting("Test");

        await Assert.That(result).IsEqualTo("Hello, Test!");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SERVICE INTERFACES AND IMPLEMENTATIONS
    // ═══════════════════════════════════════════════════════════════════════════════

    public interface ICalculator
    {
        int Add(int a, int b);
    }

    public class CalculatorService : ICalculator
    {
        public int Add(int a, int b) => a + b;
    }

    public class LoggingCalculator : ICalculator
    {
        private readonly List<string> _log;

        public LoggingCalculator(List<string> log)
        {
            _log = log;
        }

        public int Add(int a, int b)
        {
            _log.Add("Target:Add");
            return a + b;
        }
    }

    public interface IGreeter
    {
        void Greet(string name);
        string GetGreeting(string name);
    }

    public class GreeterService : IGreeter
    {
        public string? LastGreeting { get; private set; }

        public void Greet(string name)
        {
            LastGreeting = $"Hello, {name}!";
        }

        public string GetGreeting(string name) => $"Hello, {name}!";
    }

    public interface IAsyncService
    {
        Task DoWorkAsync();
        Task<int> GetValueAsync(int value);
    }

    public class AsyncService : IAsyncService
    {
        public bool WorkDone { get; private set; }

        public async Task DoWorkAsync()
        {
            await Task.Delay(1);
            WorkDone = true;
        }

        public async Task<int> GetValueAsync(int value)
        {
            await Task.Delay(1);
            return value;
        }
    }

    public interface IThrowingService
    {
        void ThrowSync();
        Task ThrowAsync();
        int ThrowWithReturn();
    }

    public class ThrowingService : IThrowingService
    {
        public void ThrowSync() => throw new InvalidOperationException("Sync error");

        public async Task ThrowAsync()
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Async error");
        }

        public int ThrowWithReturn() => throw new InvalidOperationException("Return error");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // INTERCEPTOR IMPLEMENTATIONS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Interceptor that tracks calls with before/after logging.
    /// </summary>
    public class CallTrackingInterceptor : IInterceptor
    {
        private readonly List<string> _log;

        public CallTrackingInterceptor(List<string> log)
        {
            _log = log;
        }

        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            _log.Add("Interceptor:Before");
            var result = await invocation.ProceedAsync();
            _log.Add("Interceptor:After");
            return result;
        }
    }

    /// <summary>
    /// Interceptor that simply passes through to the target.
    /// </summary>
    public class PassThroughInterceptor : IInterceptor
    {
        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            return await invocation.ProceedAsync();
        }
    }

    /// <summary>
    /// Interceptor that captures the method info from the invocation.
    /// </summary>
    public class MethodInfoCapturingInterceptor : IInterceptor
    {
        public string? CapturedMethodName { get; private set; }

        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            CapturedMethodName = invocation.Method.Name;
            return await invocation.ProceedAsync();
        }
    }

    /// <summary>
    /// Interceptor that captures the arguments from the invocation.
    /// </summary>
    public class ArgumentCapturingInterceptor : IInterceptor
    {
        public object?[]? CapturedArguments { get; private set; }

        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            CapturedArguments = invocation.Arguments;
            return await invocation.ProceedAsync();
        }
    }

    /// <summary>
    /// Interceptor that captures the target from the invocation.
    /// </summary>
    public class TargetCapturingInterceptor : IInterceptor
    {
        public object? CapturedTarget { get; private set; }

        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            CapturedTarget = invocation.Target;
            return await invocation.ProceedAsync();
        }
    }

    /// <summary>
    /// Interceptor that tracks execution order with named before/after entries.
    /// </summary>
    public class OrderTrackingInterceptor : IInterceptor
    {
        private readonly string _name;
        private readonly List<string> _callOrder;

        public OrderTrackingInterceptor(string name, List<string> callOrder)
        {
            _name = name;
            _callOrder = callOrder;
        }

        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            _callOrder.Add($"{_name}:Before");
            var result = await invocation.ProceedAsync();
            _callOrder.Add($"{_name}:After");
            return result;
        }
    }

    /// <summary>
    /// Interceptor that captures the return value from the target method.
    /// </summary>
    public class ReturnValueCapturingInterceptor : IInterceptor
    {
        public object? CapturedReturnValue { get; private set; }

        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            var result = await invocation.ProceedAsync();
            CapturedReturnValue = result;
            return result;
        }
    }

    /// <summary>
    /// Interceptor that doubles the integer return value.
    /// </summary>
    public class DoubleReturnValueInterceptor : IInterceptor
    {
        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            var result = await invocation.ProceedAsync();
            if (result is int intResult)
            {
                return intResult * 2;
            }

            return result;
        }
    }

    /// <summary>
    /// Interceptor that short-circuits without calling the target.
    /// </summary>
    public class ShortCircuitInterceptor : IInterceptor
    {
        private readonly object _returnValue;

        public ShortCircuitInterceptor(object returnValue)
        {
            _returnValue = returnValue;
        }

        public Task<object?> InterceptAsync(IInvocation invocation)
        {
            // Do not call ProceedAsync - short circuit
            return Task.FromResult<object?>(_returnValue);
        }
    }

    /// <summary>
    /// Interceptor that catches exceptions and returns a fallback value.
    /// </summary>
    public class ExceptionSwallowingInterceptor : IInterceptor
    {
        private readonly object? _fallbackValue;

        public ExceptionSwallowingInterceptor(object? fallbackValue)
        {
            _fallbackValue = fallbackValue;
        }

        public async Task<object?> InterceptAsync(IInvocation invocation)
        {
            try
            {
                return await invocation.ProceedAsync();
            }
            catch
            {
                return _fallbackValue;
            }
        }
    }
}
