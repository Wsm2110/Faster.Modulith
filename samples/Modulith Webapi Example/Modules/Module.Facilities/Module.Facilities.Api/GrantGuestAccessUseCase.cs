using System;
using Faster.Modulith.Contracts; // Access to IEvent and IUseCase

// NOTICE: This is the '.Api' project for Facilities.
// It defines the "Language" that other modules use to talk to Facilities.
namespace Module.Facilities.Api;

// ==========================================
// 1. THE EVENT (Output)
// ==========================================

// Defined here so it is visible to everyone
// WHY: This is an "Integration Event". Facilities is shouting "I did something!" (Issued a badge).
// HR or Security modules might be listening. By putting it in the .Api project, they can see it.
// HOW: Primary Constructor syntax (C# 12) makes defining immutable records one-liners.
// This is the preferred way to write simple DTOs.
public record SecurityBadgeIssuedEvent(Guid EmployeeId, string BadgeCode) : IEvent;



// ==========================================
// 2. THE USE CASE (Input)
// ==========================================

/// <summary>
/// A public request to grant access to a visitor.
/// <para>
/// <b>Why exists?</b> This is a "Contract". If the Receptionist App (Web API) wants to 
/// create a guest pass, it must send exactly this object. It acts as a strict agreement 
/// between the UI and the Backend.
/// </para>
/// </summary>
public record GrantGuestAccessUseCase : IUseCase<Result<string>>
{
    // HOW: The constructor enforces that you CANNOT create this request 
    // without providing a Name and Date. This prevents "Invalid State" bugs.
    public GrantGuestAccessUseCase(string visitorName, DateTime dateTime)
    {
        VisitorName = visitorName;
        DateTime = dateTime;

        // BUG ALERT: 'ValidUntil' is never set in the constructor!
        // It will be null (or default). If the handler relies on it, the badge might expire immediately.
    }

    // CRITIQUE: Mutable Property ({ get; set; })
    // A request should be sealed once created. Allowing 'set' means the Name 
    // could change halfway through processing, which is a security risk.
    // Ideally use: public string VisitorName { get; init; }
    public string VisitorName { get; set; }

    // GOOD: Immutable Property ({ get; })
    // This is correct. The entry time shouldn't change.
    public DateTime DateTime { get; }

    // CRITIQUE: Weak Typing (object)
    // 'ValidUntil' is an 'object'. Is it a DateTime? A TimeSpan? A String ("2 hours")?
    // This forces the Handler to guess/cast, which leads to runtime crashes.
    // FIX: Use 'public DateTime ValidUntil { get; init; }'
    public object ValidUntil { get; set; }
}