# WinLandPluginSkills

WinIsland（WinLand，Windows 11 灵动岛）的 **AI 技能仓库**：把「怎么做一件事」的完整流程写成技能（Skill），交给 AI 助手（Claude Code / Command Code 等）使用。

## 仓库里有什么

| 技能 | 说明 |
|------|------|
| [`winland-plugin-maker/`](./winland-plugin-maker/) | 从零做一个 WinIsland 插件并上架社区市场的**全流程陪跑**：想点子 → 建项目 → 写代码 → 本地测试 → 打包 `.lwp` → 上传 GitHub（含登录手把手）→ 投稿社区市场 PR。面向编程新手。 |

## 怎么安装

技能就是一个文件夹（里面有 `SKILL.md`）。把技能文件夹放进你的技能目录：

```powershell
# 1. 下载本仓库
git clone --depth 1 https://github.com/luolangaga/WinLandPluginSkills.git "$env:TEMP\WinLandPluginSkills"

# 2. 拷进技能目录（Command Code 等用 ~/.agents/skills；Claude Code 用 ~/.claude/skills）
Copy-Item -Recurse "$env:TEMP\WinLandPluginSkills\winland-plugin-maker" "$env:USERPROFILE\.agents\skills\"
```

装好后直接跟 AI 说：

> 我想给 WinIsland 灵动岛写一个天气插件

就会自动触发 `winland-plugin-maker`（更新技能时把文件夹删掉重新拷一份即可）。

## 技能会做什么

以 `winland-plugin-maker` 为例，AI 会：

1. 问清插件要做什么、叫什么名字、作者署名
2. 检查 / 准备开发环境（.NET 10 SDK、WinIsland 源码里的插件 SDK `WinIsland.Core`）
3. 从模板搭好项目并写好第一版代码（SDK 2.0 写法，不是过时的 1.x）
4. 编译后**装进你电脑上已安装的 WinIsland**（技能会自动帮你找安装目录），让你亲手实测、确认效果，有问题一起排查
5. 打包成 `.lwp` 插件包
6. **先问你要不要上传**——得到同意后，才帮你上传源码到 GitHub、或投稿到社区插件市场开 PR

“上传前必须先实测通过、必须先问用户”是技能里的硬性规则；投稿、登录 GitHub 等步骤都有逐步的中文说明，不需要懂命令行也能跟下来。

## 许可

MIT
