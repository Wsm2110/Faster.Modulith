using Faster.Modulith.Contracts; // Access to IUseCase
using System;
using System.Collections.Generic;
using System.Text;

// NOTICE: This is in '.Api'. 
// It defines the Input Contract for the "Hire Employee" action.
namespace Module.HumanResources.Api;

/// <summary>
/// A public request object representing the intent to hire a new employee.
/// <para>
/// <b>Why exists?</b> This serves as the <i>Contract</i> between the UI (or other modules) and HR.
/// By defining this strictly, we ensure we get exactly the data we need (Name, Department) 
/// before we start the hiring workflow.
/// </para>
/// </summary>
/// <remarks>
/// <b>Primary Constructor:</b> We use the modern C# 12 syntax to declare inputs inline.
/// <c>public record HireEmployeeUseCase(string firstName...)</c>
/// </remarks>
public record HireEmployeeUseCase(string firstName, string lastName, Guid departmentId) : IUseCase<Result<Guid>>
{
    // CRITIQUE: Public Field.
    // Ideally, avoid public fields in contracts. Use properties ({ get; }) instead.
    // Also, 'Name' is redundant if we already have First/Last name. 
    // This creates ambiguity: Should the handler use 'Name' or 'FirstName'?
    public string Name;

    // CRITIQUE: Inconsistent Data Types & Naming.
    // The constructor asks for 'departmentId' (Guid), but here we have a property 'DeptId' (ulong).
    // 1. Naming: 'DeptId' vs 'DepartmentId'. Stick to one!
    // 2. Type: 'ulong' vs 'Guid'. You cannot fit a Guid into a ulong. This will cause mapping errors.
    public ulong DeptId { get; set; }

    // GOOD: Immutable Properties.
    // These capture the values passed into the primary constructor and make them 
    // accessible as Read-Only properties.
    public string FirstName { get; } = firstName;
    public string LastName { get; } = lastName;

    // GOOD: Consistent usage of Guid for IDs.
    public Guid DepartmentId { get; } = departmentId;
}