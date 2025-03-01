```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26120.3073)
Unknown processor
.NET SDK 8.0.403
  [Host]     : .NET 8.0.12 (8.0.1224.60305), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.12 (8.0.1224.60305), Arm64 RyuJIT AdvSIMD


```
| Method  | highSurrogate | lowSurrogate | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------- |-------------- |------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **Current** | **\ud83c**        | **\udf39**       | **0.6387 ns** | **0.0118 ns** | **0.0110 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
|         |               |              |           |           |           |       |         |           |             |
| **Current** | **\ud83c**        | **\udf55**       | **0.6180 ns** | **0.0141 ns** | **0.0125 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
|         |               |              |           |           |           |       |         |           |             |
| **Current** | **\ud83e**        | **\udde0**       | **0.6043 ns** | **0.0164 ns** | **0.0145 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
