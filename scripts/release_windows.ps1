param(
    [string]$Version = "",
    [string]$PreviousTag = "",
    [string]$Repo = "flathack/AuthFlow",
    [string[]]$Architectures = @("x64", "arm64"),
    [switch]$SkipBuild,
    [switch]$SkipUpload,
    [switch]$AllowDirty,
    [switch]$Draft,
    [switch]$Prerelease
)

$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ""; Write-Host "== $Message ==" -ForegroundColor Cyan }
function Resolve-RepoRoot { return (Resolve-Path (Join-Path (Split-Path -Parent $PSCommandPath) "..")).Path }
function Assert-Command { param([string]$Name) if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) { throw "Required command not found: $Name" } }
function Normalize-Version { param([string]$Value) $v = $Value.Trim(); if (-not $v) { throw "Version is empty." }; if ($v.StartsWith("v")) { return $v }; return "v$v" }
function Get-AppVersion {
    [xml]$xml = Get-Content -LiteralPath "AutoLogin.App\AutoLogin.App.csproj"
    $value = $xml.Project.PropertyGroup.Version | Select-Object -First 1
    if (-not $value) { throw "Could not read Version from AutoLogin.App\AutoLogin.App.csproj" }
    return [string]$value
}
function Assert-CleanWorktree { if ($AllowDirty) { Write-Host "Dirty worktree allowed by -AllowDirty." -ForegroundColor Yellow; return }; $status = git status --short; if ($status) { throw "Worktree is dirty. Commit or stash changes before releasing:`n$status" } }
function Assert-PathInsideRepo { param([string]$Path) $root = (Resolve-Path ".").Path; $full = [System.IO.Path]::GetFullPath((Join-Path $root $Path)); if (-not $full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) { throw "Refusing to operate outside repository: $full" }; return $full }
function Remove-GeneratedPath { param([string]$Path) $full = Assert-PathInsideRepo $Path; if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force } }
function Normalize-Architectures {
    $items = New-Object System.Collections.Generic.List[string]
    foreach ($arch in $Architectures) { $value = "$arch".Trim().ToLowerInvariant(); if ($value -notin @("x64", "arm64")) { throw "Unsupported architecture: $arch. Use x64 or arm64." }; if (-not $items.Contains($value)) { $items.Add($value) } }
    if ($items.Count -eq 0) { throw "No architectures selected." }
    return @($items)
}
function Get-Runtime { param([string]$Arch) if ($Arch -eq "x64") { return "win-x64" }; if ($Arch -eq "arm64") { return "win-arm64" }; throw "Unsupported architecture: $Arch" }
function Invoke-ReleaseBuild {
    param([string]$Arch, [string]$Tag)
    Write-Step "Publishing Windows $Arch"
    $runtime = Get-Runtime $Arch
    $publishDir = "release\$Tag\publish\$runtime"
    Remove-GeneratedPath $publishDir
    dotnet publish "AutoLogin.App\AutoLogin.App.csproj" -c Release -r $runtime --self-contained false -p:PublishSingleFile=false -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $runtime" }
}
function Get-ReleaseExePath { param([string]$Arch, [string]$Tag) $p = "release\$Tag\publish\$(Get-Runtime $Arch)\AuthFlow.exe"; if (Test-Path -LiteralPath $p) { return (Resolve-Path $p).Path }; throw "Could not find $p" }
function New-ReleaseZip {
    param([string]$Arch, [string]$Tag)
    Write-Step "Creating release ZIP for Windows $Arch"
    $releaseDir = "release\$Tag"
    $runtime = Get-Runtime $Arch
    $packageName = "AuthFlow-$Tag-windows-$Arch"
    $stageDir = Join-Path $releaseDir $packageName
    Remove-GeneratedPath $stageDir
    Copy-Item -LiteralPath (Join-Path $releaseDir "publish\$runtime") -Destination $stageDir -Recurse -Force
    foreach ($file in @("README.md", "LICENSE")) { if (Test-Path -LiteralPath $file) { Copy-Item -LiteralPath $file -Destination (Join-Path $stageDir $file) -Force } }
    $zipPath = Join-Path $releaseDir "$packageName.zip"
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    tar -a -cf $zipPath -C $releaseDir $packageName
    if ($LASTEXITCODE -ne 0) { throw "ZIP creation failed: $zipPath" }
    $hashPath = "$zipPath.sha256"; $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash.ToLowerInvariant()
    Set-Content -LiteralPath $hashPath -Value "$hash  $(Split-Path -Leaf $zipPath)" -Encoding UTF8
    return @((Resolve-Path $zipPath).Path, (Resolve-Path $hashPath).Path)
}
function Get-PreviousTag { param([string]$Tag) if ($PreviousTag.Trim()) { return $PreviousTag.Trim() }; foreach ($t in git tag --sort=-creatordate) { if ($t.Trim() -and $t.Trim() -ne $Tag) { return $t.Trim() } }; return "" }
function New-ReleaseNotes {
    param([string]$Tag, [string]$PrevTag)
    Write-Step "Generating release notes"
    $range = if ($PrevTag) { "$PrevTag..HEAD" } else { "" }
    $notesPath = "release\$Tag\release-notes.md"
    if (-not (Test-Path -LiteralPath (Split-Path -Parent $notesPath))) { New-Item -ItemType Directory -Path (Split-Path -Parent $notesPath) | Out-Null }
    $lines = @("## AuthFlow $Tag", "", "Windows release build.", "", "### Changes")
    $commits = if ($range) { @(git log --reverse --oneline $range) } else { @(git log --reverse --oneline) }
    if ($commits.Count) { foreach ($line in $commits) { $lines += "- ``$line``" } } else { $lines += "- No commit changes were found for this release range." }
    Set-Content -LiteralPath $notesPath -Value $lines -Encoding UTF8
    return (Resolve-Path $notesPath).Path
}
function Assert-ReleasePrerequisites {
    param([string]$Tag)
    Assert-Command "git"; Assert-Command "dotnet"; Assert-Command "tar"
    if (-not $SkipUpload) { Assert-Command "gh"; gh auth status | Out-Null; if ($LASTEXITCODE -ne 0) { throw "GitHub CLI is not authenticated." } }
    if (git tag --list $Tag) { throw "Tag already exists locally: $Tag" }
    if (git ls-remote --tags origin $Tag) { throw "Tag already exists on origin: $Tag" }
    if (-not $SkipUpload) { gh release view $Tag --repo $Repo *> $null; if ($LASTEXITCODE -eq 0) { throw "GitHub release already exists: $Tag" } }
}
function Publish-Release {
    param([string]$Tag, [string]$NotesPath, [string[]]$Assets)
    Write-Step "Publishing GitHub release"
    git tag $Tag; git push origin $Tag
    $args = @("release", "create", $Tag) + $Assets + @("--repo", $Repo, "--title", "AuthFlow $Tag", "--notes-file", $NotesPath)
    if ($Draft) { $args += "--draft" }; if ($Prerelease) { $args += "--prerelease" }
    & gh @args
}

