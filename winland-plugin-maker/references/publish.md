# 上传到 GitHub 与投稿社区市场（逐步手册）

第 6 步动手前通读本文。**每次上传动作前都必须先问用户（SKILL.md 铁律 1）。**

## 前置条件（缺一不可）

1. 插件已经**装进用户平时用的那个 WinIsland、由用户亲手实测通过**（SKILL.md 第 4 步），用户明确说过"没问题/可以"；
2. 用户**明确同意**这次要做的上传动作。

两条没同时满足，就回到第 4 步 / 先提问，别往下走。

## 0. 先给小白翻译几个词

| 词 | 一句话解释 |
|----|-----------|
| GitHub | 存放代码的网站，可以理解成"代码的网盘"，全世界的开发者都在用 |
| 仓库（repository） | GitHub 上的一个项目文件夹 |
| fork | "把官方仓库复制一份到你自己的账号下"，改你自己的副本不用官方批准 |
| 分支（branch） | 仓库里的一条"平行时间线"，你的改动放在自己的分支上，不影响主版本 |
| push | 把你电脑上的提交"推"到 GitHub |
| Pull Request（PR） | "我把东西做好啦，请官方收下"的申请单；官方看完点合并（Merge）才真正进主仓库 |
| 合并（Merge） | 官方把你的 PR 收进主仓库，从这一刻起所有用户都能看到 |

**两个"上传"要分清楚**：

1. **源码上传**：把插件的源代码放到用户自己的 GitHub 仓库（分享、备份用）——可选
2. **市场投稿**：把打包好的插件提交到官方市场仓库 `luolangaga/WinLandPlugin`（"上架"，别人能在 WinIsland 插件市场里搜到）——这是真正的发布

## 1. 检查 gh 工具与登录状态

```powershell
gh --version          # 有没有安装 GitHub 官方命令行工具
gh auth status        # 有没有登录过
```

- 没装 gh：`winget install --id GitHub.cli`（或让用户去 https://cli.github.com 下载安装），装完**重开终端**再试。
- 已登录（显示 `✓ Logged in to github.com account xxx`）：直接进入下一步。
- 没登录：走下面的设备码登录流程。

## 2. 首次登录 GitHub（设备码方式，手把手）

登录命令必须**用户自己在终端里跑**（它需要交互，AI 代替不了）。给用户的说明按这个顺序来：

**第 1 步**，让用户打开一个新的 PowerShell / 终端窗口，粘贴并回车：

```powershell
gh auth login --web --git-protocol https --hostname github.com
```

**第 2 步**，屏幕上会出现：

```
! First copy your one-time code: XXXX-XXXX
Press Enter to open github.com in your browser...
```

让用户**记下或复制这串 8 位码**，然后按一下回车。

**第 3 步**，浏览器会自动打开 GitHub 授权页（没自动打开就让用户手动访问 `https://github.com/login/device`）：
- 把刚才那串码粘进去 → 点 Continue
- 核对是给 "GitHub CLI" 授权 → 点 **Authorize github**
- 浏览器提示成功后可以关掉

**第 4 步**，回到终端，看到类似输出就算成功：

```
✓ Authentication complete.
✓ Logged in as <用户名>
```

然后 AI 跑一次 `gh auth status` 验证确实登录上了，再继续。

**常见问题**：
- 打开 GitHub 很慢/打不开：多为网络问题，让用户挂代理重试，或在能上网的机器上完成后把 token 拷过来（新手场景建议直接重试）
- 码过期了：重新跑一遍命令，码会换新的
- 登录错账号：`gh auth logout` 后重新登录
- 公司电脑装了 git 但 push 报认证失败：跑一次 `gh auth setup-git` 让 git 用 gh 的凭据

## 3. 方案 A：把源码上传到用户自己的仓库（可选）

**先问用户**："要把源代码存到你的 GitHub 仓库吗？公开的话别人能看到代码。"顺便确认仓库名（默认用插件 id）。

