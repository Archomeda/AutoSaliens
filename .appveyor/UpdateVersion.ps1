Function RegexReplaceFile($file, $id, $replace, $encoding = "ASCII") {
    $regex = "/\* APPVEYOR_START_$id \*/.*/\* APPVEYOR_END_$id \*/"
    (Get-Content $file -encoding $encoding) -replace $regex,$replace | Set-Content $file -encoding $encoding
}

Function SetVersion() {
    Write-Host "Set version: $env:APPVEYOR_BUILD_VERSION" -ForegroundColor "Yellow"
    RegexReplaceFile -file "AutoSaliens\UpdateChecker.cs" -id "VERSION" -replace $env:APPVEYOR_BUILD_VERSION
}

Function SetBranch() {
    Write-Host "Set branch: $env:APPVEYOR_REPO_BRANCH" -ForegroundColor "Yellow"
    RegexReplaceFile -file "AutoSaliens\UpdateChecker.cs" -id "BRANCH" -replace $env:APPVEYOR_BUILD_VERSION
}

Function SetDate() {
    Write-Host "Set date: $env:APPVEYOR_REPO_COMMIT_TIMESTAMP" -ForegroundColor "Yellow"
    RegexReplaceFile -file "AutoSaliens\UpdateChecker.cs" -id "DATE" -replace $env:APPVEYOR_REPO_COMMIT_TIMESTAMP
}

SetVersion
SetBranch
SetDate
