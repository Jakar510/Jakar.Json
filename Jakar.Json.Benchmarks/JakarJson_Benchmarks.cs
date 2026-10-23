// Jakar.Json.Benchmarks
// 10/07/2026

using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using JJ = Jakar.Json;
using NewtonsoftSerializer = Newtonsoft.Json.JsonSerializer;



namespace Jakar.Json.Benchmarks;
/*

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
AMD Ryzen 9 3900X 3.80GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain

| Method                      | Categories           | Lines | Mean            | Error         | StdDev          | Ratio | RatioSD | Gen0      | Gen1      | Gen2     | Allocated  | Alloc Ratio |
|---------------------------- |--------------------- |------ |----------------:|--------------:|----------------:|------:|--------:|----------:|----------:|---------:|-----------:|------------:|
| JakarJsonNode_SumPrices     | DOM                  | 1     |      1,576.5 ns |      31.49 ns |        50.86 ns |  1.00 |    0.05 |    0.3300 |    0.0019 |        - |     2760 B |        1.00 |
| StjNode_SumPrices           | DOM                  | 1     |      1,695.6 ns |      31.58 ns |        55.30 ns |  1.08 |    0.05 |    0.3147 |         - |        - |     2640 B |        0.96 |
| NewtonsoftJObject_SumPrices | DOM                  | 1     |      3,204.4 ns |      63.49 ns |       132.52 ns |  2.03 |    0.11 |    1.1253 |    0.0267 |        - |     9440 B |        3.42 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJsonNode_SumPrices     | DOM                  | 75    |     51,265.8 ns |   1,022.94 ns |     1,764.51 ns |  1.00 |    0.05 |    7.7515 |    1.2207 |        - |    64848 B |        1.00 |
| StjNode_SumPrices           | DOM                  | 75    |     60,229.5 ns |   1,176.05 ns |     2,120.65 ns |  1.18 |    0.06 |   10.8032 |    2.1362 |        - |    90720 B |        1.40 |
| NewtonsoftJObject_SumPrices | DOM                  | 75    |    117,671.6 ns |   2,320.05 ns |     2,934.12 ns |  2.30 |    0.10 |   22.2168 |    6.3477 |        - |   185993 B |        2.87 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJsonNode_SumPrices     | DOM                  | 7500  |  6,972,686.0 ns | 139,241.48 ns |   232,641.24 ns |  1.00 |    0.05 |  757.8125 |  671.8750 |        - |  6355551 B |        1.00 |
| StjNode_SumPrices           | DOM                  | 7500  |  8,452,657.0 ns | 168,111.51 ns |   418,656.03 ns |  1.21 |    0.07 |  875.0000 |  812.5000 |  31.2500 |  8942890 B |        1.41 |
| NewtonsoftJObject_SumPrices | DOM                  | 7500  | 42,816,054.5 ns | 855,081.85 ns | 1,331,259.54 ns |  6.15 |    0.27 | 3000.0000 | 2909.0909 | 909.0909 | 17888626 B |        2.81 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_FromUtf8          | Deserialize UTF-8    | 1     |      1,003.1 ns |      19.26 ns |        46.15 ns |  1.00 |    0.06 |    0.0439 |         - |        - |      368 B |        1.00 |
| SystemTextJson_FromUtf8     | Deserialize UTF-8    | 1     |      1,235.0 ns |      24.25 ns |        49.53 ns |  1.23 |    0.07 |    0.1030 |         - |        - |      864 B |        2.35 |
| Newtonsoft_FromUtf8         | Deserialize UTF-8    | 1     |      2,701.4 ns |      53.87 ns |       111.25 ns |  2.70 |    0.16 |    1.0529 |    0.0153 |        - |     8832 B |       24.00 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_FromUtf8          | Deserialize UTF-8    | 75    |     32,761.1 ns |     653.73 ns |     1,320.57 ns |  1.00 |    0.06 |    1.4038 |         - |        - |    12208 B |        1.00 |
| SystemTextJson_FromUtf8     | Deserialize UTF-8    | 75    |     43,062.9 ns |     851.06 ns |     1,076.32 ns |  1.32 |    0.06 |    1.6479 |    0.0610 |        - |    14216 B |        1.16 |
| Newtonsoft_FromUtf8         | Deserialize UTF-8    | 75    |     74,388.2 ns |   1,471.10 ns |     2,333.32 ns |  2.27 |    0.12 |    3.0518 |    0.1221 |        - |    26137 B |        2.14 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_FromUtf8          | Deserialize UTF-8    | 7500  |  3,233,452.8 ns |  64,584.97 ns |    63,431.08 ns |  1.00 |    0.03 |  148.4375 |   78.1250 |        - |  1259431 B |        1.00 |
| SystemTextJson_FromUtf8     | Deserialize UTF-8    | 7500  |  4,398,883.5 ns |  86,849.95 ns |   142,696.98 ns |  1.36 |    0.05 |  156.2500 |  109.3750 |        - |  1331231 B |        1.06 |
| Newtonsoft_FromUtf8         | Deserialize UTF-8    | 7500  |  7,519,787.4 ns | 150,214.26 ns |   172,986.97 ns |  2.33 |    0.07 |  203.1250 |  125.0000 |        - |  1744357 B |        1.39 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_FromString        | Deserialize string   | 1     |        989.4 ns |      18.10 ns |        28.18 ns |  1.00 |    0.04 |    0.0439 |         - |        - |      368 B |        1.00 |
| SystemTextJson_FromString   | Deserialize string   | 1     |      1,382.0 ns |      26.73 ns |        23.70 ns |  1.40 |    0.05 |    0.1030 |         - |        - |      864 B |        2.35 |
| Newtonsoft_FromString       | Deserialize string   | 1     |      2,711.8 ns |      53.65 ns |       103.37 ns |  2.74 |    0.13 |    0.6981 |    0.0076 |        - |     5856 B |       15.91 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_FromString        | Deserialize string   | 75    |     30,534.7 ns |     522.15 ns |       621.58 ns |  1.00 |    0.03 |    1.4343 |    0.0305 |        - |    12208 B |        1.00 |
| SystemTextJson_FromString   | Deserialize string   | 75    |     46,531.4 ns |     921.61 ns |     1,097.12 ns |  1.52 |    0.05 |    1.6479 |    0.0610 |        - |    14216 B |        1.16 |
| Newtonsoft_FromString       | Deserialize string   | 75    |     73,073.5 ns |   1,455.83 ns |     2,391.98 ns |  2.39 |    0.09 |    2.6855 |    0.1221 |        - |    23161 B |        1.90 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_FromString        | Deserialize string   | 7500  |  3,034,344.5 ns |  58,991.25 ns |    55,180.45 ns |  1.00 |    0.02 |  148.4375 |   78.1250 |        - |  1259431 B |        1.00 |
| SystemTextJson_FromString   | Deserialize string   | 7500  |  4,590,354.2 ns |  91,315.73 ns |   133,849.43 ns |  1.51 |    0.05 |  156.2500 |  109.3750 |        - |  1331231 B |        1.06 |
| Newtonsoft_FromString       | Deserialize string   | 7500  |  7,301,285.3 ns | 125,247.80 ns |   111,028.92 ns |  2.41 |    0.05 |  203.1250 |  117.1875 |        - |  1741260 B |        1.38 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJsonTape_SumPrices     | Navigate (read-only) | 1     |        583.5 ns |       1.49 ns |         1.39 ns |  1.00 |    0.00 |    0.0076 |         - |        - |       64 B |        1.00 |
| StjDocument_SumPrices       | Navigate (read-only) | 1     |        755.7 ns |       2.21 ns |         2.07 ns |  1.30 |    0.00 |    0.0086 |         - |        - |       72 B |        1.12 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJsonTape_SumPrices     | Navigate (read-only) | 75    |     19,861.6 ns |     126.82 ns |       118.63 ns |  1.00 |    0.01 |         - |         - |        - |       64 B |        1.00 |
| StjDocument_SumPrices       | Navigate (read-only) | 75    |     26,796.8 ns |      59.39 ns |        52.65 ns |  1.35 |    0.01 |         - |         - |        - |       72 B |        1.12 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJsonTape_SumPrices     | Navigate (read-only) | 7500  |  1,974,997.2 ns |   5,201.68 ns |     4,611.15 ns |  1.00 |    0.00 |         - |         - |        - |       86 B |        1.00 |
| StjDocument_SumPrices       | Navigate (read-only) | 7500  |  2,681,552.2 ns |  16,848.64 ns |    14,935.88 ns |  1.36 |    0.01 |         - |         - |        - |       94 B |        1.09 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_Stream            | Serialize Stream     | 1     |        351.7 ns |       0.62 ns |         0.58 ns |  1.00 |    0.00 |         - |         - |        - |          - |          NA |
| SystemTextJson_Stream       | Serialize Stream     | 1     |        476.0 ns |       1.66 ns |         1.55 ns |  1.35 |    0.00 |    0.0067 |         - |        - |       56 B |          NA |
| Newtonsoft_Stream           | Serialize Stream     | 1     |      2,009.5 ns |      26.81 ns |        25.08 ns |  5.71 |    0.07 |    1.1368 |    0.0305 |        - |     9528 B |          NA |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_Stream            | Serialize Stream     | 75    |     13,766.2 ns |      59.40 ns |        55.56 ns |  1.00 |    0.01 |         - |         - |        - |          - |          NA |
| SystemTextJson_Stream       | Serialize Stream     | 75    |     17,020.6 ns |      41.60 ns |        34.74 ns |  1.24 |    0.01 |         - |         - |        - |       56 B |          NA |
| Newtonsoft_Stream           | Serialize Stream     | 75    |     49,984.8 ns |     289.94 ns |       271.21 ns |  3.63 |    0.02 |    3.6011 |    0.2441 |        - |    30136 B |          NA |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_Stream            | Serialize Stream     | 7500  |  1,323,862.9 ns |   8,524.18 ns |     7,973.53 ns |  1.00 |    0.01 |         - |         - |        - |       12 B |        1.00 |
| SystemTextJson_Stream       | Serialize Stream     | 7500  |  2,469,310.5 ns |   7,099.72 ns |     6,641.08 ns |  1.87 |    0.01 |         - |         - |        - |      527 B |       43.92 |
| Newtonsoft_Stream           | Serialize Stream     | 7500  |  4,939,314.2 ns |  47,306.09 ns |    44,250.15 ns |  3.73 |    0.04 |  101.5625 |         - |        - |   861783 B |   71,815.25 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_Utf8_IntoBuffer   | Serialize UTF-8      | 1     |        375.8 ns |       1.60 ns |         1.49 ns |  1.00 |    0.01 |         - |         - |        - |          - |        0.00 |
| JakarJson_Utf8              | Serialize UTF-8      | 1     |        377.2 ns |       2.16 ns |         2.02 ns |  1.00 |    0.01 |    0.0334 |         - |        - |      280 B |        1.00 |
| SystemTextJson_Utf8         | Serialize UTF-8      | 1     |        446.6 ns |       1.39 ns |         1.30 ns |  1.18 |    0.01 |    0.0324 |         - |        - |      272 B |        0.97 |
| Newtonsoft_Utf8             | Serialize UTF-8      | 1     |      2,206.7 ns |      41.51 ns |        42.63 ns |  5.85 |    0.11 |    1.2093 |    0.0343 |        - |    10128 B |       36.17 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_Utf8_IntoBuffer   | Serialize UTF-8      | 75    |     15,457.9 ns |     300.78 ns |       401.53 ns |  0.99 |    0.04 |         - |         - |        - |          - |        0.00 |
| JakarJson_Utf8              | Serialize UTF-8      | 75    |     15,703.8 ns |     277.49 ns |       514.35 ns |  1.00 |    0.05 |    0.9155 |         - |        - |     7848 B |        1.00 |
| SystemTextJson_Utf8         | Serialize UTF-8      | 75    |     19,692.3 ns |     386.63 ns |       601.94 ns |  1.26 |    0.06 |    0.9766 |         - |        - |     8200 B |        1.04 |
| Newtonsoft_Utf8             | Serialize UTF-8      | 75    |     57,854.3 ns |   1,132.63 ns |     2,154.94 ns |  3.69 |    0.18 |    5.8594 |    0.3662 |        - |    50017 B |        6.37 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_Utf8_IntoBuffer   | Serialize UTF-8      | 7500  |  1,492,679.3 ns |  29,188.65 ns |    44,574.15 ns |  0.92 |    0.05 |         - |         - |        - |       12 B |       0.000 |
| JakarJson_Utf8              | Serialize UTF-8      | 7500  |  1,620,660.4 ns |  32,073.29 ns |    66,236.91 ns |  1.00 |    0.06 |   23.4375 |   23.4375 |  23.4375 |   781488 B |       1.000 |
| SystemTextJson_Utf8         | Serialize UTF-8      | 7500  |  2,073,728.3 ns |  40,365.46 ns |    60,417.12 ns |  1.28 |    0.06 |    7.8125 |    7.8125 |   7.8125 |   818886 B |       1.048 |
| Newtonsoft_Utf8             | Serialize UTF-8      | 7500  |  5,714,691.4 ns | 111,939.30 ns |   177,547.60 ns |  3.53 |    0.18 |  242.1875 |  132.8125 | 125.0000 |  3731504 B |       4.775 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_String            | Serialize string     | 1     |        339.2 ns |       5.97 ns |        14.08 ns |  1.00 |    0.06 |    0.0629 |         - |        - |      528 B |        1.00 |
| JakarJson_Chars_IntoBuffer  | Serialize string     | 1     |        342.8 ns |       0.98 ns |         0.86 ns |  1.01 |    0.04 |         - |         - |        - |          - |        0.00 |
| SystemTextJson_String       | Serialize string     | 1     |        552.9 ns |      10.98 ns |        25.00 ns |  1.63 |    0.10 |    0.0601 |         - |        - |      504 B |        0.95 |
| Newtonsoft_String           | Serialize string     | 1     |      1,500.4 ns |       8.30 ns |         7.36 ns |  4.43 |    0.17 |    0.3109 |         - |        - |     2616 B |        4.95 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_Chars_IntoBuffer  | Serialize string     | 75    |      8,879.1 ns |      37.24 ns |        29.07 ns |  0.91 |    0.01 |         - |         - |        - |          - |        0.00 |
| JakarJson_String            | Serialize string     | 75    |      9,720.3 ns |      76.96 ns |        64.27 ns |  1.00 |    0.01 |    1.8005 |         - |        - |    15208 B |        1.00 |
| SystemTextJson_String       | Serialize string     | 75    |     18,228.4 ns |      69.58 ns |        65.08 ns |  1.88 |    0.01 |    1.9531 |         - |        - |    16376 B |        1.08 |
| Newtonsoft_String           | Serialize string     | 75    |     42,153.7 ns |     271.25 ns |       253.73 ns |  4.34 |    0.04 |    4.7607 |    0.1221 |        - |    40336 B |        2.65 |
|                             |                      |       |                 |               |                 |       |         |           |           |          |            |             |
| JakarJson_Chars_IntoBuffer  | Serialize string     | 7500  |    850,095.9 ns |   3,538.89 ns |     3,310.28 ns |  0.83 |    0.01 |         - |         - |        - |        6 B |       0.000 |
| JakarJson_String            | Serialize string     | 7500  |  1,028,002.2 ns |   6,729.27 ns |     6,294.56 ns |  1.00 |    0.01 |   42.9688 |   42.9688 |  42.9688 |  1517908 B |       1.000 |
| SystemTextJson_String       | Serialize string     | 7500  |  1,988,415.2 ns |   9,740.45 ns |     9,111.22 ns |  1.93 |    0.01 |   31.2500 |   31.2500 |  31.2500 |  1637818 B |       1.079 |
| Newtonsoft_String           | Serialize string     | 7500  |  4,332,946.4 ns |  32,582.67 ns |    28,883.69 ns |  4.22 |    0.04 |  281.2500 |  164.0625 |  15.6250 |  3592132 B |       2.367 |

// * Warnings *
MultimodalDistribution

// * Hints *
Outliers
  JakarJson_Benchmarks.JakarJsonNode_SumPrices: Toolchain=InProcessEmitToolchain     -> 1 outlier  was  removed, 2 outliers were detected (1.45 us, 1.69 us)
  JakarJson_Benchmarks.StjNode_SumPrices: Toolchain=InProcessEmitToolchain           -> 2 outliers were removed (1.88 us, 1.97 us)
  JakarJson_Benchmarks.JakarJsonNode_SumPrices: Toolchain=InProcessEmitToolchain     -> 1 outlier  was  removed (56.92 us)
  JakarJson_Benchmarks.JakarJsonNode_SumPrices: Toolchain=InProcessEmitToolchain     -> 1 outlier  was  removed (8.01 ms)
  JakarJson_Benchmarks.NewtonsoftJObject_SumPrices: Toolchain=InProcessEmitToolchain -> 1 outlier  was  removed (47.89 ms)
  JakarJson_Benchmarks.JakarJson_FromUtf8: Toolchain=InProcessEmitToolchain          -> 1 outlier  was  removed (1.14 us)
  JakarJson_Benchmarks.SystemTextJson_FromUtf8: Toolchain=InProcessEmitToolchain     -> 2 outliers were removed (1.39 us, 1.41 us)
  JakarJson_Benchmarks.Newtonsoft_FromUtf8: Toolchain=InProcessEmitToolchain         -> 2 outliers were removed (80.03 us, 80.58 us)
  JakarJson_Benchmarks.JakarJson_FromUtf8: Toolchain=InProcessEmitToolchain          -> 1 outlier  was  removed (3.44 ms)
  JakarJson_Benchmarks.SystemTextJson_FromUtf8: Toolchain=InProcessEmitToolchain     -> 1 outlier  was  detected (3.99 ms)
  JakarJson_Benchmarks.Newtonsoft_FromUtf8: Toolchain=InProcessEmitToolchain         -> 1 outlier  was  removed (8.25 ms)
  JakarJson_Benchmarks.JakarJson_FromString: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (1.07 us)
  JakarJson_Benchmarks.SystemTextJson_FromString: Toolchain=InProcessEmitToolchain   -> 1 outlier  was  removed (1.50 us)
  JakarJson_Benchmarks.JakarJson_FromString: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed, 2 outliers were detected (28.83 us, 32.34 us)
  JakarJson_Benchmarks.SystemTextJson_FromString: Toolchain=InProcessEmitToolchain   -> 1 outlier  was  removed (51.20 us)
  JakarJson_Benchmarks.JakarJson_FromString: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (3.26 ms)
  JakarJson_Benchmarks.Newtonsoft_FromString: Toolchain=InProcessEmitToolchain       -> 1 outlier  was  removed, 4 outliers were detected (7.10 ms..7.14 ms, 7.54 ms)
  JakarJson_Benchmarks.StjDocument_SumPrices: Toolchain=InProcessEmitToolchain       -> 1 outlier  was  removed, 2 outliers were detected (26.66 us, 26.98 us)
  JakarJson_Benchmarks.JakarJsonTape_SumPrices: Toolchain=InProcessEmitToolchain     -> 1 outlier  was  removed (1.99 ms)
  JakarJson_Benchmarks.StjDocument_SumPrices: Toolchain=InProcessEmitToolchain       -> 1 outlier  was  removed (2.73 ms)
  JakarJson_Benchmarks.Newtonsoft_Stream: Toolchain=InProcessEmitToolchain           -> 1 outlier  was  detected (1.93 us)
  JakarJson_Benchmarks.SystemTextJson_Stream: Toolchain=InProcessEmitToolchain       -> 2 outliers were removed (17.16 us, 17.27 us)
  JakarJson_Benchmarks.Newtonsoft_Utf8: Toolchain=InProcessEmitToolchain             -> 1 outlier  was  removed, 4 outliers were detected (2.06 us..2.19 us, 2.25 us)
  JakarJson_Benchmarks.JakarJson_Utf8: Toolchain=InProcessEmitToolchain              -> 3 outliers were removed (17.23 us..17.34 us)
  JakarJson_Benchmarks.SystemTextJson_Utf8: Toolchain=InProcessEmitToolchain         -> 1 outlier  was  removed (22.03 us)
  JakarJson_Benchmarks.JakarJson_Utf8_IntoBuffer: Toolchain=InProcessEmitToolchain   -> 1 outlier  was  detected (1.37 ms)
  JakarJson_Benchmarks.JakarJson_String: Toolchain=InProcessEmitToolchain            -> 11 outliers were removed (387.28 ns..409.93 ns)
  JakarJson_Benchmarks.JakarJson_Chars_IntoBuffer: Toolchain=InProcessEmitToolchain  -> 1 outlier  was  removed (347.32 ns)
  JakarJson_Benchmarks.Newtonsoft_String: Toolchain=InProcessEmitToolchain           -> 1 outlier  was  removed (1.53 us)
  JakarJson_Benchmarks.JakarJson_Chars_IntoBuffer: Toolchain=InProcessEmitToolchain  -> 3 outliers were removed, 5 outliers were detected (8.82 us, 8.83 us, 8.98 us..9.02 us)
  JakarJson_Benchmarks.JakarJson_String: Toolchain=InProcessEmitToolchain            -> 2 outliers were removed, 3 outliers were detected (9.58 us, 9.91 us, 10.11 us)
  JakarJson_Benchmarks.Newtonsoft_String: Toolchain=InProcessEmitToolchain           -> 1 outlier  was  removed (4.41 ms)

// * Legends *
  Categories  : All categories of the corresponded method, class, and assembly
  Lines       : Value of the 'Lines' parameter
  Mean        : Arithmetic mean of all measurements
  Error       : Half of 99.9% confidence interval
  StdDev      : Standard deviation of all measurements
  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD     : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Gen0        : GC Generation 0 collects per 1000 operations
  Gen1        : GC Generation 1 collects per 1000 operations
  Gen2        : GC Generation 2 collects per 1000 operations
  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Alloc Ratio : Allocated memory ratio distribution ([Current]/[Baseline])
  1 ns        : 1 Nanosecond (0.000000001 sec)

// * Diagnostic Output - MemoryDiagnoser *


// ***** BenchmarkRunner: End *****
Run time: 00:35:18 (2118.48 sec), executed benchmarks: 81

Global total time: 00:35:21 (2121.42 sec), executed benchmarks: 81
// * Artifacts cleanup *
Artifacts cleanup is finished
*/



