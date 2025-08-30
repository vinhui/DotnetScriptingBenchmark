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

### Python

The python benchmark uses [IronPython](https://github.com/IronLanguages/ironpython3).

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

BenchmarkDotNet v0.15.2, Linux Ubuntu 25.04 (Plucky Puffin)
Intel Core i7-10510U CPU 1.80GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.109
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2


```

| Method         | Count       |                 Mean |              Error |             StdDev |          Ratio |       RatioSD |
|----------------|-------------|---------------------:|-------------------:|-------------------:|---------------:|--------------:|
| **DotnetOnly** | **100**     |         **2.548 ns** |      **0.0903 ns** |      **0.1003 ns** |       **1.00** |          0.05 |
| WasmOnly       | 100         |           147.343 ns |          2.6417 ns |          2.2059 ns |          57.91 |          2.32 |
| LuaOnly        | 100         |        14,754.548 ns |        277.8287 ns |        285.3096 ns |       5,798.49 |        242.81 |
| JSOnly         | 100         |         7,597.018 ns |         84.6854 ns |         75.0714 ns |       2,985.60 |        115.32 |
| PyOnly         | 100         |        12,610.443 ns |         86.4120 ns |         67.4648 ns |       4,955.86 |        187.29 |
| CsToWasm       | 100         |           296.662 ns |          3.6442 ns |          3.0431 ns |         116.59 |          4.51 |
| CsToLua        | 100         |        42,711.183 ns |        332.6808 ns |        259.7353 ns |      16,785.36 |        636.05 |
| CsToJS         | 100         |       488,430.311 ns |      6,596.0501 ns |      5,847.2266 ns |     191,951.54 |      7,519.67 |
| CsToPy         | 100         |         3,712.249 ns |         71.6012 ns |         76.6125 ns |       1,458.90 |         61.96 |
| WasmToCs       | 100         |         2,828.871 ns |         53.0850 ns |         67.1355 ns |       1,111.74 |         48.96 |
| LuaToCs        | 100         |       153,761.038 ns |      2,947.9298 ns |      2,461.6549 ns |      60,427.59 |      2,446.53 |
| JSToCs         | 100         |       222,264.953 ns |      3,161.1372 ns |      2,802.2658 ns |      87,349.41 |      3,438.14 |
| PyToCs         | 100         |         9,007.076 ns |        178.1173 ns |        243.8090 ns |       3,539.75 |        162.42 |
|                |             |                      |                    |                    |                |               |
| **DotnetOnly** | **10000**   |         **3.629 ns** |      **0.0866 ns** |      **0.0723 ns** |       **1.00** |          0.03 |
| WasmOnly       | 10000       |         9,528.824 ns |        189.3710 ns |        218.0800 ns |       2,626.43 |         77.32 |
| LuaOnly        | 10000       |     1,214,422.980 ns |     23,619.6768 ns |     20,938.2283 ns |     334,731.71 |      8,502.20 |
| JSOnly         | 10000       |       617,024.760 ns |     11,022.4564 ns |     20,155.1865 ns |     170,070.69 |      6,386.27 |
| PyOnly         | 10000       |     1,287,857.703 ns |     19,393.8310 ns |     17,192.1261 ns |     354,972.55 |      8,202.55 |
| CsToWasm       | 10000       |           582.948 ns |         11.5219 ns |         11.3160 ns |         160.68 |          4.32 |
| CsToLua        | 10000       |     3,808,441.168 ns |     42,853.3675 ns |     35,784.5027 ns |   1,049,721.61 |     22,259.88 |
| CsToJS         | 10000       |    47,190,876.656 ns |    778,949.6084 ns |    690,518.5388 ns |  13,007,233.31 |    309,859.92 |
| CsToPy         | 10000       |       596,137.231 ns |     11,341.7337 ns |     10,609.0647 ns |     164,313.46 |      4,235.97 |
| WasmToCs       | 10000       |       273,313.257 ns |      5,351.6400 ns |      5,948.3337 ns |      75,333.40 |      2,154.19 |
| LuaToCs        | 10000       |    14,817,775.147 ns |    235,656.9447 ns |    196,784.2218 ns |   4,084,227.12 |     94,158.15 |
| JSToCs         | 10000       |    20,266,394.127 ns |    399,468.6984 ns |    392,331.6758 ns |   5,586,031.36 |    149,932.39 |
| PyToCs         | 10000       |       986,192.718 ns |     15,052.1975 ns |     11,751.7657 ns |     271,824.55 |      6,071.48 |
|                |             |                      |                    |                    |                |               |
| **DotnetOnly** | **1000000** |         **5.125 ns** |      **0.1128 ns** |      **0.0942 ns** |       **1.00** |          0.03 |
| WasmOnly       | 1000000     |       930,919.850 ns |      8,919.9616 ns |      6,964.1193 ns |     181,684.65 |      3,468.80 |
| LuaOnly        | 1000000     |   133,883,666.269 ns |  1,750,499.7775 ns |  1,461,746.5951 ns |  26,129,647.39 |    537,688.21 |
| JSOnly         | 1000000     |     1,428,757.305 ns |     17,322.1074 ns |     15,355.5970 ns |     278,846.00 |      5,717.84 |
| PyOnly         | 1000000     |                   NA |                 NA |                 NA |              ? |             ? |
| CsToWasm       | 1000000     |           826.827 ns |         15.8472 ns |         15.5641 ns |         161.37 |          4.10 |
| CsToLua        | 1000000     |   385,577,637.857 ns |  7,409,972.5344 ns |  6,568,747.6466 ns |  75,251,955.68 |  1,818,012.62 |
| CsToJS         | 1000000     | 4,827,195,108.600 ns | 86,691,331.2142 ns | 81,091,125.0421 ns | 942,108,246.66 | 22,636,044.54 |
| CsToPy         | 1000000     |    63,469,215.587 ns |    736,618.5830 ns |    615,109.8786 ns |  12,387,084.02 |    247,729.72 |
| WasmToCs       | 1000000     |    26,499,219.923 ns |    517,932.4958 ns |    432,497.0914 ns |   5,171,768.09 |    122,402.50 |
| LuaToCs        | 1000000     | 1,667,330,513.714 ns | 11,651,204.0601 ns | 10,328,488.9241 ns | 325,407,569.31 |  6,074,115.25 |
| JSToCs         | 1000000     | 1,939,102,508.714 ns | 25,012,147.2517 ns | 22,172,617.0553 ns | 378,448,441.27 |  7,890,468.89 |
| PyToCs         | 1000000     |                   NA |                 NA |                 NA |              ? |             ? |

### Remarks

The Python `NA` results are because it's causing the following exception:

```
System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.OverflowException: Value was either too large or too small for an Int32.
```

## Conclusion

Given the above results, WebAssembly seems like a very interesting approach for adding external scripting functionality
within your normal application/game. A lot better than the languages that are currently popular for this purpose.
WebAssembly also allows the scripter to choose from a variety of languages like c/c++, [Swift](https://swiftwasm.org/),
AssemblyScript [and more](https://webassembly.org/getting-started/developers-guide/).