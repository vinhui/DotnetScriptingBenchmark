# Dotnet Scripting Benchmark

This project is for benchmarking various plugin/addon scripting options for usage within dotnet.
This requires calls from dotnet to script and script to dotnet.

The main reason for creating this was to see if WebAssembly would be a viable option for this use case. The `.wasm` file
was compiled from [AssemblyScript](https://www.assemblyscript.org/).

## Scripting options

### WebAssembly

The webassembly code was written in [AssemblyScript](https://www.assemblyscript.org/)
with [WASI](https://github.com/AssemblyScript/wasi-shim) as target. On the dotnet
side, [wasmtime-dotnet](https://github.com/bytecodealliance/wasmtime-dotnet) is being used to execute the code.

### Lua

The lua scripting benchmark uses [NLua](https://github.com/NLua/NLua), which in turn depends
on [KeraLua](https://github.com/nlua/KeraLua).

### Javascript

The javascript benchmark uses [ClearScript](https://github.com/microsoft/ClearScript) with the V8 engine. The V8
javascript engine is also being used in Chromium.

## Benchmarks

The scripts are very basic. This is not a thorough test for real-world results, but it should show relative performance
pretty well.

The basic structure of the tests is the following:

```js
function add(a, b) {
    return a + b;
}

function addLoop(count) {
    var a = 0;
    for (var i = 0; i < count; i++) {
        a += add(i, i);
    }
    return a;
}

function addLoopInterop(count) {
    var a = 0;
    for (var i = 0; i < count; i++) {
        a += addCs(i, i);
    }
    return a;
}
```

Where `addCs` is a function defined in C# and exposed to the script.

- The `*Only` benchmark calls the `addLoop` function in the scripts.
- The `CsTo*` benchmark is a for loop in dotnet that call the script function in the same manner as the above `addLoop`
  example.
- The `*ToCs` benchmark calls the `addLoopInterop` function of the script.

## Results

In the following results, `count` is the amount of iterations for the `for` loops.

```

BenchmarkDotNet v0.14.0, Ubuntu 24.10 (Oracular Oriole)
Intel Core i7-10510U CPU 1.80GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```

| Method         | Count       |                 Mean |              Error |              StdDev |            Ratio |       RatioSD |
|----------------|-------------|---------------------:|-------------------:|--------------------:|-----------------:|--------------:|
| **DotnetOnly** | **100**     |         **1.770 ns** |      **0.0162 ns** |       **0.0151 ns** |         **1.00** |      **0.01** |
| WasmOnly       | 100         |           109.712 ns |          0.6308 ns |           0.5268 ns |            62.00 |          0.59 |
| LuaOnly        | 100         |        10,894.471 ns |        185.7369 ns |         221.1065 ns |         6,156.39 |        132.38 |
| JSOnly         | 100         |         6,504.573 ns |        129.4953 ns |         212.7646 ns |         3,675.69 |        122.47 |
| CsToWasm       | 100         |           246.689 ns |          4.9680 ns |          12.3721 ns |           139.40 |          7.04 |
| CsToLua        | 100         |        27,703.456 ns |        547.5674 ns |       1,155.0057 ns |        15,655.04 |        659.92 |
| CsToJS         | 100         |       430,458.110 ns |      7,495.9558 ns |       7,697.7947 ns |       243,249.00 |      4,685.76 |
| WasmToCs       | 100         |         2,427.462 ns |         47.8515 ns |          55.1059 ns |         1,371.74 |         32.46 |
| JSToCs         | 100         |       178,471.711 ns |      4,038.3509 ns |      11,521.6433 ns |       100,853.17 |      6,532.28 |
| LuaToCs        | 100         |       104,808.598 ns |      2,048.9653 ns |       3,898.3698 ns |        59,226.64 |      2,234.62 |
|                |             |                      |                    |                     |                  |               |
| **DotnetOnly** | **10000**   |         **3.049 ns** |      **0.0937 ns** |       **0.2333 ns** |         **1.01** |      **0.11** |
| WasmOnly       | 10000       |         7,946.583 ns |        158.5880 ns |         357.9595 ns |         2,620.98 |        228.24 |
| LuaOnly        | 10000       |     1,030,332.711 ns |     20,504.4974 ns |      51,063.3178 ns |       339,829.75 |     30,418.13 |
| JSOnly         | 10000       |       374,753.330 ns |      4,528.0715 ns |       3,781.1448 ns |       123,603.11 |      9,311.29 |
| CsToWasm       | 10000       |           487.897 ns |          9.7656 ns |          23.0188 ns |           160.92 |         14.19 |
| CsToLua        | 10000       |     2,710,022.471 ns |     46,758.8120 ns |      67,060.0965 ns |       893,833.85 |     70,217.09 |
| CsToJS         | 10000       |    41,296,478.349 ns |    821,570.5653 ns |   1,732,971.2974 ns |    13,620,621.41 |  1,164,888.45 |
| WasmToCs       | 10000       |       250,542.941 ns |      4,966.5511 ns |      14,089.2799 ns |        82,635.39 |      7,717.26 |
| LuaToCs        | 10000       |    10,054,387.845 ns |    189,220.9946 ns |     158,008.1004 ns |     3,316,191.01 |    252,767.58 |
| JSToCs         | 10000       |    15,425,279.250 ns |    308,312.1780 ns |     400,892.9531 ns |     5,087,646.63 |    401,535.32 |
|                |             |                      |                    |                     |                  |               |
| **DotnetOnly** | **1000000** |         **4.216 ns** |      **0.1141 ns** |       **0.1442 ns** |         **1.00** |      **0.05** |
| WasmOnly       | 1000000     |       793,422.926 ns |     16,498.5946 ns |      45,717.5313 ns |       188,409.88 |     12,411.36 |
| LuaOnly        | 1000000     |   104,194,888.593 ns |  2,073,855.4586 ns |   5,463,365.1886 ns |    24,742,600.41 |  1,519,462.97 |
| JSOnly         | 1000000     |       974,237.746 ns |      6,362.7605 ns |       5,313.1932 ns |       231,347.00 |      7,610.29 |
| CsToWasm       | 1000000     |           763.126 ns |         22.9509 ns |          64.7335 ns |           181.22 |         16.39 |
| CsToLua        | 1000000     |   305,256,191.247 ns |  8,114,534.7110 ns |  23,019,586.0320 ns |    72,487,547.76 |  5,927,017.99 |
| CsToJS         | 1000000     | 4,477,876,033.146 ns | 89,286,222.1114 ns | 236,774,412.1304 ns | 1,063,337,164.36 | 65,701,595.34 |
| WasmToCs       | 1000000     |    25,853,710.358 ns |    725,285.6943 ns |   2,057,514.9448 ns |     6,139,341.70 |    525,492.26 |
| LuaToCs        | 1000000     | 1,109,358,342.958 ns | 22,323,146.7037 ns |  64,049,297.1778 ns |   263,433,365.71 | 17,384,145.34 |
| JSToCs         | 1000000     | 1,560,277,541.900 ns | 30,413,437.6451 ns |  54,059,858.7833 ns |   370,510,725.34 | 17,477,384.33 |

Given the above results, WebAssembly seems like a very interesting approach for adding external scripting functionality
within your normal application/game. A lot better than the languages that are currently popular for this purpose.
WebAssembly also allows the scripter to choose from a variety of languages like c/c++, [Swift](https://swiftwasm.org/),
AssemblyScript [and more](https://webassembly.org/getting-started/developers-guide/).