# PaddleOCR 模型文件

本目录存放PaddleOCR的模型文件和配置文件。

## 目录结构

```
Models/PaddleOCR/
├── ch/                     # 中文模型
│   ├── det_model/          # 检测模型
│   ├── rec_model/          # 识别模型
│   └── cls_model/          # 分类模型 (可选)
├── en/                     # 英文模型
│   ├── det_model/          # 检测模型
│   ├── rec_model/          # 识别模型
│   └── cls_model/          # 分类模型 (可选)
├── ppocr_keys_v1.txt       # 中文字符字典
├── en_dict.txt             # 英文字符字典
└── README.md               # 本说明文件
```

## 使用说明

1. 在应用程序设置中选择"PaddleOCR"引擎
2. 选择识别语言（中文/英文）
3. 程序会自动加载对应语言的模型文件

## 注意事项

- `det_model` 和 `rec_model` 是必需的
- `cls_model` 是可选的，用于文字方向纠正
- 模型文件较大（约8-10MB），首次加载需要时间
- 确保选择的语言有对应的模型文件

## 版本兼容性

- 基于 PaddleOCRSharp 5.0.0.1
- 支持 PaddleOCR v2.0+ 模型
- 推荐使用 PP-OCRv4 模型获得最佳性能 