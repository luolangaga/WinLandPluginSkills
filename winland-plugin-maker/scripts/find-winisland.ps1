<#
.SYNOPSIS
    查找本机安装/运行的 WinIsland，输出候选安装目录（含 plugins 路径）。

.DESCRIPTION
    依次从这些地方找：
      1. 正在运行的 WinIsland 进程
      2. 注册表 App Paths
      3. 卸载记录（安装版）
      4. 开始菜单快捷方式
      5. 常见的安装目录
    找到的每个目录都会检查 WinIsland.exe 是否存在，并给出 plugins 子目录的位置。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\find-winisland.ps1
#>

$results = New-Object System.Collections.Generic.List[object]

function Add-Candidate {
    param([string]$Path, [string]$Source)

    if ([string]::IsNullOrWhiteSpace($Path)) { return }

    $dir = $Path.TrimEnd('\')
    $exe = Join-Path $dir "WinIsland.exe"
    if (-not (Test-Path $exe)) { return }

    $plugins = Join-Path $dir "plugins"
    $results.Add([pscustomobject]@{
        来源        = $Source
        安装目录    = $dir
        Exe版本     = (Get-Item $exe).VersionInfo.FileVersion
        plugins目录 = if (Test-Path $plugins) { $plugins } else { "（还没有 plugins 目录，安装插件时宿主会自动创建）" }
    })
}

# 1) 正在运行的进程
Get-Process WinIsland -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Path) { Add-Candidate (Split-Path $_.Path -Parent) "正在运行的进程" }
}

# 2) 注册表 App Paths
foreach ($key in @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinIsland.exe",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinIsland.exe")) {
    $value = (Get-ItemProperty -Path $key -ErrorAction SilentlyContinue).'(default)'
    if ($value) { Add-Candidate (Split-Path $value -Parent) "注册表 App Paths" }
}

# 3) 卸载记录（安装版）
foreach ($root in @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*")) {
    Get-ItemProperty -Path $root -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -like "*WinIsland*" } |
        ForEach-Object { Add-Candidate $_.InstallLocation "安装记录（$($_.DisplayName)）" }
}

# 4) 开始菜单快捷方式
$shell = New-Object -ComObject WScript.Shell
Get-ChildItem "$env:ProgramData\Microsoft\Windows\Start Menu\Programs", "$env:APPDATA\Microsoft\Windows\Start Menu\Programs" `
    -Recurse -Filter "*.lnk" -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "*WinIsland*" } |
    ForEach-Object {
        $target = $shell.CreateShortcut($_.FullName).TargetPath
        if ($target) { Add-Candidate (Split-Path $target -Parent) "开始菜单快捷方式" }
    }

# 5) 常见安装目录
foreach ($dir in @(
        "$env:LOCALAPPDATA\Programs\WinIsland",
        "$env:ProgramFiles\WinIsland",
        "${env:ProgramFiles(x86)}\WinIsland")) {
    Add-Candidate $dir "常见安装目录"
}

$unique = $results | Sort-Object 安装目录 -Unique
if ($unique) {
    $unique | Format-Table -AutoSize
}
else {
    Write-Output "没有自动找到 WinIsland。"
    Write-Output "请手动确认：右键桌面 / 开始菜单里的 WinIsland 图标 →「打开文件所在位置」，把那个文件夹路径记下来。"
}
