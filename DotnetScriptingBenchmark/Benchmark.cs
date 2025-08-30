using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.ClearScript.V8;
using Microsoft.Scripting.Hosting;
using NLua;
using Wasmtime;
using Module = Wasmtime.Module;

namespace DotnetScriptingBenchmark;

public class Benchmark
{
    public dynamic JSAdd = null!;
    public LuaFunction NLuaAdd = null!;
    public LuaFunction NLuaAddLoop = null!;
    public LuaFunction NLuaAddLoopInterop = null!;

    public Func<int, int, int> WasmAdd = null!;
    public Func<int, int> WasmAddLoop = null!;
    public Func<int, int> WasmAddLoopInterop = null!;

    public Func<int, int, int> PyAdd = null!;
    public Func<int, int> PyAddLoop = null!;
    public Func<int, int> PyAddLoopInterop = null!;


    public Benchmark()
    {
        InitWasm();
        InitNLua();
        InitJS();
        InitPy();
    }

    public V8ScriptEngine JSEngine { get; set; } = null!;

    [ParamsSource(nameof(ValuesForCount))] public int Count { get; set; }

    public static IEnumerable<int> ValuesForCount => [100, 10_000, 1_000_000];

    public Lua NLua { get; set; } = null!;

    public Instance WasmInstance { get; set; } = null!;

    public Engine WasmEngine { get; set; } = null!;

    public ScriptEngine PyEngine { get; set; } = null!;


    private void InitPy()
    {
        PyEngine = IronPython.Hosting.Python.CreateEngine();
        var scope = PyEngine.CreateScope();
        scope.SetVariable("addCs", Add);
        PyEngine.Execute(@"
def add(a, b):
    return a + b

def addLoop(count):
    a = 0
    for i in range(count):
        a += add(i, i)
    return a

def addLoopInterop(count):
    a = 0
    for i in range(count):
        a += addCs(i, i)
    return a
    ", scope);
        PyAdd = scope.GetVariable<Func<int, int, int>>("add");
        PyAddLoop = scope.GetVariable<Func<int, int>>("addLoop");
        PyAddLoopInterop = scope.GetVariable<Func<int, int>>("addLoopInterop");
    }

    private void InitJS()
    {
        JSEngine = new V8ScriptEngine();
        JSEngine.Execute(@"
function add(a, b) {
    return a + b;
}
function addLoop(count) {
    let a = 0;
    for(var i = 0; i < count; i++) {
        a += add(i, i);
    }
    return a;
}
function addLoopInterop(count) {
    let a = 0;
    for(var i = 0; i < count; i++) {
      a += addCs(i, i);
    }
    return a;
}
");

        JSEngine.AddHostObject("addCs", new Func<int, int, int>(Add));
    }

    private void InitNLua()
    {
        NLua = new Lua();

        NLua.DoString(@"
	function add (a, b)
		return a + b;
	end

    function addLoop (count)
        a = 0;
        for i = 0, count do
            a = a + add(i, i);
        end
        return a;
    end

    function addLoopInterop (count)
        a = 0;
        for i = 0, count do
            a = a + addCs(i, i);
        end
        return a;
    end
	");

        var methodInfo = typeof(Benchmark)
            .GetMethod(nameof(Add), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        NLua.RegisterFunction("addCs", methodInfo);

        NLuaAdd = NLua["add"] as LuaFunction ?? throw new InvalidOperationException();
        NLuaAddLoop = NLua["addLoop"] as LuaFunction ?? throw new InvalidOperationException();
        NLuaAddLoopInterop = NLua["addLoopInterop"] as LuaFunction ?? throw new InvalidOperationException();
    }

    public void InitWasm()
    {
        var engine = new Engine(
            new Config()
                .WithDebugInfo(true)
                .WithReferenceTypes(true)
                .WithSIMD(true)
                .WithWasmThreads(true)
                .WithOptimizationLevel(OptimizationLevel.Speed)
        );
        WasmEngine = engine;
        var module =
            Module.FromFile(engine, "benchmark.wasm");

        var linker = new Linker(engine);
        var store = new Store(engine);
        var wasiConf = new WasiConfiguration()
            .WithInheritedStandardOutput()
            .WithInheritedStandardError();
        store.SetWasiConfiguration(wasiConf);
        linker.DefineWasi();

        linker.Define(
            "",
            "addCs",
            Function.FromCallback(store, (int a, int b) => Add(a, b))
        );

        var instance = linker.Instantiate(store, module);
        WasmInstance = instance;
        var startFunc = instance.GetFunction("_start");
        startFunc?.Invoke();

        WasmAdd = instance.GetFunction<int, int, int>("add") ?? throw new InvalidOperationException();
        WasmAddLoop = instance.GetFunction<int, int>("addLoop") ?? throw new InvalidOperationException();
        WasmAddLoopInterop = instance.GetFunction<int, int>("addLoopInterop") ?? throw new InvalidOperationException();
    }

    private static int Add(int a, int b)
    {
        return a + b;
    }

    [Benchmark(Baseline = true)]
    public int DotnetOnly()
    {
        var a = 0;
        for (var i = 0; i < Count; i++) i += Add(i, i);

        return a;
    }

    [Benchmark]
    public int WasmOnly()
    {
        return WasmAddLoop.Invoke(Count);
    }

    [Benchmark]
    public int NLuaOnly()
    {
        return (int)(long)NLuaAddLoop.Call(Count)[0];
    }

    [Benchmark]
    public int JSOnly()
    {
        return (int)JSEngine.Script.addLoop(Count);
    }

    [Benchmark]
    public int PyOnly()
    {
        return PyAddLoop(Count);
    }

    [Benchmark]
    public int CsToWasm()
    {
        var a = 0;
        for (var i = 0; i < Count; i++) i += WasmAdd(i, i);

        return a;
    }

    [Benchmark]
    public int CsToNLua()
    {
        var a = 0;
        for (var i = 0; i < Count; i++) a += (int)(long)NLuaAdd.Call(i, i)[0];
        return a;
    }

    [Benchmark]
    public int CsToJS()
    {
        var a = 0;
        for (var i = 0; i < Count; i++) a += (int)JSEngine.Script.add(i, i);

        return a;
    }

    [Benchmark]
    public int CsToPy()
    {
        var a = 0;
        for (var i = 0; i < Count; i++) a += PyAdd(i, i);

        return a;
    }

    [Benchmark]
    public int WasmToCs()
    {
        return WasmAddLoopInterop.Invoke(Count);
    }

    [Benchmark]
    public int NLuaToCs()
    {
        return (int)(long)NLuaAddLoopInterop.Call(Count)[0];
    }

    [Benchmark]
    public int JSToCs()
    {
        return (int)JSEngine.Script.addLoopInterop(Count);
    }

    [Benchmark]
    public int PyToCs()
    {
        return PyAddLoopInterop(Count);
    }
}