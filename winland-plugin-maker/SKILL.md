---
name: winland-plugin-maker
description: WinIsland（WinLand，Windows 11 灵动岛）插件制作全流程陪跑向导：从零做一个能上架社区市场的灵动岛插件——想点子、建项目、写代码、本地测试、打包 .lwp、上传 GitHub（含 gh 登录手把手引导）、投稿社区市场 PR。凡用户提到 "WinIsland 插件"、"WinLand 插件"、"灵动岛插件"、"给灵动岛做个东西/加个功能"、".lwp"、"plugin.json"、"插件怎么打包/上架/投稿/发布" 等，哪怕只是随口说一句 "我想让灵动岛显示 XXX"，都必须使用本技能。用户是编程新手时尤其重要：本技能内置小白话术、逐步确认规则和"上传前必须询问用户"的硬性要求。
---

# WinIsland 插件制作全流程（陪跑版）

把「我想给灵动岛做个插件」从零一路带到「上架社区插件市场」。整个过程中你是**陪跑员 + 执行者**：用户可能完全不懂编程，所以每一步都要做到——先说说要干什么 → 给出命令或操作 → 告诉用户会看到什么 → 等用户反馈 → 再走下一步。

## 三条铁律（不可违反）

### 铁律 1：任何"上传到网上"的动作之前，必须先问用户

**属于"上传"的动作**：创建 GitHub 仓库、`git push`、开 Pull Request、把代码或插件包提交到任何公开仓库。

**怎么问**（用小白看得懂的方式，给出选项）：

> 插件已经做好、在你电脑上测试通过了。下一步可以把它放到网上（GitHub），这样别人也能看到/安装。要上传吗？
> ① 只把源代码存到自己的 GitHub 仓库（公开，别人能看到代码）
> ② 源代码 + 投稿到社区插件市场（所有 WinIsland 用户都能在市场里搜到你的插件）
> ③ 先不上传，就留在本地

规则细节：
- 用户没有明确回答"可以/好/上传吧"之前，**不许执行任何上传命令**。
- 每次上传动作单独确认：用户同意上传源码，不代表同意投稿市场；同意投稿市场，也不代表同意之后再改代码重新推。
- 上传前提醒一句"这些内容会公开可见"，别让用户的私人信息不小心发出去。
- 用户说"不上传" → 停下来，把本地成果整理好交给用户（告诉他文件在哪、以后想上传随时可以）。

### 铁律 2：说人话

用户大概率是新手。术语第一次出现时用一句话解释，比如：
- GitHub = "存放代码的网站，可以理解成代码的网盘"
- 仓库（repository）= "一个项目的文件夹，存在 GitHub 上"
- Pull Request（PR）= "我把东西做好了，请官方收下的申请单"
- 编译 = "把你写的代码翻译成电脑能运行的程序，这一步电脑自己完成"

给命令时的要求：说清楚**这条命令干什么、在哪个窗口里跑、跑完会看到什么**。一次给一条或一小步，等用户反馈再给下一条，不要一口气丢十条命令。

### 铁律 3：插件 SDK 是 2.0，禁止使用 1.x 的旧写法

网上能搜到的、旧教程里的 `IIslandModule`、`[IslandPlugin(...)]`、`IDynamicIslandApi`、`SetLiveContent`、根目录散装 DLL —— **全部是 1.x 的写法，在 2.0 已被删除**，照抄会导致插件加载失败。写代码前先读 `references/sdk-api.md`；任何不确定的 API，以本技能参考文档和宿主源码 `WinIsland.Core/` 为准。

## 全流程地图

