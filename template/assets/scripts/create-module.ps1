<#
.SYNOPSIS
    Creates a new Module with the specific "Application/Domain/Infrastructure" structure for .NET.

.DESCRIPTION
    This script scaffolds a new module following the modular monolith architecture pattern.
    
    File System Structure:
      {ModulesDir}/Module.{Name}/       -> Contains Api and Implementation projects
      {TestsDir}/Module.{Name}.Tests/   -> Contains Test project (.csproj is here directly)
    
    Solution Structure:
      src   -> Module.{Name} and Module.{Name}.Api
      tests -> Module.{Name}.Tests

.PARAMETER Name
    The name of the module to create (without the "Module." prefix).

.PARAMETER SolutionPath
    Optional path to the solution file. If not provided, the script will auto-detect .sln or .slnx files by searching upwards.

.PARAMETER FrameworkVersion
    Target .NET framework version (e.g., "net10.0", "net9.0", "net8.0"). Default is "net10.0".

.PARAMETER SkipRestore
    Skip running dotnet restore after creating the module.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Name,

    [Parameter(Mandatory=$false)]
    [string]$SolutionPath,

    [Parameter(Mandatory=$false)]
    [string]$FrameworkVersion = "net10.0",

    [Parameter(Mandatory=$false)]
    [switch]$SkipRestore
)

# --- Configuration ---
$prefix = "Module"
$apiSuffix = "Api"
$testsSuffix = "Tests"

$moduleName = "$prefix.$Name"
$apiProjectName = "$moduleName.$apiSuffix"
$testProjectName = "$moduleName.$testsSuffix"

# Framework & Versions
$frameworkVersion = $FrameworkVersion
$vExtensions = "10.0.2"
$vImmutable  = "9.0.0"
$vContracts  = "1.0.0"
$vAnalyzers  = "1.0.0"
$vFluent     = "12.1.1"
$vMapperly   = "4.3.1"

# ----------------------------------------------------------------
# 0. Intelligent Solution Root Discovery
# ----------------------------------------------------------------

function Get-SolutionFile([string]$path) {
    return Get-ChildItem -Path $path -File | Where-Object { 
        ($_.Name -match '\.sln$') -or ($_.Name -match '\.slnx$')
    } | Select-Object -First 1
}

$targetSolutionFile = $null

# Priority 1: User provided path
if (-not [string]::IsNullOrWhiteSpace($SolutionPath)) {
    $resolved = Resolve-Path $SolutionPath -ErrorAction SilentlyContinue
    if ($resolved) { $targetSolutionFile = Get-Item $resolved.Path }
}

# Priority 2: Current Shell Location (PWD) - ONLY if not System32
$currentShellPath = Get-Location
if (-not $targetSolutionFile -and $currentShellPath.Path -ne "C:\Windows\System32") {
    $targetSolutionFile = Get-SolutionFile $currentShellPath.Path
}

# Priority 3: Walk up from Script Location
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
    Write-Host "ERROR: Could not locate a .sln or .slnx file." -ForegroundColor Red
    exit 1
}

$solutionRoot = $targetSolutionFile.DirectoryName
$SolutionPath = $targetSolutionFile.FullName

# Validate module name
$invalidChars = '[^a-zA-Z0-9_]'
if ($Name -match $invalidChars) {
    Write-Host "ERROR: Module name can only contain letters, numbers, and underscores" -ForegroundColor Red
    exit 1
}

Write-Host "Creating module: $Name ($FrameworkVersion)" -ForegroundColor Cyan
Write-Host "Target Solution: $SolutionPath" -ForegroundColor DarkGray

$executionLocation = Get-Location
Set-Location $solutionRoot

