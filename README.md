# YTOCR - 智能截图OCR识别工具

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

一个基于 WPF 的 OCR 应用程序，支持快捷键截图和多引擎文字识别。

## 主要特性

-  **快捷键截图**：自定义全局热键，快速启动截图
-  **多引擎支持**：PaddleOCR / Tesseract / Windows OCR
-  **智能剪贴板**：自动复制识别结果和截图
-  **系统托盘**：最小化到托盘运行

## 技术栈

- .NET 9.0 + WPF
- PaddleOCRSharp 5.0.0.1
- Tesseract 5.2.0
- Hardcodet.NotifyIcon.Wpf

## 安装与运行

### 系统要求
- Windows 10/11 (x64)
- .NET 9.0 Runtime

### 运行步骤
```bash
git clone https://github.com/ytdttj/OCR.git
cd OCR
dotnet restore
dotnet build
dotnet run
```

## 使用说明

1. **启动**：运行程序后最小化到系统托盘
2. **截图**：使用快捷键（默认 Ctrl+Shift+A）启动截图
3. **选择区域**：拖拽鼠标选择需要识别的区域
4. **查看结果**：识别完成后弹出结果窗口，自动复制到剪贴板

### 设置
- 右键托盘图标 → 设置
- 可配置热键、OCR引擎、语言等

## 使用的开源项目

- [PaddleOCR](https://github.com/PaddlePaddle/PaddleOCR) - 百度飞桨 OCR 工具
- [PaddleOCRSharp](https://github.com/raoyutian/PaddleOCRSharp) - PaddleOCR 的 .NET 封装库
- [Tesseract](https://github.com/tesseract-ocr/tesseract) - Google 开源 OCR 引擎
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) - WPF 系统托盘控件

## 许可证

本项目采用 [MIT 许可证](LICENSE)。 