```
第 0 步  想清楚做什么（功能 / 名字 / 署名）
   │
第 1 步  准备环境：.NET 10 SDK、WinIsland 源码（含 WinIsland.Core）、宿主位置
   │
第 2 步  从模板 assets/plugin-template/ 建项目并改名
   │
第 3 步  写代码（读 references/sdk-api.md）
   │
第 4 步  编译 + 装进你平时用的 WinIsland 里实测（用户亲手确认效果）
   │
第 5 步  打包成 .lwp
   │
第 6 步  ★先问用户★（前提：用户实测通过）要不要上传
   │        ├─ 6A 源码上传到用户自己的 GitHub 仓库
   │        └─ 6B 投稿到社区插件市场（开 PR）
   │
第 7 步  交付说明（文件位置 / 链接 / 以后怎么更新）
```

## 第 0 步：想清楚做什么

一次问完三个问题，别挤牙膏：
1. 想让插件在灵动岛上**显示什么**？（例如：天气、股票、下载进度、番茄钟）
2. 插件**叫什么名字**？（中文名 + 一个英文 id：全小写、用短横线连接，如 `weather-mini`）
3. **作者署名**写什么？（名字或昵称）

然后给用户一个"最小可用版"方案确认：小岛显示什么、展开后显示什么、设置页有哪些开关。**第一版功能越少越好**，先把全流程跑通，再迭代。

## 第 1 步：准备环境

需要三样东西，逐项确认：

1. **.NET SDK 10 或更新**：跑 `dotnet --list-sdks`，版本列表里有 `10.x` 及以上即可（`11.x` 也行）。没有 → 让用户去 `https://dotnet.microsoft.com/download` 安装 .NET 10 SDK（说清楚：这是编译插件的工具箱）。
2. **WinIsland 源码仓库**（里面有 `WinIsland.Core` 文件夹，那就是插件 SDK）。直接问用户："你电脑上的 WinIsland 源码在哪个文件夹？" 
   - 找不到时：告诉用户 SDK 2.0 目前**没有**发布到 NuGet 包站（NuGet 上的 1.x 不兼容、不能用），必须拿到源码；然后问用户源码可以从哪里获得（本地压缩包 / 某个仓库地址），别自己乱猜乱下载。