```powershell
cd <插件项目目录>

git init
git add .
git commit -m "feat: 初始版本 <插件中文名>"

# 一条命令搞定：创建仓库 + 关联 + 推送
gh repo create <仓库名> --public --source=. --push
```

- 想私有仓库就把 `--public` 换成 `--private`
- 如果仓库已经建过：`git remote add origin https://github.com/<用户名>/<仓库名>.git` 然后 `git push -u origin main`
- 完成后把 `https://github.com/<用户名>/<仓库名>` 给用户

> 提醒：上传前检查项目里没有 `bin/`、`obj/`（模板自带 `.gitignore` 会排除）、没有个人隐私信息；注意 `.lwp` 包可以不进源码仓库（构建产物），也可以带上方便别人直接下载。

## 4. 方案 B：投稿到社区插件市场（真正的"上架"）

### 4.1 市场仓库长什么样

官方市场仓库：[`luolangaga/WinLandPlugin`](https://github.com/luolangaga/WinLandPlugin)

```
WinLandPlugin/
├── index.json                 ← 自动生成的清单（客户端唯一数据源，别手改）
├── tools/
│   ├── build-index.ps1        ← 扫描 plugins/ 生成 index.json（CI 会跑）
│   └── submit-plugin.ps1      ← 打包 + 校验 + 生成待提交目录（你要用这个）
└── plugins/
    └── <id>/
        ├── plugin.json        ← 必须与 .lwp 包内那份完全一致
        ├── <id>.lwp           ← 插件包，一个目录只允许一个
        ├── logo.png           ← 可选，240×240 透明 PNG
        └── README.md          ← 可选，市场详情页展示
```

投稿就是：在自己的分支上新增 `plugins/<id>/` 目录，然后开 PR。合并后 GitHub Action 自动重建 `index.json`，所有用户刷新市场即可看到。

### 4.2 fork 并克隆市场仓库

**先问用户**："投稿会把插件公开到社区市场（谁都能下载），确认要上传吗？"得到同意后：

```powershell
# 在你想存放市场仓库的文件夹里
gh repo fork luolangaga/WinLandPlugin --clone
cd WinLandPlugin
git checkout -b add-plugin-<id>
```

（fork 有时会花几秒到几十秒，失败就重试一次。）

### 4.3 生成投稿目录

在市场仓库根目录跑官方脚本（它会：打包 Release 输出 → 过滤宿主自带程序集 → 校验清单 → 生成 `plugins/<id>/` → 刷新 `index.json` 和 README 表格）：

```powershell
pwsh tools/submit-plugin.ps1 -SourceDir <插件Release构建输出目录> -Logo <logo.png的路径> -Readme <README.md的路径>
```

- `-SourceDir` 指向插件的 Release 构建输出：`<插件项目>\bin\Release\net10.0-windows10.0.26100.0\win-x64\`（里面要有 `plugin.json` 和入口 dll；没构建过先 `dotnet build -c Release`）
- 没有 logo 就不传 `-Logo`；没有 README 就不传 `-Readme`
- 用不了 `pwsh` 就用 Windows 自带的 `powershell` 跑同名脚本
- 跑完先检查 `plugins\<id>\` 里有 `plugin.json` 和 `<id>.lwp`

**兜底（仓库里没有脚本 / 脚本跑不了）**：手动打包——把 Release 输出目录里的文件（去掉 `*.pdb` 和宿主的 `WinIsland.Core.dll`、`Microsoft.WinUI.dll`、`Microsoft.WindowsAppSDK*` 等）复制到临时目录，确保含 `plugin.json` 和入口 dll，压成 zip 后把扩展名改成 `.lwp`，放进 `plugins\<id>\`，再把 `plugin.json` 一起放进去。

### 4.4 提交并推送

```powershell
git add plugins/<id> index.json README.md
git commit -m "feat: add <id> plugin"
git push -u origin add-plugin-<id>
```

### 4.5 开 Pull Request

```powershell
gh pr create --repo luolangaga/WinLandPlugin --base main --head <你的用户名>:add-plugin-<id> `
  --title "Add plugin <id>" --body "..."
```

PR 正文模板（按 CONTRIBUTING.md 的要求写，小白也看得懂）：

```markdown
## 插件简介
<插件中文名>：<一句话说明它干什么、怎么用>

## 测试环境
- WinIsland 版本：<例如 2.0.0>
- Windows 版本：<例如 Windows 11 24H2>
- 是否需要管理员权限：<不需要 / 需要（说明原因）>

## 权限与数据
- 是否访问网络：<否 / 是（访问哪里、拿什么数据）>
- 读写哪些文件：<例如 只用插件自己的设置键；或说明读写路径>

## 截图（可选）
<贴图或链接>
```

完成后把 PR 链接发给用户，并告诉他：

> 接下来官方会审核这个申请单（PR）。通过后会被合并进主仓库，几分钟后所有 WinIsland 用户的插件市场里就能搜到你的插件了。如果官方留言要求修改，随时告诉我，我帮你改。

### 4.6 如果 CI 校验失败（PR 页面会显示红色 ×）

按报错改，常见的是：
- `version` 不是 `1.2.3` 这样的数字点分格式，或与包内 `plugin.json` 不一致
- `entry_dll` 写了路径、或包内没有这个文件名
- 目录名与 `plugin.json` 的 `id` 不一致
- `id` 格式不对（必须全小写字母/数字/短横线，2~64 字符）

改完 `git add` / `git commit` / `git push`（同一个分支，PR 会自动更新），等 CI 重跑。

## 5. 更新已发布的插件（发新版本）

1. 改代码 → 把 `plugin.json` 的 `version` 提高（如 `1.0.0` → `1.0.1`）
2. `dotnet build -c Release` 重新构建
3. 回到市场仓库（如果上次的本地副本还在）：`git checkout main` → `git pull` → `git checkout -b update-plugin-<id>`
4. 重新跑 `submit-plugin.ps1` 覆盖 `plugins/<id>/`
5. commit → push → 开新 PR

> 版本号必须**只增不减**，客户端靠它判断"有没有新版本"。

## 6. 兜底：完全不用命令行的网页流程

如果用户装不上 gh / 不想用命令行：

1. 浏览器打开 `https://github.com/luolangaga/WinLandPlugin` → 右上角 **Fork** → 创建自己的副本
2. 在自己的 fork 里进入 `plugins/` → **Add file → Upload files**，上传 `plugin.json` + `<id>.lwp`（+ logo/README）——注意 GitHub 网页不能建两层目录后直接放文件，需要先上传后编辑路径，或用 "Create new file" 输入路径 `plugins/<id>/plugin.json` 来建目录
3. 提交后页面会出现 **Contribute → Open pull request**，点进去填标题和正文（用 4.5 的模板）→ **Create pull request**

（流程比命令行版麻烦，但完全不用安装任何工具。）

## 7. 常见问题

**Q：gh 登录时报 "authentication failed"？**
重跑 `gh auth login`；注意别把浏览器里贴码的页面关太早，看到 "Authorize" 才算完成。

**Q：push 时提示没有权限？**
确认推的是**自己 fork 的仓库**而不是官方仓库；确认登录的是自己的 GitHub 账号（`gh auth status`）。

**Q：插件 id 已经被别人占用了？**
换一个 id（比如加个前缀 `yourname-xxx`），`plugin.json`、目录名、`entry_dll` 之外相关的都同步改。

**Q：PR 被官方要求修改怎么办？**
在同一个分支上改文件 → `git add` → `git commit` → `git push`，PR 会自动更新，不需要重开。

**Q：投稿市场会不会收费/审核很久？**
不收费；审核节奏看维护者，插件满足 CONTRIBUTING.md 的规范（能正常安装/启用/禁用/卸载、不打包宿主运行时、说明与行为一致、版本号递增）通常就能过。
