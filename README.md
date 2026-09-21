# WinLandPluginSkills

WinIsland（WinLand，Windows 11 灵动岛）的 **AI 技能仓库**。

装上这里的技能后，你直接对 AI 说出需求，它就能带你做出一个灵动岛插件、装进你电脑上的 WinIsland 实测，最后帮你上架社区插件市场——全程不用懂编程。

## 怎么用（两步，全程对话）

### 第 1 步：让 AI 帮你安装技能

把下面这段**原样发给你的 AI 助手**（Claude Code / Command Code 等）：

> 请帮我把这个仓库里的技能装到我的技能目录，装好后告诉我怎么用：
> https://github.com/luolangaga/WinLandPluginSkills

AI 会自己完成：下载仓库 → 把 `winland-plugin-maker` 放进你的技能目录（`~/.agents/skills`，Claude Code 是 `~/.claude/skills`）→ 验证装好了、告诉你可以开始。

不想让 AI 装？手动两行命令：

```powershell
git clone --depth 1 https://github.com/luolangaga/WinLandPluginSkills.git "$env:TEMP\WinLandPluginSkills"
Copy-Item -Recurse "$env:TEMP\WinLandPluginSkills\winland-plugin-maker" "$env:USERPROFILE\.agents\skills\"
```

### 第 2 步：直接说需求

装好之后，像这样聊天就行（不需要提"技能"两个字，AI 会自己认出来）：

> 我想给 WinIsland 灵动岛写一个天气插件，小岛显示当前温度，展开后显示未来三天预报

然后 AI 会自动走完整个流程：

1. **先做环境体检**——检查 .NET 10 SDK、WinIsland 源码、GitHub CLI 等；**缺 .NET 会主动提出帮你装好**
2. 问清插件功能 / 名字 / 署名，从模板搭出项目
3. 写好代码（SDK 2.0 写法，不会抄网上过时的旧教程）
4. 编译并**装进你电脑上已安装的 WinIsland**，让你亲手实测、确认效果
5. 打包成 `.lwp` 插件包
6. **先问你要不要上传**——你同意之后，才帮你上传 GitHub、投稿到社区插件市场（开 PR）

可直接照说的例子：

> 我给灵动岛做了个番茄钟插件，帮我打包成 .lwp 并投稿到插件市场

> 我电脑上没装 .NET，帮我装一下，然后做个显示 CPU 温度的插件

> 我的插件展开动画有点卡，帮我看看哪里有问题

> 帮我在插件的设置页加一个显示开关

**两条硬性规则**（写在技能里，防止 AI 越界）：插件必须装进你实际在用的 WinIsland、由你亲口确认没问题，才算测试通过；任何"发到网上"的动作（建仓库、push、开 PR）都必须先问你同意。

## 仓库里有什么

| 技能 | 说明 |
|------|------|
| [`winland-plugin-maker/`](./winland-plugin-maker/) | 从零做一个 WinIsland 插件并上架社区市场的全流程陪跑 |

技能内部结构（给好奇的人）：

```
winland-plugin-maker/
├── SKILL.md                     ← 全流程主文档：四条铁律 + 7 步陪跑流程
├── references/
│   ├── sdk-api.md               ← SDK 2.0 API 参考（含 1.x 旧写法对照）
│   ├── publish.md               ← GitHub 登录 / 上传源码 / 投稿市场全流程
│   └── troubleshooting.md       ← 编译、加载、动画、打包的排错表
├── assets/plugin-template/      ← 开箱可编译的插件模板工程
└── scripts/
    ├── check-env.ps1            ← 环境体检（.NET / git / gh / 源码）
    └── find-winisland.ps1       ← 自动定位你安装的 WinIsland
```

## 许可

MIT