Set-Location (Resolve-RepoRoot)
$appVersion = Get-AppVersion
$tag = Normalize-Version $(if ($Version.Trim()) { $Version } else { $appVersion })
$appTag = Normalize-Version $appVersion
if ($tag -ne $appTag) { throw "Release tag $tag does not match project version $appTag. Update the app version before releasing." }
$prevTag = Get-PreviousTag $tag
$architecturesToBuild = Normalize-Architectures
Write-Step "Preparing AuthFlow release $tag"
Write-Host "GitHub repo: $Repo"
Write-Host "Architectures: $($architecturesToBuild -join ', ')"
Assert-CleanWorktree
Assert-ReleasePrerequisites $tag
if (-not $SkipBuild) { foreach ($arch in $architecturesToBuild) { Invoke-ReleaseBuild $arch $tag } } else { Write-Step "Skipping build by request" }
$assets = @()
foreach ($arch in $architecturesToBuild) { [void](Get-ReleaseExePath $arch $tag); $assets += New-ReleaseZip $arch $tag }
$notes = New-ReleaseNotes $tag $prevTag
if ($SkipUpload) { Write-Step "Skipping upload by request"; Write-Host "Release notes: $notes"; $assets | ForEach-Object { Write-Host "Asset ready: $_" }; exit 0 }
Publish-Release $tag $notes $assets