try {
    # ----------------------------------------------------------------
    # 1. Setup Directories (Recursive Discovery)
    # ----------------------------------------------------------------
    
    # Helper to find folders while ignoring build artifacts
    function Find-ProjectFolder {
        param([string]$rootPath, [string]$folderName)
        return Get-ChildItem -Path $rootPath -Directory -Recurse -ErrorAction SilentlyContinue | 
            Where-Object { 
                $_.Name -ieq $folderName -and 
                $_.FullName -notmatch '[\\](bin|obj|\.git|\.vs|node_modules)[\\]' 
            } | Select-Object -First 1
    }

    # 1a. Modules Directory
    $existingModules = Find-ProjectFolder -rootPath $solutionRoot -folderName "Modules"
    
    if ($existingModules) {
        $modulesDirectory = $existingModules.FullName
        Write-Host "Found existing Modules directory: $($existingModules.FullName)" -ForegroundColor DarkGray
    } else {
        # Default fallback if not found: create it in the root
        $modulesDirectory = Join-Path $solutionRoot "Modules"
        New-Item -Path $modulesDirectory -ItemType Directory -Force | Out-Null
        Write-Host "Created new Modules directory: $modulesDirectory" -ForegroundColor DarkGray
    }

    # 1b. Tests Directory
    $existingTests = Find-ProjectFolder -rootPath $solutionRoot -folderName "tests"

    if ($existingTests) {
        $testsDirectory = $existingTests.FullName
        Write-Host "Found existing tests directory: $($existingTests.FullName)" -ForegroundColor DarkGray
    } else {
        # Default fallback if not found: create it in the root
        $testsDirectory = Join-Path $solutionRoot "tests"
        New-Item -Path $testsDirectory -ItemType Directory -Force | Out-Null
        Write-Host "Created new tests directory: $testsDirectory" -ForegroundColor DarkGray
    }

    # ----------------------------------------------------------------
    # 2. Create Module Projects (API & Implementation)
    # ----------------------------------------------------------------
    Set-Location $modulesDirectory
    
    if (-not (Test-Path $moduleName)) {
        New-Item -Path $moduleName -ItemType Directory | Out-Null
    }
    Set-Location $moduleName
    $moduleRootPath = Get-Location

    # --- 2a. Create API Project ---
    if (-not (Test-Path "$apiProjectName/$apiProjectName.csproj")) {
        dotnet new classlib -n $apiProjectName --framework $frameworkVersion --no-restore | Out-Null
        New-Item -Path "$apiProjectName/Dto" -ItemType Directory -Force | Out-Null
        Remove-Item "$apiProjectName/Class1.cs" -ErrorAction SilentlyContinue

        $apiCsproj = "$apiProjectName/$apiProjectName.csproj"
        $apiXml = Get-Content $apiCsproj -Raw

        $apiInject = @(
            "  <ItemGroup>",
            "    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""$vExtensions"" />",
            "    <PackageReference Include=""Microsoft.Extensions.Options"" Version=""$vExtensions"" />",        
            "    <PackageReference Include=""Faster.Modulith.Contracts"" Version=""$vContracts"" />",
            "    <PackageReference Include=""Faster.Modulith.Analyzers"" Version=""$vAnalyzers"" OutputItemType=""Analyzer"" ReferenceOutputAssembly=""false"" />",
            "  </ItemGroup>",
            "  <ItemGroup>",
            "    <Folder Include=""Dto\"" />",
            "  </ItemGroup>",
            "</Project>"
        ) -join [Environment]::NewLine

        $closingTag = "</Project>"
        Set-Content -Path $apiCsproj -Value ($apiXml -replace $closingTag, $apiInject)
    }

    # --- 2b. Create Implementation Project ---
    if (-not (Test-Path "$moduleName/$moduleName.csproj")) {
        dotnet new classlib -n $moduleName --framework $frameworkVersion --no-restore | Out-Null

        $folders = @("Domain", "Infrastructure", "Contracts", "Application/UseCases", "Application/CommandHandlers", "Application/EventHandlers")
        foreach ($f in $folders) { New-Item -Path "$moduleName/$f" -ItemType Directory -Force | Out-Null }
        
        Remove-Item "$moduleName/Class1.cs" -ErrorAction SilentlyContinue

        $implCsproj = "$moduleName/$moduleName.csproj"
        $implXml = Get-Content $implCsproj -Raw

        $implInject = @(
            "  <ItemGroup>",
            "    <PackageReference Include=""FluentValidation"" Version=""$vFluent"" />",
            "    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""$vExtensions"" />",
            "    <PackageReference Include=""Microsoft.Extensions.Options"" Version=""$vExtensions"" />",    
            "    <PackageReference Include=""Faster.Modulith.Contracts"" Version=""$vContracts"" />",
            "    <PackageReference Include=""Faster.Modulith.Analyzers"" Version=""$vAnalyzers"" OutputItemType=""Analyzer"" ReferenceOutputAssembly=""false"" />",
            "  </ItemGroup>",
            "  <ItemGroup>",
            "    <Folder Include=""Application\UseCases\"" />",
            "    <Folder Include=""Application\EventHandlers\"" />",
            "    <Folder Include=""Application\CommandHandlers\"" />",
            "    <Folder Include=""Contracts\"" />",
            "    <Folder Include=""Domain\"" />",
            "    <Folder Include=""Infrastructure\"" />",
            "  </ItemGroup>",
            "</Project>"
        ) -join [Environment]::NewLine

        $closingTag = "</Project>"
        Set-Content -Path $implCsproj -Value ($implXml -replace $closingTag, $implInject)
    }

    $apiCsprojPath = Join-Path $moduleRootPath (Join-Path $apiProjectName "$apiProjectName.csproj")
    $implCsprojPath = Join-Path $moduleRootPath (Join-Path $moduleName "$moduleName.csproj")

    # Wire API -> Impl
    dotnet add $implCsprojPath reference $apiCsprojPath > $null

    # ----------------------------------------------------------------
    # 3. Create Test Project
    # ----------------------------------------------------------------
    Set-Location $testsDirectory
    
    if (-not (Test-Path $testProjectName)) {
        New-Item -Path $testProjectName -ItemType Directory | Out-Null
    }
    
    Set-Location $testProjectName
    
    if (-not (Test-Path "$testProjectName.csproj")) {
        dotnet new xunit -n $testProjectName -o . --framework $frameworkVersion --no-restore | Out-Null
    }

    $testCsprojPath = Join-Path $testsDirectory (Join-Path $testProjectName "$testProjectName.csproj")

    # Wire Tests -> API & Impl
    dotnet add $testCsprojPath reference $apiCsprojPath > $null
    dotnet add $testCsprojPath reference $implCsprojPath > $null

    # ----------------------------------------------------------------
    # 4. Add to Solution
    # ----------------------------------------------------------------
    Set-Location $solutionRoot

    dotnet sln $SolutionPath add $apiCsprojPath  --solution-folder "src" > $null
    dotnet sln $SolutionPath add $implCsprojPath --solution-folder "src" > $null
    dotnet sln $SolutionPath add $testCsprojPath --solution-folder "tests" > $null

    Write-Host "Done!" -ForegroundColor Green
    
    if (-not $SkipRestore) {
        dotnet restore $apiCsprojPath --verbosity quiet
        dotnet restore $implCsprojPath --verbosity quiet
        dotnet restore $testCsprojPath --verbosity quiet
    }

} catch {
    Write-Host "ERROR: An error occurred: $_" -ForegroundColor Red
} finally {
    Set-Location $executionLocation
}