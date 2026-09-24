---
name: winland-plugin-maker
description: WinIsland（WinLand，Windows 11 灵动岛）插件制作全流程陪跑向导：从零做一个能上架社区市场的灵动岛插件——想点子、建项目、写代码、本地测试、打包 .lwp、上传 GitHub（含 gh 登录手把手引导）、投稿社区市场 PR。凡用户提到 "WinIsland 插件"、"WinLand 插件"、"灵动岛插件"、"给灵动岛做个东西/加个功能"、".lwp"、"plugin.json"、"插件怎么打包/上架/投稿/发布" 等，哪怕只是随口说一句 "我想让灵动岛显示 XXX"，都必须使用本技能。用户是编程新手时尤其重要：本技能内置小白话术、逐步确认规则和"上传前必须询问用户"的硬性要求。
---

# WinIsland 插件制作全流程（陪跑版）

把「我想给灵动岛做个插件」从零一路带到「上架社区插件市场」。整个过程中你是**陪跑员 + 执行者**：用户可能完全不懂编程，所以每一步都要做到——先说说要干什么 → 给出命令或操作 → 告诉用户会看到什么 → 等用户反馈 → 再走下一步。

## 五条铁律（不可违反）

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

网上能搜到的、旧教程里的 `IIslandModule`、`[IslandPlugin(...)]`、`IDynamicIslandApi`、`SetLiveContent`、根目录散装 DLL —— **全部是 1.x 的写法，在 2.0 已被删除**，照抄会导致插件加载失败。写代码前先读 `references/sdk-api.md`；任何不确定的 API，以本技能参考文档和 SDK 里的接口注释（NuGet 包 `luolan.winland.Core`）为准。

`api_version` 是 `2`；之后新增的能力是**增量 API**，靠 `plugin.json` 的 `min_host_version` 做门槛 —— 用到哪个就把门槛提到对应版本：**聚光卡要 `2.1.0`**、**文件投放要 `2.2.0`**。写低了旧宿主会抛 `MissingMethodException`（宿主按插件异常捕获并记日志，功能直接不可用）。

### 铁律 4：构建用的 .NET SDK 不能比宿主新，否则插件必定加载失败

正式版宿主由官方 CI 用 **.NET 10.0.x** 构建，所以插件也必须钉在 .NET 10。

**为什么**：`.csproj` 里没有 SDK 版本，`dotnet build` 会用**这台机器上最高的 SDK**。机器上装了 .NET 11 / 预览版就会用它，插件于是引用 `Microsoft.Windows.SDK.NET 10.0.26100.86`、`WinRT.Runtime 2.3.1.0`，而宿主只提供 `10.0.26100.38` / `2.2.0.0`。这类投影程序集**必须由宿主提供**（不能随插件分发，否则 WinRT 类型身份分裂），而 .NET **不允许向下绑定强命名程序集** —— 插件必定加载失败：报 `FileNotFoundException`；若发生在静态构造里还会被包成 `TypeInitializationException`，看起来像"缺依赖"，其实差的是版本。

**怎么做**：新建插件项目时，项目根目录（与 `.csproj` 同级）必须有 `global.json` 钉住 .NET 10 —— 模板 `assets/plugin-template/` 已经带好这一份，复制过去**不要删、不要改**：

