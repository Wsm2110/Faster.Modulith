using Faster.Modulith.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Reports.Api;

/// <summary>
/// Event triggered when a monthly passes report has been successfully generated and archived.
/// </summary>
/// <param name="Month">The month of the generated report.</param>
/// <param name="Year">The year of the generated report.</param>
public record PassesReportGeneratedEvent(int Month, int Year) : IEvent;