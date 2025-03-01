```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26120.3073)
Unknown processor
.NET SDK 8.0.403
  [Host]     : .NET 8.0.12 (8.0.1224.60305), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.12 (8.0.1224.60305), Arm64 RyuJIT AdvSIMD


```
| Method   | rune | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------- |----- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Previous** | **a**    | **1.064 ns** | **0.0415 ns** | **0.0388 ns** |  **0.95** |    **0.07** |      **-** |         **-** |          **NA** |
| Current  | a    | 1.127 ns | 0.0608 ns | 0.0812 ns |  1.00 |    0.10 |      - |         - |          NA |
|          |      |          |           |           |       |         |        |           |             |
| **Previous** | **𝔹**   | **7.448 ns** | **0.1892 ns** | **0.2460 ns** |  **1.35** |    **0.05** | **0.0153** |      **64 B** |        **2.00** |
| Current  | 𝔹   | 5.506 ns | 0.1017 ns | 0.0951 ns |  1.00 |    0.02 | 0.0076 |      32 B |        1.00 |