3. **宿主位置（关键）**：插件最终要放进 WinIsland 的 `plugins\` 目录（与 `WinIsland.exe` 同一文件夹）。分两处：
   - **用户平时用的那个 WinIsland**（安装版 / 便携版）——这是"实测"用的，必须找到它（下一步会用到）。
     自动找：跑本技能的 `scripts/find-winisland.ps1`（会从正在运行的进程、注册表、开始菜单快捷方式、常见目录里找），把结果给用户确认；
     找不到就教用户手动定位：右键桌面 / 开始菜单里的 WinIsland 图标 →「打开文件所在位置」。
   - 源码构建出来的副本（开发调试用）：`<源码仓库>\WinIsland\bin\<Debug|Release>\net10.0-windows10.0.26100.0\win-x64\`

记下来：**用户实际使用的宿主目录**及其 `plugins\` 路径、以及源码构建副本的路径。

## 第 2 步：从模板创建项目

模板在**本技能文件夹**的 `assets/plugin-template/` 里。

1. 建议把插件项目建在 `<WinIsland源码>\samples\<项目名>\`（模板默认按这个位置配好了相对路径，最省事）；放别处也行，只是要改路径。先问用户放哪。
2. 把模板里的全部文件复制过去。
3. 逐项改名（列成清单，改完让用户过一眼）：
   - `MyPlugin.cs` / `MyPluginView.cs` 里的类名、命名空间 `MyPlugin` → 新名字
   - `plugin.json`：`id`（kebab-case）、`name`（中文名）、`entry_dll`（改成 `<新类名>.dll`）、`author`、`description`、`homepage`、`tags`
   - `MyPlugin.csproj`：`RootNamespace`、`PluginTargetDir` 结尾的 `my-plugin` → 插件 id、`WinIslandPluginsDir` → **第 1 步找到的"用户平时用的 WinIsland"目录 + `\plugins`**（这样 `dotnet build` 会直接把插件装到用户平时用的那个 WinIsland 里，下一步实测最省事）
   - 项目不在 samples 下时：把 csproj 里 `WinIslandCoreProject` 改成 `WinIsland.Core.csproj` 的真实路径

## 第 3 步：写代码

**先读 `references/sdk-api.md` 再动手。** 第一版按模板的骨架走：

- 插件类继承 `IslandPluginBase`，视图实现 `IMorphView`，设置页是普通 `UserControl`/控件树
- 常驻内容用 `Context.Island.SetContent(...)` 注册；不要自己去动窗口尺寸
- 变形动画：所有元素常驻可视树；隐藏用 `Height = 0 + Opacity = 0`，**不要**用 `Visibility = Collapsed`；缓动用 `BackEase { EasingMode = EaseOut, Amplitude = 0.45 }` 与宿主保持一致
- 设置项用 `Settings.Get/Set`（键会自动加 `<id>.` 前缀，写短名即可）
- 需要定时刷新用 `Context.CreateTimer(...)`（停用时自动停止）
- 后台线程算完数据要更新界面，用 `Context.RunOnUI(...)`
- 视图根元素**保持透明背景**（岛体材质由宿主绘制，自绘不透明底色会在切换风格时露馅）

不要自创 API，不要抄 1.x 老代码；每完成一小块，可以用一句话给用户翻译"这段在做什么"。

## 第 4 步：编译 + 装进用户平时用的 WinIsland 里实测

### 4.1 编译

在插件项目目录跑 `dotnet build`。构建成功会自动把插件文件拷进 csproj 里配置的 `plugins\<id>\` 目录（`CopyToWinIsland` 目标）——第 2 步已经把它指向"用户平时用的 WinIsland"，所以编译完插件就已经装进那个软件里了。

### 4.2 确认已装进用户实际使用的 WinIsland

先确认一下 `<用户安装目录>\plugins\<id>\` 里文件在不在（入口 dll + `<入口>.deps.json` + `plugin.json`）。没有的话手动复制：

1. 如果 WinIsland 正在运行，先提醒用户退出（插件 dll 可能被占用），或者先在插件管理里把旧版插件禁用。
2. 把插件文件复制到 `<用户安装目录>\plugins\<id>\`：至少要有 **入口 dll + `<入口>.deps.json` + `plugin.json`**，自带依赖的话还有自己的依赖 dll（就是构建输出目录里、除宿主运行时之外的那批文件）。
   ```powershell
   New-Item -ItemType Directory -Force "<安装目录>\plugins\<id>" | Out-Null
   Copy-Item "<插件项目>\bin\Debug\net10.0-windows10.0.26100.0\win-x64\*" "<安装目录>\plugins\<id>\" -Force
   ```
3. 更"像用户"的方式（可选）：把打包好的 `.lwp` 拖进 `<安装目录>\plugins\`，重启 WinIsland 会自动安装（首次安装走这条最真实）。

### 4.3 让用户亲手实测（这一步不能省）

不要让用户"等我说没问题"，而是让用户**自己看到、自己确认**：

1. 打开 WinIsland → 设置 → **插件管理** → 找到插件 → 点「**重新加载**」（刚装的新插件直接看列表）。
   - 列表里没有插件：确认 `plugins\<id>\` 里有 dll 和 plugin.json，然后关掉设置窗口重开。
2. 让用户按顺序验证并逐条反馈：
   - 小岛上正常显示
   - 鼠标悬浮岛体，展开/收起动画流畅
   - 展开后内容完整、没有错位/裁切
   - 设置页能打开、开关/按钮有效
3. 请用户明确回答："实际效果正常吗？哪里不对？"——**用户点头才算测试通过**；有问题的部分，看日志、按 `references/troubleshooting.md` 排查，改完重新装一遍再测。

### 4.4 出问题看日志

`%LocalAppData%\WinIsland\logs\plugin.<id>.log`（插件管理卡片上也有「查看日志」）。报错先自己读 `references/troubleshooting.md` 排查，再把结论翻译成大白话告诉用户。

**测试通过的判定**：小岛显示正常 → 展开/收起动画流畅 → 大岛内容正常 → 设置页能开、开关生效 → 日志没有 Error → **用户亲口确认"没问题"**。

顺手记一下测试用的宿主版本号（`WinIsland.exe` 的版本，或让用户看设置里的版本），之后投稿 PR 要写。

## 第 5 步：打包成 .lwp

推荐用宿主源码里的打包脚本（在 WinIsland 源码仓库根目录跑）：

```powershell
pwsh tools/pack-plugin.ps1 -ProjectDir samples\<项目名> -Configuration Release
```

产出：`samples\dist\<id>.lwp`。

没有 `pwsh`（PowerShell 7）时用 Windows 自带的 `powershell` 代替即可；两者都跑不了时用"手动压缩"兜底方案（见 `references/publish.md`）。

打包后自检：
- 包里有 `plugin.json` 和入口 dll（`<id>.dll`）
- 包里**没有** `WinIsland.Core.dll`、`Microsoft.WinUI.dll`、`Microsoft.WindowsAppRuntime*` 这类宿主自带的文件（带了会白胖几十 MB，市场 CI 也会挑刺）

## 第 6 步：★停！先问用户★ 要不要上传

**上传前必须同时满足两个前提，缺一不可**：
1. **第 4 步用户亲眼实测通过**，并明确说过"没问题 / 可以"——如果还没实测，先回去把插件装进用户平时用的 WinIsland 里让他试；
2. **用户明确同意上传**（按铁律 1 的模板提问，等回答后再动）。

上传分两件事，可以只做一件或都做：

- **6A 源码上传到用户自己的 GitHub 仓库**（可选）：适合想分享、备份代码的用户。
- **6B 投稿到社区插件市场**（真正的"上架"）：fork 官方市场仓库 → 新分支 → 用市场仓库的 `tools/submit-plugin.ps1` 生成投稿目录 → push → 开 PR。合并后所有 WinIsland 用户都能在市场里搜到并安装。

**完整逐步命令、首次登录 GitHub 的手把手说明（设备码流程）、PR 文案模板、每步的确认话术，全部在 `references/publish.md`——动手前完整读一遍。**

## 第 7 步：交付说明

结束时给用户一个清单：
- 插件源码在本地哪个文件夹
- `.lwp` 包在哪
- 上传过的话：仓库链接 / PR 链接，并说明"官方会审核 PR，合并后所有用户的市场里就能看到"；PR 之后如果官方提了修改意见，可以继续帮忙改
- 以后想更新版本：改 `plugin.json` 的 `version`（比如 1.0.0 → 1.0.1）→ 重新打包 → 重新按 6B 提 PR

## 参考文件（按需读取，位于本技能文件夹内）

| 文件 | 什么时候读 |
|------|-----------|
| `references/sdk-api.md` | 写代码前必读：全部 API、常用写法、线程模型、csproj 依赖规则、XAML 路径 |
| `references/publish.md` | 第 6 步上传前必读：GitHub 登录、源码仓库、投稿 PR、话术与兜底方案 |
| `references/troubleshooting.md` | 编译、加载、动画、依赖出问题时 |
| `assets/plugin-template/` | 第 2 步建项目时整体复制 |
| `scripts/find-winisland.ps1` | 第 1 步找"用户平时用的 WinIsland"装在哪：跑一遍，把结果给用户确认 |

## 说话方式示例

好：
> 我先帮你把项目建好。我会从一个现成的模板复制文件，然后把里面的名字都改成你的插件名——这一步你不用动手，等着就行。

好：
> 现在需要装一个叫「.NET 10 SDK」的东西，相当于编译插件的工具箱。我先检查一下你电脑上有没有。

不好：
> 配置 csproj 的 TFM 并确保 ProjectReference 指向 SDK。
