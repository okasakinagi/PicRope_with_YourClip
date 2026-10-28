param(
    [switch]$SkipBuild,
    [switch]$SkipFetch,
    [string]$Branch,
    [string]$UpstreamRef,
    [string]$Remote = 'upstream'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    if (-not $UpstreamRef) { $UpstreamRef = "$Remote/main" }

    $remotes = @(& git remote)
    if ($remotes -notcontains $Remote) {
        throw "没有名为 $Remote 的远程。先执行：git remote add $Remote https://github.com/Renly1994/PicRope.git"
    }

    $current = (& git rev-parse --abbrev-ref HEAD).Trim()
    if (-not $Branch) { $Branch = $current }
    if ($Branch -eq 'main' -or $Branch -eq 'master') {
        throw "工作分支不能是 $Branch —— main 只做上游镜像，你的改动应放在自己的分支上。"
    }

    $dirty = @(& git status --porcelain --untracked-files=no)
    if ($dirty.Count -gt 0) {
        throw ("工作区有未提交的改动，请先提交或 stash：`n" + ($dirty -join "`n"))
    }

    Write-Output "同步 $Branch ← $UpstreamRef"
    if (-not $SkipFetch) {
        & git fetch $Remote --prune --tags 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "无法访问远程 $Remote，检查网络后重试（或加 -SkipFetch 用本地已有的引用）。" }
    }

    $before = (& git rev-parse main).Trim()
    $target = (& git rev-parse $UpstreamRef).Trim()
    if ($before -eq $target) {
        Write-Output ('已是最新：main 与 ' + $UpstreamRef + ' 都是 ' + $target.Substring(0, 7) + '，无需更新。')
        exit 0
    }

    $incoming = @(& git log --oneline --no-decorate "$before..$target")
    $changed = @(& git diff --name-only $before $target)
    $ours = @(& git diff --name-only main "refs/heads/$Branch")
    $overlap = @($changed | Where-Object { $ours -contains $_ })

    Write-Output ('上游新增 ' + $incoming.Count + ' 个提交，涉及 ' + $changed.Count + ' 个文件：')
    $incoming | ForEach-Object { Write-Output ('  ' + $_) }
    Write-Output ''
    if ($overlap.Count -gt 0) {
        Write-Output '注意：以下文件同时被上游和你的改动修改，可能要手工处理：'
        $overlap | ForEach-Object { Write-Output ('  ' + $_) }
    }
    else {
        Write-Output '你的改动文件未被上游修改，预计无冲突。'
    }
    if ($changed -contains 'packaging/prepare-assets.ps1') {
        Write-Output '提醒：资源管线 prepare-assets.ps1 有变动。若缩放尺寸不再是 384，需同步调整 src\ClipArt.cs 的比例常量。'
    }
    Write-Output ''

    & git checkout main --quiet
    & git merge --ff-only $UpstreamRef --quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & git checkout $Branch --quiet
        throw 'main 无法快进（main 上可能有你自己的提交）。请先处理 main 分支。'
    }

    & git checkout $Branch --quiet
    & git rebase main --quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        $conflicted = @(& git diff --name-only --diff-filter=U)
        & git rebase --abort 2>&1 | Out-Null
        & git checkout main --quiet
        & git reset --hard $before --quiet
        & git checkout $Branch --quiet
        Write-Output '合并出现冲突，已回滚到更新前的状态。'
        Write-Output '冲突文件：'
        $conflicted | ForEach-Object { Write-Output ('  ' + $_) }
        Write-Output ''
        Write-Output '这属于需要人工判断的情况：先看上游怎么改的，再把你的改动重新套一遍。'
        exit 1
    }
    Write-Output '已重放你的改动，无冲突。'

    if ($SkipBuild) { Write-Output '（已跳过构建）'; exit 0 }

    $exe = Join-Path $root 'build\PicRope.exe'
    if ((Test-Path -LiteralPath $exe) -and (Get-Process PicRope -ErrorAction SilentlyContinue)) {
        Write-Output '关闭正在运行的程序…'
        & $exe --quit 2>&1 | Out-Null
        Start-Sleep -Seconds 2
    }
    Write-Output '开始构建…'
    & (Join-Path $root 'build.ps1')
    if ($LASTEXITCODE -ne 0) {
        Write-Output ''
        Write-Output '构建或测试失败。合并结果已保留，可检查后重跑 build.ps1。'
        Write-Output ('要回到更新前：git checkout ' + $Branch + '; git reset --hard ' + $before)
        exit 1
    }

    Write-Output ''
    Write-Output '同步完成，构建通过。推送：'
    Write-Output ('  git push --force-with-lease origin ' + $Branch)
    exit 0
}
finally { Pop-Location }
