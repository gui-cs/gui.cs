# FindDuplicateTestMethodsInSameFileName.ps1
param (
    [string]$solutionPath = "."
)

# Set the base path for relative paths (current directory when script is run)
$basePath = Get-Location | Select-Object -ExpandProperty Path

# Function to get relative path (compatible with PowerShell 5.1)
function Get-RelativePath {
    param (
        [string]$basePath,
        [string]$fullPath
    )
    # Ensure paths are absolute and normalized
    $basePath = [System.IO.Path]::GetFullPath($basePath)
    $fullPath = [System.IO.Path]::GetFullPath($fullPath)
    # Calculate relative path using .NET
    if ($fullPath.StartsWith($basePath)) {
        $relative = $fullPath.Substring($basePath.Length).TrimStart('\', '/')
        return $relative
    }
    return $fullPath  # Fallback to full path if not relative
}

# Function to extract method names from a C# file
function Get-TestMethodNames {
    param ($filePath)
    $content = Get-Content -Path $filePath -Raw
    $testMethods = @()

    $methodPattern = '(?s)(\[TestMethod\]|\[Test\]|\[Fact\]|\[Theory\])\s*[\s\S]*?public\s+(?:void|Task)\s+(\w+)\s*\('
    $methods = [regex]::Matches($content, $methodPattern)

    foreach ($match in $methods) {
        $methodName = $match.Groups[2].Value
        if ($methodName) {
            $testMethods += $methodName
        }
    }
    return $testMethods
}

# Collect all test files
$testFiles = Get-ChildItem -Path $solutionPath -Recurse -Include *.cs | 
             Where-Object { $_.FullName -match "Tests" -or $_.FullName -match "Test" }

# Group files by filename
$fileGroups = $testFiles | Group-Object -Property Name

# Dictionary to track method names and their locations, scoped to same filenames
$duplicates = @{}

foreach ($group in $fileGroups) {
    if ($group.Count -gt 1) {
        $fileName = $group.Name
        $methodMap = @{}

        foreach ($file in $group.Group) {
            $methods = Get-TestMethodNames -filePath $file.FullName
            foreach ($method in $methods) {
                if ($methodMap.ContainsKey($method)) {
                    if (-not $duplicates.ContainsKey($method)) {
                        $duplicates[$method] = @($methodMap[$method])
                    }
                    $duplicates[$method] += $file.FullName
                } else {
                    $methodMap[$method] = $file.FullName
                }
            }
        }
    }
}

# Output results with relative paths
if ($duplicates.Count -eq 0) {
    Write-Host "No duplicate test method names found in files with the same name across projects." -ForegroundColor Green
} else {
    Write-Host "Duplicate test method names found in files with the same name across projects:" -ForegroundColor Yellow
    foreach ($dup in $duplicates.Keys) {
        Write-Host "Method: $dup" -ForegroundColor Cyan
        foreach ($fullPath in $duplicates[$dup]) {
            $relativePath = Get-RelativePath -basePath $basePath -fullPath $fullPath
            Write-Host "  - $relativePath" -ForegroundColor White
        }
    }
    Write-Host "Total number of duplicate methods: $($duplicates.Count)" -ForegroundColor Magenta
    exit 1
}