/// <summary>
///     Jakar.Json (<c>[JsonModel]</c>) against System.Text.Json (source generation, the <c>SystemTextJson_*</c> methods) and Newtonsoft.Json (reflection), on the same model shape (Jakar.Json SPEC.md §12.5).
///     Jakar.Json is the baseline of every category: the context-less <c>[JsonModel]</c> <see cref="BenchOrder"/>, and <c>JsonTape</c> / <c>JNode</c> for the dynamic ones.
///     <para>
///         Each library gets its own model type (<see cref="BenchOrder"/> / <see cref="StjOrder"/>): BenchOrder is a [JsonModel]; StjOrder is a plain class that
///         System.Text.Json reads and writes through its source-generated <see cref="BenchContext"/>, and Newtonsoft by reflection. All three write enums as names.
///     </para>
///     Run: <c>dotnet run -c Release --project Jakar.Json.Benchmarks -- --filter *JakarJson*</c>
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
[CategoriesColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class JakarJson_Benchmarks
{
    private static readonly JsonSerializerSettings __newtonsoftSettings   = new() { Converters = [new StringEnumConverter()] };
    private static readonly NewtonsoftSerializer   __newtonsoftSerializer = NewtonsoftSerializer.Create(__newtonsoftSettings);
    private static readonly UTF8Encoding           __utf8NoBom            = new(false);

    private readonly char[]       __chars  = new char[4 * 1024 * 1024];
    private readonly byte[]       __bytes  = new byte[4 * 1024 * 1024];
    private readonly MemoryStream __output = new(4 * 1024 * 1024);

    private BenchOrder __order    = null!;
    private StjOrder   __stjOrder = null!;
    private string     __json     = "";
    private byte[]     __utf8     = [];


    /// <summary> Lines per order: ≈ 200 B, 10 KB and 1 MB documents. </summary>
    [Params(1, 75, 7_500)] public int Lines { get; set; }


    [GlobalSetup] public void Setup()
    {
        Random random = new(510);

        __order = new BenchOrder
                  {
                      Id       = new Guid(Enumerable.Range(0, 16).Select(i => (byte)random.Next(256)).ToArray()),
                      Customer = "Ada Lovelace",
                      Created  = new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero),
                      Status   = BenchStatus.Paid,
                      Lines = Enumerable.Range(0, Lines)
                                        .Select(i => new BenchLine
                                                     {
                                                         Sku      = $"SKU-{i:D6}",
                                                         Name     = $"Item \"{i}\" — Ø",
                                                         Quantity = random.Next(1, 100),
                                                         Price    = Math.Round((decimal)random.NextDouble() * 100, 2),
                                                         Taxable  = i % 2 == 0
                                                     })
                                        .ToList()
                  };

        __stjOrder = StjOrder.From(__order);

        // Every library reads the same document. All three accept the others' output; the inputs differ only in whitespace (Jakar.Json writes "name" : value).
        __json = __order.ToJson();
        __utf8 = __order.ToJsonUtf8();

        Verify();
    }

    /// <summary> Fails setup, instead of reporting meaningless numbers, if a library reads the shared document differently. </summary>
    private void Verify()
    {
        string expected = __stjOrder.ToJson();

        Check(nameof(SystemTextJson_FromString),  SystemTextJson_FromString().ToJson());
        Check(nameof(SystemTextJson_FromUtf8),    SystemTextJson_FromUtf8().ToJson());
        Check(nameof(Newtonsoft_FromString),      Newtonsoft_FromString()!.ToJson());
        Check(nameof(Newtonsoft_FromUtf8),        Newtonsoft_FromUtf8()!.ToJson());
        Check(nameof(JakarJson_FromString),       StjOrder.From(JakarJson_FromString()).ToJson());
        Check(nameof(JakarJson_FromUtf8),         StjOrder.From(JakarJson_FromUtf8()).ToJson());
        Check(nameof(Newtonsoft_String),          StjOrder.FromJson(Newtonsoft_String()).ToJson());

        decimal sum = __order.Lines.Sum(static line => line.Price);
        if ( JakarJsonTape_SumPrices() != sum || JakarJsonNode_SumPrices() != sum || StjDocument_SumPrices() != sum || StjNode_SumPrices() != sum || NewtonsoftJObject_SumPrices() != sum ) { throw new InvalidOperationException("The navigation benchmarks disagree on the sum of prices."); }

        void Check( string name, string actual )
        {
            if ( actual != expected ) { throw new InvalidOperationException($"{name} doesn't round-trip the benchmark document."); }
        }
    }


    // ─── Serialize ───────────────────────────────────────────────────────────

    [Benchmark] [BenchmarkCategory(                 "Serialize string")] public string SystemTextJson_String()  => __stjOrder.ToJson();
    [Benchmark(Baseline = true)] [BenchmarkCategory("Serialize string")] public string JakarJson_String()       => __order.ToJson();
    [Benchmark] [BenchmarkCategory(                 "Serialize string")] public string Newtonsoft_String()      => JsonConvert.SerializeObject(__stjOrder, __newtonsoftSettings);

    [Benchmark] [BenchmarkCategory("Serialize string")] public int JakarJson_Chars_IntoBuffer() => __order.TryFormat(__chars, out int written, default, null)
                                                                                                       ? written
                                                                                                       : -1;

    [Benchmark] [BenchmarkCategory(                 "Serialize UTF-8")] public byte[] SystemTextJson_Utf8()  => __stjOrder.ToUtf8Json();
    [Benchmark(Baseline = true)] [BenchmarkCategory("Serialize UTF-8")] public byte[] JakarJson_Utf8()       => __order.ToJsonUtf8();

    [Benchmark] [BenchmarkCategory("Serialize UTF-8")] public byte[] Newtonsoft_Utf8()
    {
        using MemoryStream stream = new();

        using ( StreamWriter text = new(stream, __utf8NoBom, 4096, true) )
        using ( JsonTextWriter writer = new(text) ) { __newtonsoftSerializer.Serialize(writer, __stjOrder); }

        return stream.ToArray();
    }

    [Benchmark] [BenchmarkCategory("Serialize UTF-8")] public int JakarJson_Utf8_IntoBuffer() => __order.TryFormat(__bytes, out int written, default, null)
                                                                                                     ? written
                                                                                                     : -1;

    [Benchmark] [BenchmarkCategory("Serialize Stream")] public long SystemTextJson_Stream()
    {
        __output.Position = 0;
        __stjOrder.WriteToAsync(__output).GetAwaiter().GetResult();
        return __output.Position;
    }

    [Benchmark(Baseline = true)] [BenchmarkCategory("Serialize Stream")] public long JakarJson_Stream()
    {
        __output.Position = 0;
        __order.ToJson(__output);
        return __output.Position;
    }

    [Benchmark] [BenchmarkCategory("Serialize Stream")] public long Newtonsoft_Stream()
    {
        __output.Position = 0;

        using ( StreamWriter text = new(__output, __utf8NoBom, 4096, true) )
        using ( JsonTextWriter writer = new(text) ) { __newtonsoftSerializer.Serialize(writer, __stjOrder); }

        return __output.Position;
    }


    // ─── Deserialize ─────────────────────────────────────────────────────────

    [Benchmark] [BenchmarkCategory(                 "Deserialize string")] public StjOrder    SystemTextJson_FromString()  => StjOrder.FromJson(__json);
    [Benchmark(Baseline = true)] [BenchmarkCategory("Deserialize string")] public BenchOrder  JakarJson_FromString()       => BenchOrder.FromJson(__json);
    [Benchmark] [BenchmarkCategory(                 "Deserialize string")] public StjOrder?   Newtonsoft_FromString()      => JsonConvert.DeserializeObject<StjOrder>(__json, __newtonsoftSettings);

    [Benchmark] [BenchmarkCategory(                 "Deserialize UTF-8")] public StjOrder    SystemTextJson_FromUtf8()  => StjOrder.FromJson(__utf8.AsSpan());
    [Benchmark(Baseline = true)] [BenchmarkCategory("Deserialize UTF-8")] public BenchOrder  JakarJson_FromUtf8()       => BenchOrder.FromJson(__utf8.AsSpan());

    [Benchmark] [BenchmarkCategory("Deserialize UTF-8")] public StjOrder? Newtonsoft_FromUtf8()
    {
        using StreamReader   text   = new(new MemoryStream(__utf8, false), __utf8NoBom, false);
        using JsonTextReader reader = new(text);
        return __newtonsoftSerializer.Deserialize<StjOrder>(reader);
    }


    // ─── Dynamic ─────────────────────────────────────────────────────────────

    [Benchmark] [BenchmarkCategory("Navigate (read-only)")] public decimal StjDocument_SumPrices()
    {
        using JsonDocument document = JsonDocument.Parse(__utf8);
        decimal            sum      = 0;
        foreach ( JsonElement line in document.RootElement.GetProperty("Lines").EnumerateArray() ) { sum += line.GetProperty("Price").GetDecimal(); }

        return sum;
    }

    [Benchmark(Baseline = true)] [BenchmarkCategory("Navigate (read-only)")] public decimal JakarJsonTape_SumPrices()
    {
        using JJ.JsonTape tape = JJ.JsonTape.Parse(__utf8);
        decimal           sum  = 0;
        foreach ( JJ.JsonItem line in tape.Root["Lines"].EnumerateArray() ) { sum += line["Price"].GetDecimal(); }

        return sum;
    }

    [Benchmark] [BenchmarkCategory("DOM")] public decimal StjNode_SumPrices()
    {
        System.Text.Json.Nodes.JsonNode root = System.Text.Json.Nodes.JsonNode.Parse(__utf8)!;
        decimal                         sum  = 0;
        foreach ( System.Text.Json.Nodes.JsonNode? line in root["Lines"]!.AsArray() ) { sum += line!["Price"]!.GetValue<decimal>(); }

        return sum;
    }

    [Benchmark(Baseline = true)] [BenchmarkCategory("DOM")] public decimal JakarJsonNode_SumPrices()
    {
        JJ.JNode root = JJ.JNode.Parse(__utf8.AsSpan())!;
        decimal  sum  = 0;
        foreach ( JJ.JNode? line in root["Lines"]!.AsArray() ) { sum += (decimal)line!["Price"]; }

        return sum;
    }

    [Benchmark] [BenchmarkCategory("DOM")] public decimal NewtonsoftJObject_SumPrices()
    {
        JObject root = JObject.Parse(__json);
        decimal sum  = 0;
        foreach ( JToken line in (JArray)root["Lines"]! ) { sum += (decimal)line["Price"]!; }

        return sum;
    }
}



