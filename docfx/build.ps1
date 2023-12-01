# Builds the Terminal.gui API documentation using docfx

$prevPwd = $PWD; Set-Location -ErrorAction Stop -LiteralPath $PSScriptRoot

try {
    $PWD  # output the current location 

    dotnet build --configuration Release ../Terminal.sln

    rm ../docs -Recurse -Force -ErrorAction SilentlyContinue
    rm api -Recurse -Force -ErrorAction SilentlyContinue

    $env:DOCFX_SOURCE_BRANCH_NAME="v2_develop"

    docfx --serve --force
}
finally {
  # Restore the previous location.
  $prevPwd | Set-Location
}