```json
{
  "sdk": {
    "version": "10.0.200",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

- 插件项目恰好放在 WinIsland 源码仓库的 `samples\` 下时，仓库根的 `global.json` 已经覆盖它，但项目里那份留着无害，**照样别删**（插件项目现在已经不要求放在源码仓库里了，放哪都行）。
- 判定方法：在插件项目目录里跑 `dotnet --version`，必须是 `10.x`；是 `11.x` 或带 `-rc`/`-preview` 就说明钉的没生效。
- **必须在插件项目目录里跑构建**：`global.json` 是按**你运行 `dotnet` 时的当前目录**往上找的，跟 `.csproj` 放在哪儿无关。在别处（仓库根、桌面…）用 `dotnet build <路径>` 调用，等于没有 `global.json` —— 会直接挑机器上最新的 SDK（实测：从别的目录构建时，构建日志里出现 `sdk\11.0.100-rc...\Sdks\Microsoft.NET.Sdk`，这正是"本地能跑、装到正式版宿主就加载失败"的成因）。所以：**先 `cd` 进插件目录，再 `dotnet build`**。
- 报错特征（宿主 2.0.x 起会直接说清版本差）：`插件需要 Microsoft.Windows.SDK.NET 10.0.26100.86，宿主提供的是 10.0.26100.38：插件构建用的 .NET / Windows SDK 比宿主新……`
- 看到「缺少依赖程序集：xxx」**先别急着查依赖复制**，先确认 SDK 版本 —— 这是最常见的成因。细节见 `references/sdk-api.md` §12 与 `references/troubleshooting.md`。

### 铁律 5：形态动画只能逐帧赋值，禁止用 Storyboard

插件的形态动画**不能用 `Storyboard`**：属性路径动画（`Storyboard.SetTargetProperty(anim, "Height")`）在动态加载的插件程序集里解析不出类型信息，会在**动画 tick 上**抛 `COMException (0x800F1001): Invalid attribute value Unknown for property Height`。这个异常发生在 tick 里，**调用点的 try/catch 拦不住**，会冒到宿主的未处理异常处理器并打断状态机 —— 岛体尺寸已经收回了、插件元素却卡在中间值、不再响应 hover（表现出来就是**"大岛变小之后卡死"**），累计几次异常还会让插件被自动停用。

- 正确写法：`DispatcherQueueTimer`（16ms）+ 每帧直接给 `Height`/`Opacity`/`FontSize` 赋值，缓动与宿主一致（`BackEase(EaseOut, 0.45)` 的公式）。模板 `assets/plugin-template/MyPluginView.cs` 已经是这个写法，**照抄，不要"优化"成 Storyboard**。
- **纯代码自绘的界面同样适用**，不只是 XAML 视图。宿主内置模块（Media/Battery）能用 Storyboard，是因为它们在宿主程序集里、类型元数据可解析 —— 插件侧没有这个条件。
- 定时器 / 每帧回调里抛的异常同样计入宿主未处理异常计数：能兜住的错误（取数失败之类）自己 `try/catch` 记日志。
- 细节与完整代码：`references/sdk-api.md` §6；现象对照：`references/troubleshooting.md`。

## 全流程地图

```
第 0 步  想清楚做什么（功能 / 名字 / 署名）——可与下面的环境体检并行
   │
第 1 步  环境体检：跑 check-env.ps1，缺 .NET 就帮忙装，确认宿主位置（插件 SDK 从 NuGet 装，不需要源码）
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

> **别让用户干等**：问完上面的问题、等用户回答的同时，就把第 1 步的 `scripts/check-env.ps1` 跑起来——环境体检和问需求并行进行，有缺的东西可以早点告诉用户。

## 第 1 步：环境体检（缺什么补什么）

**先跑本技能的 `scripts/check-env.ps1`**，它会给出一张清单（.NET SDK / git / GitHub CLI / PowerShell / 插件 SDK 是否可达），**再用大白话念给用户听**。然后缺什么补什么：

1. **.NET SDK 10.x**（编译插件的"工具箱"，硬性要求）——缺失就**主动提出帮用户装**：
   - 先征得同意（装软件是改动用户电脑，要说清"装什么、干什么用"）
   - 首选你代跑 `winget install --id Microsoft.DotNet.SDK.10`（弹 UAC 时让用户点"是"；装完**重开终端**才能识别）
   - 没有 winget 或安装失败 → 让用户打开 `https://dotnet.microsoft.com/download/dotnet/10.0` 下载安装包双击安装
   - 装完再跑一次 `dotnet --list-sdks` 验证，**装出了 `10.x` 才能继续**。注意这里是"有 10.x"，**不是"10 或更新"**：用户机器上有 11/预览版不算问题，别让人为了这个去卸 SDK —— 挡住它的是铁律 4 的项目级 `global.json`；但体检结果里出现更高的 SDK 时，**一定要提醒**：项目里那份 `global.json` 不能少。
