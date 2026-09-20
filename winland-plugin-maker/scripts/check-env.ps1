<#
.SYNOPSIS
    WinIsland 插件开发「环境体检」：检查 .NET SDK / git / GitHub CLI / PowerShell，给出缺失项的一键安装建议。

.DESCRIPTION
    只做检查，不安装任何东西（安装要先征得用户同意，由 AI 或用户自己执行）。
    每个缺失项都会附上对应的安装命令。

.PARAMETER WinIslandRepo
    可选：WinIsland 源码目录，用来验证里面有没有插件 SDK（WinIsland.Core）。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\check-env.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\check-env.ps1 -WinIslandRepo "C:\src\winland"
#>
param(
    [string]$WinIslandRepo = ""
)

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param([string]$Item, [bool]$Ok, [string]$Detail, [string]$Fix)

    $status = "缺失"
    if ($Ok) { $status = "OK" }

    $results.Add([pscustomobject]@{
        检查项     = $Item
        状态       = $status
        详情       = $Detail
        安装建议   = $Fix
    })
}

# ---- 1) .NET SDK 10 或更新（编译插件的硬性要求）----
$sdkOk = $false
$sdkDetail = "未检测到 dotnet 命令"
try {
    $lines = @(& dotnet --list-sdks 2>$null)
    if ($lines.Count -gt 0) {
        $versions = @($lines | ForEach-Object { ($_ -split ' ')[0] })
        $maxMajor = 0
        foreach ($v in $versions) {
            $major = 0
            [void][int]::TryParse(($v -split '\.')[0], [ref]$major)
            if ($major -gt $maxMajor) { $maxMajor = $major }
        }
        $sdkDetail = "已安装：" + ($versions -join "、")
        if ($maxMajor -ge 10) { $sdkOk = $true }
        else { $sdkDetail += "（版本太旧，需要 10 或更新）" }
    }
}
catch { }
Add-Result ".NET SDK 10+" $sdkOk $sdkDetail "winget install --id Microsoft.DotNet.SDK.10  （装完重开终端；没有 winget 就用 https://dotnet.microsoft.com/download/dotnet/10.0 的安装包）"

# ---- 2) git（源码管理，投稿用得到）----
$gitOk = $false
$gitDetail = "未检测到 git"
try {
    $gitVersion = (& git --version 2>$null)
    if ($gitVersion) { $gitOk = $true; $gitDetail = "$gitVersion" }
}
catch { }
Add-Result "Git" $gitOk $gitDetail "winget install --id Git.Git"

# ---- 3) GitHub CLI（第 6 步上传/投稿时才需要）----
$ghOk = $false
$ghDetail = "未检测到 gh（现在不影响写插件，只影响上传）"
try {
    $ghVersion = (& gh --version 2>$null | Select-Object -First 1)
    if ($ghVersion) { $ghOk = $true; $ghDetail = "$ghVersion" }
}
catch { }
Add-Result "GitHub CLI（gh）" $ghOk $ghDetail "winget install --id GitHub.cli  （装完重开终端）"

# ---- 4) PowerShell 7（pwsh，可选：打包脚本用它最顺）----
$pwshOk = $false
$pwshDetail = "没装 pwsh 也能继续，用自带的 powershell 跑打包脚本即可"
try {
    $pwshVersion = (& pwsh -v 2>$null)
    if ($pwshVersion) { $pwshOk = $true; $pwshDetail = "$pwshVersion" }
}
catch { }
Add-Result "PowerShell 7（pwsh，可选）" $pwshOk $pwshDetail "winget install --id Microsoft.PowerShell"

# ---- 5) WinIsland 源码 / 插件 SDK ----
if ($WinIslandRepo) {
    $core = Join-Path $WinIslandRepo "WinIsland.Core\WinIsland.Core.csproj"
    if (Test-Path $core) {
        Add-Result "WinIsland 源码（含 WinIsland.Core）" $true "已找到：$core" ""
    }
    else {
        Add-Result "WinIsland 源码（含 WinIsland.Core）" $false "路径不对，没找到 $core" "把 -WinIslandRepo 换成真实源码目录，或先获取 WinIsland 源码"
    }
}
else {
    Add-Result "WinIsland 源码（含 WinIsland.Core）" $false "没提供路径（用 -WinIslandRepo 传源码目录可自动验证）" "SDK 2.0 还没发布到 NuGet，必须从 WinIsland 源码引用；先确认源码在哪个文件夹"
}

foreach ($r in $results) {
    if ($r.状态 -eq "OK") {
        Write-Output ("[OK]   {0}：{1}" -f $r.检查项, $r.详情)
    }
    else {
        Write-Output ("[缺失] {0}：{1}" -f $r.检查项, $r.详情)
        if ($r.安装建议) {
            Write-Output ("       → 安装建议：{0}" -f $r.安装建议)
        }
    }
}

Write-Output ""

$missing = @($results | Where-Object { $_.状态 -eq "缺失" })
if ($missing.Count -eq 0) {
    Write-Output "环境体检通过，可以开始做插件了。"
}
else {
    Write-Output ("有 {0} 项缺失/待确认：{1}" -f $missing.Count, (($missing | ForEach-Object { $_.检查项 }) -join "、"))
    Write-Output "硬性要求只有「.NET SDK 10+」和「WinIsland 源码」；gh 等第 6 步要上传时再补也行。"
}
