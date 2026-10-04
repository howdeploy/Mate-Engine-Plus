<p align="center"><img src="docs/banner.svg" alt="Mate Engine Plus — a desktop companion for ShojiWM" width="880"></p>
<p align="center"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md"><strong>简体中文</strong></a></p>

# Mate Engine Plus

<table><tr><td><strong>能够理解你的桌面的桌面伴侣。</strong><br>
MateEngine Linux 移植版的非官方后续分支。从源码构建，并针对 ShojiWM
适配：真实的窗口和鼠标坐标、跨显示器与工作区拖动，以及坐在窗口和底部 Dock 上。</td></tr></table>

**先发布源码。** 当前 X3.4 应用源码和集成修复位于 [Linux/](Linux/)。
可下载的干净安装包将在后续阶段单独处理。本次发布不包含作者的私人模型、
音乐、DLC、创意工坊下载、用户配置或从 Steam 恢复的资源。

## 技术栈

| 应用 | 桌面 | Shell | 平台 |
|:--|:--|:--|:--|
| Unity 6000.2.6f2 · C# | ShojiWM · TypeScript IPC | Quickshell · KISA Stack | Linux x86_64 · Xwayland |

本项目派生自 [Marksonthegamer 的 Linux 移植版](https://github.com/Marksonthegamer/Mate-Engine-Linux-Port)，
原作是 [shinyflvre 的 MateEngine](https://github.com/shinyflvre/Mate-Engine)。
这是独立的非官方分支，并非 Steam 官方 Linux 版本。

## 集成功能

- 通过合成器 IPC 支持普通鼠标左键拖动、丢失释放事件后的恢复，以及跨显示器和工作区移动。
- 每帧统一读取窗口与鼠标快照，用于手、头部、眼睛、反应和菜单；隐藏工作区不会响应鼠标。
- 支持坐在应用窗口和底部 Dock 上，包含姿势接触与遮挡处理；顶部栏作为工作区域边界。
- 菜单与提示限制在可见区域内，Big Screen 底部定位与退出，行走和显示器外侧边缘躲藏。
- 恢复 Linux 着色器源码、透明背景、UI Alpha、俄语字体和本地化，并重建内置模型的 Humanoid 数据。
- 保留 Steam 所有权、DLC 检查和创意工坊加载；向桌面音乐组件提供当前舞蹈曲目的元数据。
- 使用 GTK 对话框、原生托盘和 PulseAudio/PipeWire；通过哈希校验修复 Discord 原生管道插件的文件描述符泄漏。

部分模型和姿势仍需单独校准。Wayland 全局键盘检测及多个同时运行的角色仍有限制。
详见[状态与验证范围](docs/STATUS.md)。

## 从源码构建

请先阅读 [X3.4 构建指南](docs/BUILD.md)。当前工作项目由本地合法拥有的 Steam
X3.4 版本恢复而来；构建需要你自己的资源、匹配的依赖和已激活的 Unity。
应用源码已经发布，但新编写的准备流程尚未通过全新克隆构建验证。

仓库根目录保留旧 Linux 3.2 项目的历史。其脚本不代表完整的 3.4 安装流程。
[原移植版 README](docs/UPSTREAM-LINUX.md) 单独保存在文档中。

## 桌面兼容性

使用 [howdeploy/ShojiWM main](https://github.com/howdeploy/ShojiWM/tree/main)，
并采用 [KISA Stack dotfiles](https://github.com/howdeploy/kisa-stack/tree/main/dotfiles)
锁定的提交。参考配置提供 IPC 与 Dock 几何信息。通用输入区域穿透修复已单独提交
[PR #122](https://github.com/bea4dev/ShojiWM/pull/122)。完整集成针对这套 ShojiWM
配置；其他 Wayland 合成器需要自己的适配器。源码发布不会修改正在运行的桌面。

## 文档

| 入门 | 开发与维护 |
|:--|:--|
| [构建指南](docs/BUILD.md) | [源码结构](Linux/README.md) |
| [状态与限制](docs/STATUS.md) | [来源与许可证](docs/PROVENANCE.md) |
| [桌面配置](https://github.com/howdeploy/kisa-stack/tree/main/dotfiles) | [更新记录](CHANGELOG.md) |

## 所有权与许可

使用已购买内容时需运行 Steam。此分支保留所有权检查，不提供或解锁他人的付费资源。
Windows 创意工坊资源包不保证兼容 Linux；本地音乐和 VRM 成功加载并不代表所有模组均可使用。

MateEngine 使用自定义 **MateEngine Pro License**，并非 MIT。
旧 Linux 基础保留[原许可证](LICENSE)，X3.4 应用源码保留
[对应的上游许可证](Linux/licenses/MateEngine-3.4.md)。第三方许可及资源版权仍然适用。
