<#
.SYNOPSIS
    Creates a new Module with the specific "Application/Domain/Infrastructure" structure for .NET.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Name,

    [Parameter(Mandatory=$false)]
    [string]$SolutionPath,

    [Parameter(Mandatory=$false)]
    [string]$FrameworkVersion = "net8.0",

    [Parameter(Mandatory=$false)]
    [switch]$SkipRestore,

    [Parameter(Mandatory=$false)]
    [switch]$AspNetCore
)

function Write-Log {
    param(
        [string]$Message,
        [ConsoleColor]$ForegroundColor = "White"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $ForegroundColor
}

# --- Configuration ---
$prefix = "Module"
$apiSuffix = "Api"
$testsSuffix = "Tests"

$moduleName = "$prefix.$Name"
$apiProjectName = "$moduleName.$apiSuffix"
$testProjectName = "$moduleName.$testsSuffix"

# Framework & Versions
$frameworkVersion = $FrameworkVersion
$vExtensions = "9.0.0" 
$vImmutable  = "9.0.0"
$vContracts  = "1.0.*"
$vAnalyzers  = "1.1.*"
$vFluent     = "11.9.0"

# ----------------------------------------------------------------
# 0. Intelligent Solution Root Discovery
# ----------------------------------------------------------------

function Get-SolutionFile([string]$path) {
    return Get-ChildItem -Path $path -File | Where-Object { 
        ($_.Name -match '\.sln$') -or ($_.Name -match '\.slnx$')
    } | Select-Object -First 1
}

$targetSolutionFile = $null

if (-not [string]::IsNullOrWhiteSpace($SolutionPath)) {
    $resolved = Resolve-Path $SolutionPath -ErrorAction SilentlyContinue
    if ($resolved) { $targetSolutionFile = Get-Item $resolved.Path }
}

$currentShellPath = Get-Location
if (-not $targetSolutionFile -and $currentShellPath.Path -ne "C:\Windows\System32") {
    $targetSolutionFile = Get-SolutionFile $currentShellPath.Path
}

if (-not $targetSolutionFile) {
    $searchPath = $PSScriptRoot
    while ($searchPath -and (Split-Path -Parent $searchPath) -ne $searchPath) {
        $found = Get-SolutionFile $searchPath
        if ($found) {
            $targetSolutionFile = $found
            break
        }
        $searchPath = Split-Path -Parent $searchPath
    }
}

if (-not $targetSolutionFile) {
    Write-Log "ERROR: Could not locate a .sln or .slnx file." -ForegroundColor Red
    exit 1
}

$solutionRoot = $targetSolutionFile.DirectoryName
$SolutionPath = $targetSolutionFile.FullName

$invalidChars = '[^a-zA-Z0-9_]'
if ($Name -match $invalidChars) {
    Write-Log "ERROR: Module name can only contain letters, numbers, and underscores" -ForegroundColor Red
    exit 1
}

Write-Log "Creating module: $Name ($FrameworkVersion)" -ForegroundColor Cyan
if ($AspNetCore) { Write-Log "ASP.NET Core Framework Reference: Enabled" -ForegroundColor Cyan }
Write-Log "Target Solution: $SolutionPath" -ForegroundColor DarkGray

$executionLocation = Get-Location
Set-Location $solutionRoot

# Prepare the ASP.NET Core XML block
$aspNetCoreXml = ""
if ($AspNetCore) {
    $aspNetCoreXml = @"
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
"@
}

try {
    # ----------------------------------------------------------------
    # 1. Setup Directories
    # ----------------------------------------------------------------
    
    # 1a. Modules Directory (src/modules)
    $srcDirectory = Join-Path $solutionRoot "src"
    if (-not (Test-Path $srcDirectory)) {
        New-Item -Path $srcDirectory -ItemType Directory -Force | Out-Null
    }
    
    $modulesDirectory = Join-Path $srcDirectory "modules"
    if (-not (Test-Path $modulesDirectory)) {
        New-Item -Path $modulesDirectory -ItemType Directory -Force | Out-Null
    }

    # 1b. Tests Directory
    $testsDirectory = Join-Path $solutionRoot "tests"
    if (-not (Test-Path $testsDirectory)) {
        New-Item -Path $testsDirectory -ItemType Directory -Force | Out-Null
    }

    # ----------------------------------------------------------------
    # 2. Create Module Projects (API & Implementation)
    # ----------------------------------------------------------------

    # --- 2a. Create API Project ---
    $apiProjectPath = Join-Path $modulesDirectory $apiProjectName
    if (-not (Test-Path "$apiProjectPath/$apiProjectName.csproj")) {
        dotnet new classlib -n $apiProjectName -o $apiProjectPath --framework $frameworkVersion --no-restore | Out-Null
        New-Item -Path "$apiProjectPath/Dto" -ItemType Directory -Force | Out-Null
        Remove-Item "$apiProjectPath/Class1.cs" -ErrorAction SilentlyContinue

        $apiCsproj = "$apiProjectPath/$apiProjectName.csproj"
        $apiXml = Get-Content $apiCsproj -Raw
        
        # Inject AspNetCore block + Packages
        $apiInject = @"
$aspNetCoreXml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$vExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="$vExtensions" />  
    <PackageReference Include="Faster.Modulith.Analyzers" Version="$vAnalyzers" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include="Dto\" />
  </ItemGroup>
</Project>
"@
        Set-Content -Path $apiCsproj -Value ($apiXml -replace "</Project>", $apiInject)
    }

    # --- 2b. Create Implementation Project ---
    $implProjectPath = Join-Path $modulesDirectory $moduleName
    if (-not (Test-Path "$implProjectPath/$moduleName.csproj")) {
        dotnet new classlib -n $moduleName -o $implProjectPath --framework $frameworkVersion --no-restore | Out-Null

        $folders = @("Domain", "Infrastructure", "Contracts", "Application/UseCases", "Application/CommandHandlers", "Application/EventHandlers")
        foreach ($f in $folders) { New-Item -Path "$implProjectPath/$f" -ItemType Directory -Force | Out-Null }
        Remove-Item "$implProjectPath/Class1.cs" -ErrorAction SilentlyContinue

        # --- Create {ModuleName}Extensions.cs (IN INFRASTRUCTURE) ---
        $extFilePath = Join-Path $implProjectPath "Infrastructure\${Name}Extensions.cs"
        
        $extContent = @"
using Microsoft.Extensions.DependencyInjection;

namespace ${moduleName}.Infrastructure;

/// <summary>
/// Extension methods for registering module-specific dependencies.
/// </summary>
public static partial class ${Name}Extensions
{
    /// <summary>
    /// Adds infrastructure dependencies.
    /// </summary>
    static partial void AddInfrastructure(IServiceCollection services)
    {
    }
}
"@
        Set-Content -Path $extFilePath -Value $extContent

        # --- Create {ModuleName}Options.cs (IN INFRASTRUCTURE) ---
        $optionsFilePath = Join-Path $implProjectPath "Infrastructure\${Name}Options.cs"
        
        $optionsContent = @"
namespace ${moduleName}.Infrastructure;

/// <summary>
/// Provides configuration options for the ${Name} module.
/// </summary>
public partial class ${Name}Options
{
}
"@
        Set-Content -Path $optionsFilePath -Value $optionsContent

        # --- Create {ModuleName}Endpoints.cs (IN ROOT) (ONLY IF ASP.NET CORE) ---
        if ($AspNetCore) {
            $endpointsPath = Join-Path $implProjectPath "${Name}Endpoints.cs"
            
            $endpointsContent = @"
namespace ${moduleName};

/// <summary>
/// Defines the HTTP endpoints for the ${Name} module.
/// </summary>
public static partial class ${Name}Endpoints
{
    // This partial class is extended by the Source Generator to include Map${Name}Endpoints()
}
"@
            Set-Content -Path $endpointsPath -Value $endpointsContent
        }


        $implCsproj = "$implProjectPath/$moduleName.csproj"
        $implXml = Get-Content $implCsproj -Raw

        # Inject AspNetCore block + Packages
        $implInject = @"
$aspNetCoreXml
  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="$vFluent" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$vExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="$vExtensions" />       
    <PackageReference Include="Faster.Modulith.Analyzers" Version="$vAnalyzers" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include="Application\UseCases\" />
    <Folder Include="Application\EventHandlers\" />
    <Folder Include="Application\CommandHandlers\" />
    <Folder Include="Contracts\" />
    <Folder Include="Domain\" />
    <Folder Include="Infrastructure\" />
  </ItemGroup>
</Project>
"@
        Set-Content -Path $implCsproj -Value ($implXml -replace "</Project>", $implInject)
    }

    # API and Impl Csproj paths for wiring
    $apiCsprojFile = Join-Path $apiProjectPath "$apiProjectName.csproj"
    $implCsprojFile = Join-Path $implProjectPath "$moduleName.csproj"

    # Wire API -> Impl
    dotnet add $implCsprojFile reference $apiCsprojFile > $null

    # ----------------------------------------------------------------
    # 3. Create Test Project
    # ----------------------------------------------------------------
    $testProjectPath = Join-Path $testsDirectory $testProjectName
    
    if (-not (Test-Path "$testProjectPath/$testProjectName.csproj")) {
        dotnet new xunit -n $testProjectName -o $testProjectPath --framework $frameworkVersion --no-restore | Out-Null
    }

    $testCsprojFile = Join-Path $testProjectPath "$testProjectName.csproj"

    # Wire Tests -> API & Impl
    dotnet add $testCsprojFile reference $apiCsprojFile > $null
    dotnet add $testCsprojFile reference $implCsprojFile > $null

    # ----------------------------------------------------------------
    # 4. Add to Solution
    # ----------------------------------------------------------------
    $moduleSolutionFolder = "src\modules\$Name"
    dotnet sln $SolutionPath add $apiCsprojFile  --solution-folder $moduleSolutionFolder > $null
    dotnet sln $SolutionPath add $implCsprojFile --solution-folder $moduleSolutionFolder > $null
    dotnet sln $SolutionPath add $testCsprojFile --solution-folder "tests" > $null

    Write-Log "Done!" -ForegroundColor Green
    
    if (-not $SkipRestore) {
        dotnet restore $apiCsprojFile --verbosity quiet
        dotnet restore $implCsprojFile --verbosity quiet
        dotnet restore $testCsprojFile --verbosity quiet
    }

} catch {
    Write-Log "ERROR: An error occurred: $_" -ForegroundColor Red
} finally {
    Set-Location $executionLocation
}