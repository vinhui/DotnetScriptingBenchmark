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

There are 2 different benchmarks for Lua, one uses [NLua](https://github.com/NLua/NLua), which in turn depends
on [KeraLua](https://github.com/nlua/KeraLua). The other uses [Lua-CSharp](https://github.com/nuskey8/Lua-CSharp).

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
| **DotnetOnly** | **100**     |         **2.440 ns** |      **0.0779 ns** |      **0.0729 ns** |       **1.00** |          0.04 |
| WasmOnly       | 100         |           140.098 ns |          2.7996 ns |          2.9955 ns |          57.46 |          2.03 |
| NLuaOnly       | 100         |        13,867.614 ns |        222.6671 ns |        197.3885 ns |       5,687.80 |        180.33 |
| LuaCSharpOnly  | 100         |        19,822.336 ns |        155.2322 ns |        121.1951 ns |       8,130.12 |        237.19 |
| JSOnly         | 100         |        10,522.215 ns |        202.5968 ns |        290.5583 ns |       4,315.68 |        170.00 |
| PyOnly         | 100         |        11,608.102 ns |        229.0424 ns |        263.7656 ns |       4,761.06 |        172.15 |
| CsToWasm       | 100         |           264.980 ns |          4.3048 ns |          4.0267 ns |         108.68 |          3.49 |
| CsToNLua       | 100         |        35,665.296 ns |        689.6078 ns |        989.0150 ns |      14,628.11 |        577.38 |
| CsToLuaCSharp  | 100         |        26,512.495 ns |        472.1804 ns |        418.5756 ns |      10,874.09 |        352.15 |
| CsToJS         | 100         |       482,210.770 ns |      9,383.9389 ns |     10,040.7095 ns |     197,778.55 |      6,926.43 |
| CsToPy         | 100         |         3,346.453 ns |         64.0377 ns |         78.6440 ns |       1,372.55 |         50.31 |
| WasmToCs       | 100         |         2,523.584 ns |         38.7917 ns |         34.3878 ns |       1,035.05 |         32.56 |
| NLuaToCs       | 100         |       153,649.825 ns |      2,133.5090 ns |      1,781.5767 ns |      63,019.41 |      1,933.33 |
| LuaCSharpToCs  | 100         |        18,574.045 ns |        321.5438 ns |        394.8847 ns |       7,618.14 |        269.09 |
| JSToCs         | 100         |       220,140.174 ns |      3,806.4055 ns |      3,908.8982 ns |      90,290.40 |      3,012.89 |
| PyToCs         | 100         |         8,261.577 ns |        106.2404 ns |         88.7156 ns |       3,388.48 |        102.97 |
|                |             |                      |                    |                    |                |               |
| **DotnetOnly** | **10000**   |         **3.503 ns** |      **0.0746 ns** |      **0.0623 ns** |       **1.00** |          0.02 |
| WasmOnly       | 10000       |         9,024.102 ns |        112.6887 ns |         94.1002 ns |       2,576.97 |         50.83 |
| NLuaOnly       | 10000       |     1,151,386.688 ns |     21,083.9728 ns |     18,690.3927 ns |     328,795.41 |      7,598.42 |
| LuaCSharpOnly  | 10000       |     1,938,640.312 ns |     26,016.8901 ns |     21,725.2815 ns |     553,607.27 |     11,137.37 |
| JSOnly         | 10000       |       554,888.032 ns |     11,087.9684 ns |     23,144.7209 ns |     158,456.44 |      7,080.56 |
| PyOnly         | 10000       |     1,229,821.183 ns |     16,654.1290 ns |     14,763.4516 ns |     351,193.53 |      7,219.15 |
| CsToWasm       | 10000       |           539.518 ns |         10.5543 ns |         15.1367 ns |         154.07 |          4.99 |
| CsToNLua       | 10000       |     3,673,936.792 ns |     72,116.2898 ns |     80,157.0651 ns |   1,049,146.71 |     28,548.80 |
| CsToLuaCSharp  | 10000       |     2,773,538.739 ns |     49,899.5097 ns |     49,007.9908 ns |     792,024.80 |     19,106.78 |
| CsToJS         | 10000       |    45,727,489.958 ns |    889,120.4518 ns |    951,348.9229 ns |  13,058,157.59 |    345,058.40 |
| CsToPy         | 10000       |       557,550.302 ns |      7,417.2590 ns |      8,829.7200 ns |     159,216.69 |      3,655.85 |
| WasmToCs       | 10000       |       243,099.556 ns |      3,593.5831 ns |      3,000.8046 ns |      69,420.66 |      1,438.88 |
| NLuaToCs       | 10000       |    14,219,299.296 ns |    177,613.3320 ns |    157,449.5926 ns |   4,060,530.13 |     81,461.06 |
| LuaCSharpToCs  | 10000       |     1,840,481.592 ns |     18,062.4069 ns |     16,011.8533 ns |     525,576.60 |      9,953.34 |
| JSToCs         | 10000       |    18,812,445.094 ns |    372,986.8045 ns |    311,460.8745 ns |   5,372,170.50 |    125,147.13 |
| PyToCs         | 10000       |       929,547.260 ns |      7,875.7888 ns |      6,576.6403 ns |     265,445.90 |      4,855.37 |
|                |             |                      |                    |                    |                |               |
| **DotnetOnly** | **1000000** |         **4.915 ns** |      **0.0708 ns** |      **0.0628 ns** |       **1.00** |          0.02 |
| WasmOnly       | 1000000     |       917,532.016 ns |     14,821.1515 ns |     12,376.3328 ns |     186,723.90 |      3,333.16 |
| NLuaOnly       | 1000000     |   119,635,414.107 ns |  2,006,453.1110 ns |  1,876,837.4858 ns |  24,346,606.48 |    474,899.08 |
| LuaCSharpOnly  | 1000000     |   202,624,473.786 ns |  3,980,264.3821 ns |  5,708,376.7147 ns |  41,235,434.87 |  1,248,489.81 |
| JSOnly         | 1000000     |     1,338,810.369 ns |     13,796.5431 ns |     11,520.7383 ns |     272,456.86 |      4,027.27 |
| PyOnly         | 1000000     |                   NA |                 NA |                 NA |              ? |             ? |
| CsToWasm       | 1000000     |           737.365 ns |         14.8015 ns |         16.4518 ns |         150.06 |          3.75 |
| CsToNLua       | 1000000     |   374,334,662.429 ns |  6,474,790.9664 ns |  5,739,733.5449 ns |  76,179,606.06 |  1,463,664.05 |
| CsToLuaCSharp  | 1000000     |   274,154,611.467 ns |  3,706,719.9131 ns |  3,467,268.1081 ns |  55,792,296.03 |    965,802.05 |
| CsToJS         | 1000000     | 4,759,201,147.267 ns | 55,175,992.9663 ns | 51,611,658.0780 ns | 968,529,246.51 | 15,615,350.03 |
| CsToPy         | 1000000     |    58,454,626.495 ns |    999,494.7804 ns |  1,227,469.4581 ns |  11,895,907.23 |    284,445.02 |
| WasmToCs       | 1000000     |    26,881,902.976 ns |    526,495.4193 ns |    908,178.8020 ns |   5,470,646.95 |    194,403.36 |
| NLuaToCs       | 1000000     | 1,440,028,568.867 ns | 20,396,302.8302 ns | 19,078,714.3309 ns | 293,055,439.68 |  5,195,223.56 |
| LuaCSharpToCs  | 1000000     |   190,920,084.641 ns |  1,925,402.1702 ns |  1,607,798.0144 ns |  38,853,513.44 |    570,473.79 |
| JSToCs         | 1000000     | 1,843,768,537.786 ns | 23,536,666.7451 ns | 20,864,642.0176 ns | 375,219,222.17 |  6,156,644.23 |
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