public enum BenchStatus
{
    Draft,
    Sent,
    Paid
}



// ─── Jakar.Json ──────────────────────────────────────────────────────────────



[JsonModel]
public sealed partial class BenchOrder
{
    public Guid            Id       { get; set; }
    public string          Customer { get; set; } = "";
    public DateTimeOffset  Created  { get; set; }
    public BenchStatus     Status   { get; set; }
    public List<BenchLine> Lines    { get; set; } = [];
}



[JsonModel]
public sealed partial class BenchLine
{
    public string  Sku      { get; set; } = "";
    public string  Name     { get; set; } = "";
    public int     Quantity { get; set; }
    public decimal Price    { get; set; }
    public bool    Taxable  { get; set; }
}



// ─── System.Text.Json (and Newtonsoft.Json, by reflection) ──────────────────



public sealed class StjOrder
{
    public Guid           Id       { get; set; }
    public string         Customer { get; set; } = "";
    public DateTimeOffset Created  { get; set; }
    public BenchStatus    Status   { get; set; }
    public List<StjLine>  Lines    { get; set; } = [];


    public static StjOrder From( BenchOrder order ) => new()
                                                       {
                                                           Id       = order.Id,
                                                           Customer = order.Customer,
                                                           Created  = order.Created,
                                                           Status   = order.Status,
                                                           Lines =
                                                           [
                                                               .. order.Lines.Select(static line => new StjLine
                                                                                                    {
                                                                                                        Sku      = line.Sku,
                                                                                                        Name     = line.Name,
                                                                                                        Quantity = line.Quantity,
                                                                                                        Price    = line.Price,
                                                                                                        Taxable  = line.Taxable
                                                                                                    })
                                                           ]
                                                       };

    public string ToJson()                     => System.Text.Json.JsonSerializer.Serialize(this, BenchContext.Default.StjOrder);
    public byte[] ToUtf8Json()                 => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(this, BenchContext.Default.StjOrder);
    public System.Threading.Tasks.Task WriteToAsync( Stream stream ) => System.Text.Json.JsonSerializer.SerializeAsync(stream, this, BenchContext.Default.StjOrder);

    public static StjOrder FromJson( string             json )     => System.Text.Json.JsonSerializer.Deserialize(json,     BenchContext.Default.StjOrder)!;
    public static StjOrder FromJson( ReadOnlySpan<byte> utf8Json ) => System.Text.Json.JsonSerializer.Deserialize(utf8Json, BenchContext.Default.StjOrder)!;
}



public sealed class StjLine
{
    public string  Sku      { get; set; } = "";
    public string  Name     { get; set; } = "";
    public int     Quantity { get; set; }
    public decimal Price    { get; set; }
    public bool    Taxable  { get; set; }
}



// Enums as names, like Jakar.Json's default and the Newtonsoft StringEnumConverter above.
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(StjOrder))]
[JsonSerializable(typeof(StjLine))]
public sealed partial class BenchContext : JsonSerializerContext;