2. **插件 SDK：不用你操心，从 NuGet 装**。插件用的 SDK 是 NuGet 包 `luolan.winland.Core`（主次版本 = 宿主 API 版本，当前最新 `2.2.1`），第 2 步的模板里已经写好了 —— **不需要 WinIsland 源码，也不用手动下载任何东西**。
   - 这一步唯一可能的坑是网络（还原不动就查 `references/troubleshooting.md` 的「找不到版本为 … 的包」一行；国内网络可能需要代理）。
   - 只有用户明确说「我要改 SDK 本身」或「我要用源码构建的宿主来调试」时，才需要 WinIsland 源码仓库；那时把模板 csproj 里的 `PackageReference` 换成 `ProjectReference`（见 `references/sdk-api.md` §1）。
3. **宿主位置（关键）**：插件最终要放进 WinIsland 的 `plugins\` 目录（与 `WinIsland.exe` 同一文件夹）：
   - **用户平时用的那个 WinIsland**（安装版 / 便携版）——这是"实测"用的，必须找到它（第 4 步要用）。
     自动找：跑 `scripts/find-winisland.ps1`（会从正在运行的进程、注册表、开始菜单快捷方式、常见目录里找），把结果给用户确认；
     找不到就教用户手动定位：右键桌面 / 开始菜单里的 WinIsland 图标 →「打开文件所在位置」。
     安装版默认在 `%LocalAppData%\Programs\WinIsland\plugins`。
   - （可选）如果用户也在跑源码构建的宿主：`<源码仓库>\WinIsland\bin\<Debug|Release>\net10.0-windows10.0.26100.0\win-x64\plugins`。
4. **git / GitHub CLI（gh）**：现在缺了也不影响写插件（体检脚本会列出来），等第 6 步要上传时再补装——装法见 `references/publish.md`。

记下来：**用户实际使用的宿主目录**及其 `plugins\` 路径（第 2 步要写进 csproj）。体检没过就先解决，别急着进第 2 步。

## 第 2 步：从模板创建项目

模板在**本技能文件夹**的 `assets/plugin-template/` 里。

1. **插件项目放哪都行**（推荐 `文档\<插件名>\` 或任意工作目录）—— 模板已经从 NuGet 引用 SDK，不再依赖源码路径、也不用放进 `samples\`。先问用户放哪。
2. 把模板里的全部文件复制过去 —— **连同 `global.json`**（它决定用哪个 SDK 构建，见铁律 4；漏了会在正式版宿主上加载失败）。
3. 逐项改名（列成清单，改完让用户过一眼）：
   - `MyPlugin.cs` / `MyPluginView.cs` 里的类名、命名空间 `MyPlugin` → 新名字
   - `plugin.json`：`id`（kebab-case）、`name`（中文名）、`entry_dll`（改成 `<新类名>.dll`）、`author`、`description`、`homepage`、`tags`
   - `MyPlugin.csproj`：`RootNamespace`、`PluginTargetDir` 结尾的 `my-plugin` → 插件 id、**`WinIslandPluginsDir` → 第 1 步找到的"用户平时用的 WinIsland"目录 + `\plugins`**（这样 `dotnet build` 会直接把插件装进用户平时用的那个 WinIsland，下一步实测最省事；这是**必须改**的一项）
   - `global.json`：**保持原样**，不要改名、不要删（铁律 4 靠它把构建 SDK 钉在 .NET 10）

## 第 3 步：写代码

**先读 `references/sdk-api.md` 再动手。** 第一版按模板的骨架走：

- 插件类继承 `IslandPluginBase`，视图实现 `IMorphView`，设置页是普通 `UserControl`/控件树
- 常驻内容用 `Context.Island.SetContent(...)` 注册；不要自己去动窗口尺寸
- 变形动画：所有元素常驻可视树；隐藏用 `Height = 0 + Opacity = 0`，**不要**用 `Visibility = Collapsed`；**必须逐帧赋值（禁止 Storyboard，见铁律 5）**，缓动与宿主一致：`BackEase(EaseOut, 0.45)`
- 设置项用 `Settings.Get/Set`（键会自动加 `<id>.` 前缀，写短名即可）
- 设置页里的输入框/下拉框：**动作按钮（「立即刷新」之类）里要先提交输入框的值，再干活**（按钮点击早于 LostFocus，否则用的是旧值）；失焦、回车也要提交；值没变就别写设置
- 刷新要"同一时刻只跑一次"时，用 `SemaphoreSlim(1,1)` 排队串行化，**绝不**用"忙就 return"把并发请求静默丢掉（那会让"改完设置点刷新"悄无声息地失败）—— 这两条的完整写法见 `references/sdk-api.md` §15
- 需要定时刷新用 `Context.CreateTimer(...)`（停用时自动停止）
- 后台线程算完数据要更新界面，用 `Context.RunOnUI(...)`
- 视图根元素**保持透明背景**（岛体材质由宿主绘制，自绘不透明底色会在切换风格时露馅）

不要自创 API，不要抄 1.x 老代码；每完成一小块，可以用一句话给用户翻译"这段在做什么"。

**硬性规则：要展示的信息比大岛装得下的更多时，用「超级展开」聚光卡，不要往岛里继续塞元素。**

* 点击岛体 → `Context.Island.OpenSpotlight(new IslandSpotlight { Content = 大卡片视图, Size = …, OnClosed = … })`：
  宿主会让卡片从岛体位置**带倾角飞入、居中放大**，点卡片外区域或按 `Esc` 反向收回。
* **何时打开、卡片多大、里面放什么，全由插件决定**；飞入飞回动画、遮罩、圆角、层级都由宿主负责，插件只给一棵内容树。
* 大卡片视图必须是**独立于岛视图的另一个控件实例**（每个窗口一棵树，共用同一个 `UIElement` 会白屏）。
* 定时器在视图 `Loaded` 里起、`Unloaded` 里停；网络请求之类的收尾放 `OnClosed`。
* `plugin.json` 的 `min_host_version` 要写 `"2.1.0"`（旧宿主没有这个 API）。
* 完整写法、四条细则与坑：`references/sdk-api.md` §16。

**想让插件"接收"拖进来的东西时，用文件投放卡片，不要自己去接拖放事件。**

* 用户从资源管理器 / 浏览器把**文件、文本、图片**拖到岛上时，岛会展开成一排投放卡片，拖到某张卡片上松手就执行它：

  ```csharp
  Context.Island.AddDropTarget(new IslandDropTarget
  {
      Id = "add-to-playlist", Title = "加入播放列表", Glyph = "\uE8C8",
      Kinds = IslandDropKind.Files,                       // 默认只收文件；文本用 Text、图片用 Image
      Extensions = new[] { ".mp3", ".flac" },             // 可选：只对文件生效
      Handler = async context => { await AddAsync(context.Paths); return "已加入"; },
  });
  ```
* 卡片外观、命中、边缘自动滚动、系统拖拽气泡全由宿主负责；`Handler` 在 **UI 线程**被调用，返回的字符串会被宿主当提示弹出来。
* **不匹配的卡片根本不会出现**（不是变暗）：拖文本时只显示收文本的卡片；`Kinds` 与 `Extensions` 一起决定这一点。
* `plugin.json` 的 `min_host_version` 要写 `"2.2.0"`（旧宿主没有这个 API）。
* 完整写法、三种载荷、过滤规则与坑：`references/sdk-api.md` §17。

## 第 4 步：编译 + 装进用户平时用的 WinIsland 里实测

### 4.1 编译

在插件项目目录跑 `dotnet build`（**先 `cd` 进去再跑**：`global.json` 只按当前目录生效，在别处用 `dotnet build <路径>` 会挑到机器上最新的 SDK，见铁律 4）。构建成功会自动把插件文件拷进 csproj 里配置的 `plugins\<id>\` 目录（`CopyToWinIsland` 目标）——第 2 步已经把它指向"用户平时用的 WinIsland"，所以编译完插件就已经装进那个软件里了。

构建前顺手确认一次 `dotnet --version` 是 `10.x`（铁律 4）；不是的话先查项目里的 `global.json` 还在不在 —— 用错 SDK 构建出来的插件在正式版宿主上**一定**加载失败。

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
   - 鼠标悬浮岛体，展开/收起动画流畅；**来回快速 hover，动画一直跟手、不会卡在中间**
   - 展开后内容完整、没有错位/裁切
    - **点击岛体 → 弹出超级大卡片（带倾角飞入、居中放大）；点卡片外区域或按 Esc 能收起、岛体恢复正常**
    - **（注册了投放目标才有）拖东西到岛上：拖文件时卡片里应有"打开 / 所在位置 / 复制路径"加上你插件那张卡；拖一段文字时应只剩收文本的卡片 —— 卡片数量会随载荷类型变。拖到你那张卡片上松手，要真的执行并弹出你返回的提示**
   - 设置页能打开、开关/按钮有效
   - **改完输入框，不点别处、直接点「立即刷新」：要按新值刷**（刷的是旧值 = LostFocus 那个坑，见 §15.1）
   - **改了设置马上点刷新 / 连续快点两次刷新：两次都要真的跑**（日志里能看到两条；只有一条就是请求被"单飞"吞了，见 §15.2）
3. 请用户明确回答："实际效果正常吗？哪里不对？"——**用户点头才算测试通过**；有问题的部分，看日志、按 `references/troubleshooting.md` 排查，改完重新装一遍再测。

### 4.4 出问题看日志

`%LocalAppData%\WinIsland\logs\plugin.<id>.log`（插件管理卡片上也有「查看日志」）。报错先自己读 `references/troubleshooting.md` 排查，再把结论翻译成大白话告诉用户。

看到 `0x800F1001` / `Invalid attribute value Unknown for property Height` 就是形态动画用了 Storyboard（铁律 5）；看到「已忽略调用」说明停用后还有定时器/回调没收干净。

**测试通过的判定**：小岛显示正常 → 展开/收起动画流畅 → 大岛内容正常 → 设置页能开、开关都生效、改了设置马上能看到变化 → 日志没有 Error → **用户亲口确认"没问题"**。

顺手记一下测试用的宿主版本号（`WinIsland.exe` 的版本，或让用户看设置里的版本），之后投稿 PR 要写。

## 第 5 步：打包成 .lwp

推荐用宿主源码里的打包脚本（在 WinIsland 源码仓库根目录跑；**手上有源码就用它，最省事**）：

```powershell
pwsh tools/pack-plugin.ps1 -ProjectDir samples\<项目名> -Configuration Release
```

产出：`samples\dist\<id>.lwp`。

没有 pwsh（PowerShell 7）时用 Windows 自带的 `powershell` 代替即可。**手上没有 WinIsland 源码也没关系**：按 `references/publish.md` 的"手动压缩"兜底方案自己压一个 zip 再把扩展名改成 `.lwp`（包内结构要求见 `references/publish.md`）。

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
| `scripts/check-env.ps1` | 第 1 步开工前先跑：检查 .NET SDK / git / gh / pwsh / 插件 SDK 是否可达，缺什么按提示补 |
| `scripts/find-winisland.ps1` | 第 1 步找"用户平时用的 WinIsland"装在哪：跑一遍，把结果给用户确认 |

## 说话方式示例

好：
> 我先帮你把项目建好。我会从一个现成的模板复制文件，然后把里面的名字都改成你的插件名——这一步你不用动手，等着就行。

好：
> 现在需要装一个叫「.NET 10 SDK」的东西，相当于编译插件的工具箱。我先检查一下你电脑上有没有。

不好：
> 配置 csproj 的 TFM，并从 NuGet 引用插件 SDK。
