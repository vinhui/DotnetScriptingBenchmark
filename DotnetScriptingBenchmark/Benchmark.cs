using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.ClearScript.V8;
using NLua;
using Wasmtime;
using Module = Wasmtime.Module;

namespace DotnetScriptingBenchmark;

public class Benchmark
{
    public dynamic JSAdd;
    public LuaFunction LuaAdd;
    public LuaFunction LuaAddLoop;
    public LuaFunction LuaAddLoopInterop;

    public Func<int, int, int> WasmAdd;
    public Func<int, int> WasmAddLoop;
    public Func<int, int> WasmAddLoopInterop;


    public Benchmark()
    {
        InitWasm();
        InitLua();
        InitJS();
    }

    public V8ScriptEngine JSEngine { get; set; }

    [ParamsSource(nameof(ValuesForCount))] public int Count { get; set; }

    public static IEnumerable<int> ValuesForCount => [100, 10_000, 1_000_000];

    public Lua Lua { get; set; }

    public Instance WasmInstance { get; set; }

    public Engine WasmEngine { get; set; }

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

    private void InitLua()
    {
        Lua = new Lua();

        Lua.DoString(@"
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
        Lua.RegisterFunction("addCs", methodInfo);

        LuaAdd = Lua["add"] as LuaFunction ?? throw new InvalidOperationException();
        LuaAddLoop = Lua["addLoop"] as LuaFunction ?? throw new InvalidOperationException();
        LuaAddLoopInterop = Lua["addLoopInterop"] as LuaFunction ?? throw new InvalidOperationException();
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
    public int LuaOnly()
    {
        return (int)(long)LuaAddLoop.Call(Count)[0];
    }

    [Benchmark]
    public int JSOnly()
    {
        return (int)JSEngine.Script.addLoop(Count);
    }

    [Benchmark]
    public int CsToWasm()
    {
        var a = 0;
        for (var i = 0; i < Count; i++) i += WasmAdd(i, i);

        return a;
    }

    [Benchmark]
    public int CsToLua()
    {
        var a = 0;
        for (var i = 0; i < Count; i++) a += (int)(long)LuaAdd.Call(i, i)[0];
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
    public int WasmToCs()
    {
        return WasmAddLoopInterop.Invoke(Count);
    }

    [Benchmark]
    public int LuaToCs()
    {
        return (int)(long)LuaAddLoopInterop.Call(Count)[0];
    }

    [Benchmark]
    public int JSToCs()
    {
        return (int)JSEngine.Script.addLoopInterop(Count);
    }
}