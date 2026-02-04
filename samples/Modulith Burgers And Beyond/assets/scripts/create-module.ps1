<#
.SYNOPSIS
    Creates a new Module with the specific "Application/Domain/Infrastructure" structure for .NET.

.DESCRIPTION
    This script scaffolds a new module following the modular monolith architecture pattern.
    
    File System Structure:
      src/modules/Module.{Name}/Module.{Name}/       -> Implementation project
      src/modules/Module.{Name}/Module.{Name}.Api/   -> Api project
      tests/Module.{Name}.Tests/                     -> Test project
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
$vContracts  = "1.0.*"
$vAnalyzers  = "1.0.*"
$vFluent     = "12.1.1"

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
    Write-Host "ERROR: Could not locate a .sln or .slnx file." -ForegroundColor Red
    exit 1
}

$solutionRoot = $targetSolutionFile.DirectoryName
$SolutionPath = $targetSolutionFile.FullName

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
    
    # Create the module folder: src/modules/Module.{Name}
    $moduleParentPath = Join-Path $modulesDirectory $moduleName
    if (-not (Test-Path $moduleParentPath)) {
        New-Item -Path $moduleParentPath -ItemType Directory | Out-Null
    }

    # --- 2a. Create API Project ---
    $apiProjectPath = Join-Path $moduleParentPath $apiProjectName
    if (-not (Test-Path "$apiProjectPath/$apiProjectName.csproj")) {
        dotnet new classlib -n $apiProjectName -o $apiProjectPath --framework $frameworkVersion --no-restore | Out-Null
        New-Item -Path "$apiProjectPath/Dto" -ItemType Directory -Force | Out-Null
        Remove-Item "$apiProjectPath/Class1.cs" -ErrorAction SilentlyContinue

        $apiCsproj = "$apiProjectPath/$apiProjectName.csproj"
        $apiXml = Get-Content $apiCsproj -Raw
        $apiInject = @"
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$vExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="$vExtensions" />        
    <PackageReference Include="Faster.Modulith.Contracts" Version="$vContracts" />
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
    $implProjectPath = Join-Path $moduleParentPath $moduleName
    if (-not (Test-Path "$implProjectPath/$moduleName.csproj")) {
        dotnet new classlib -n $moduleName -o $implProjectPath --framework $frameworkVersion --no-restore | Out-Null

        $folders = @("Domain", "Infrastructure", "Contracts", "Application/UseCases", "Application/CommandHandlers", "Application/EventHandlers")
        foreach ($f in $folders) { New-Item -Path "$implProjectPath/$f" -ItemType Directory -Force | Out-Null }
        Remove-Item "$implProjectPath/Class1.cs" -ErrorAction SilentlyContinue

        $implCsproj = "$implProjectPath/$moduleName.csproj"
        $implXml = Get-Content $implCsproj -Raw
        $implInject = @"
  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="$vFluent" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$vExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="$vExtensions" />    
    <PackageReference Include="Faster.Modulith.Contracts" Version="$vContracts" />
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

    Write-Host "Done!" -ForegroundColor Green
    
    if (-not $SkipRestore) {
        dotnet restore $apiCsprojFile --verbosity quiet
        dotnet restore $implCsprojFile --verbosity quiet
        dotnet restore $testCsprojFile --verbosity quiet
    }

} catch {
    Write-Host "ERROR: An error occurred: $_" -ForegroundColor Red
} finally {
    Set-Location $executionLocation
}