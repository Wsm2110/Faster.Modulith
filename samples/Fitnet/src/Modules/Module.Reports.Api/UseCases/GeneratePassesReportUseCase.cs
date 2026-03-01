using System;
using Faster.Modulith.Contracts;

namespace Module.Reports.Api;

/// <summary>
/// Represents the public entry point for generating the passes per month report.
/// </summary>
/// <param name="Month">The target month for the report.</param>
/// <param name="Year">The target year for the report.</param>
public record GenerateReportUseCase(int Month, int Year) : IUseCase<Result>;