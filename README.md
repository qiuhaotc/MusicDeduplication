# 🎵 MusicDuplicateFilter - 重复音乐过滤工具

一个基于 WPF (.NET 10) 的重复音乐文件检测与清理工具，帮助你扫描目录、识别重复歌曲，节省存储空间。

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-MVVM-purple)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

🌐 [English](README.en.md)

## ✨ 功能特点

- **📂 文件扫描** — 扫描指定目录及子目录，支持 MP3、FLAC、WAV、OGG、WMA、AAC、M4A 等常见格式
- **🔍 智能重复检测** — 基于文件名、歌曲标题、艺术家、专辑等元数据 + Levenshtein 距离算法进行模糊匹配
- **📊 相似度评分** — 0-100% 的相似度评分系统，可自定义阈值
- **🗑️ 安全删除** — 将重复文件移至回收站（可恢复），删除前需用户确认
- **👁️ 预览功能** — 删除前可预览保留文件和重复文件的详细信息
- **🌐 多语言支持** — 中文 / 英文界面，默认跟随系统语言
- **📝 操作日志** — 记录扫描和删除历史，方便回溯
- **⚙️ 灵活设置** — 自定义扫描目录、扩展名、相似度阈值、文件大小容差等

## 🚀 快速开始

### 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11

### 构建与运行

```bash
# 克隆仓库
git clone <repository-url>
cd MusicDuplicateFilter

# 构建项目
dotnet build

# 运行应用程序
dotnet run --project src/MusicDuplicateFilter

# 运行单元测试
dotnet test
```

## 🏗️ 项目结构

```
MusicDuplicateFilter/
├── MusicDuplicateFilter.slnx          # 解决方案文件
├── src/
│   └── MusicDuplicateFilter/          # WPF 主项目
│       ├── Models/                    # 数据模型
│       │   ├── MusicFileInfo.cs       # 音乐文件信息
│       │   ├── DuplicateGroup.cs      # 重复文件组
│       │   ├── ScanResult.cs          # 扫描结果
│       │   └── AppSettings.cs         # 应用设置
│       ├── Services/                  # 业务服务层
│       │   ├── IFileScanService.cs    # 文件扫描接口
│       │   ├── FileScanService.cs     # 文件扫描实现
│       │   ├── IDuplicateDetector.cs  # 重复检测接口
│       │   ├── DuplicateDetector.cs   # 重复检测实现
│       │   ├── IFileOperationService.cs # 文件操作接口
│       │   ├── FileOperationService.cs  # 文件操作实现
│       │   ├── ILogService.cs         # 日志接口
│       │   ├── LogService.cs          # 日志实现
│       │   ├── ILocalizationService.cs # 本地化接口
│       │   └── LocalizationService.cs  # 本地化实现
│       ├── ViewModels/                # MVVM ViewModel
│       │   ├── ViewModelBase.cs       # ViewModel 基类
│       │   ├── MainViewModel.cs       # 主窗口逻辑
│       │   ├── SettingsViewModel.cs   # 设置窗口逻辑
│       │   └── DuplicatePreviewViewModel.cs # 预览窗口逻辑
│       ├── Views/                     # XAML 视图
│       │   ├── SettingsWindow.xaml    # 设置窗口
│       │   └── DuplicatePreviewWindow.xaml # 预览窗口
│       ├── Helpers/                   # 工具类
│       │   ├── StringSimilarity.cs    # 字符串相似度 (Levenshtein)
│       │   └── MetadataReader.cs      # 音乐元数据读取器
│       ├── Converters/                # WPF 值转换器
│       │   └── Converters.cs
│       └── Resources/                 # 多语言资源
│           ├── Strings.resx           # 中文（默认）
│           └── Strings.en-US.resx     # 英文
└── tests/
    └── MusicDuplicateFilter.Tests/    # 单元测试
        ├── Helpers/
        │   └── StringSimilarityTests.cs
        ├── Services/
        │   └── DuplicateDetectorTests.cs
        └── Models/
            └── ModelTests.cs
```

## 🔧 技术栈

| 技术 | 用途 |
|------|------|
| .NET 10 | 运行时框架 |
| WPF | 桌面 UI 框架 |
| MVVM (CommunityToolkit.Mvvm) | 架构模式 |
| Panuon.WPF.UI | UI 组件库 |
| TagLibSharp | 音乐元数据读取 |
| Microsoft.Extensions.DependencyInjection | 依赖注入 |
| xUnit | 单元测试框架 |

## 📖 使用说明

1. **选择目录** — 点击「添加目录」或输入路径后回车，支持多目录同时扫描
2. **开始扫描** — 点击「开始扫描」，程序会递归扫描目录中的所有音乐文件
3. **查看结果** — 扫描完成后，重复文件会按组显示，标注相似度和建议保留的文件
4. **预览详情** — 点击每组右侧「预览」查看保留文件与重复文件的详细对比
5. **选择删除** — 勾选要删除的重复文件（默认已勾选非保留项），或点击「全选」
6. **导出清单** — 点击「导出 JSON」将重复文件信息导出为带时间戳的 JSON 文件
7. **确认删除** — 点击「删除选中文件」，弹出包含完整文件列表的确认窗口，确认后文件将移至回收站

## 📝 重复检测原理

- **文件名匹配**: 使用 Levenshtein 距离计算文件名相似度，自动过滤常见噪音标签（如 `[320 kbps]`、`(Official Video)` 等）
- **元数据匹配**: 读取 ID3/Vorbis 标签中的标题、艺术家、专辑信息进行比对
- **文件大小**: 可选的字节级大小比较，容差可配置
- **综合评分**: 加权平均各项指标，输出 0-100% 的相似度分数

## 📄 许可证

本项目基于 [MIT License](LICENSE) 开源。
