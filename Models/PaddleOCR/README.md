# PaddleOCR 模型文件说明

此文件夹用于存放PaddleOCR所需的模型文件和配置文件。

## 目录结构（多语言支持）

```
Models/PaddleOCR/
├── ch/                  # 中文模型
│   ├── det_model/       # 中文检测模型
│   ├── rec_model/       # 中文识别模型
│   └── cls_model/       # 中文分类模型 (可选)
├── en/                  # 英文模型
│   ├── det_model/       # 英文检测模型
│   ├── rec_model/       # 英文识别模型
│   └── cls_model/       # 英文分类模型 (可选)
├── det_model/           # 通用检测模型 (向后兼容)
├── rec_model/           # 通用识别模型 (向后兼容)
├── cls_model/           # 通用分类模型 (向后兼容)
├── ppocr_keys_v1.txt    # 中文字符字典文件
├── en_dict.txt          # 英文字符字典文件
└── README.md            # 说明文件 (本文件)
```

## 模型文件安装步骤

### 第一步：解压下载的模型文件
1. 解压 `ch_PP-OCRv4.zip` (中文模型)
2. 解压 `en_PP-OCRv4.zip` (英文模型)

### 第二步：按语言分类复制模型文件

**中文模型 (ch_PP-OCRv4)：**
- 将解压后的检测模型文件复制到：`Models/PaddleOCR/ch/det_model/`
- 将解压后的识别模型文件复制到：`Models/PaddleOCR/ch/rec_model/`
- 如果有分类模型文件，复制到：`Models/PaddleOCR/ch/cls_model/`

**英文模型 (en_PP-OCRv4)：**
- 将解压后的检测模型文件复制到：`Models/PaddleOCR/en/det_model/`
- 将解压后的识别模型文件复制到：`Models/PaddleOCR/en/rec_model/`
- 如果有分类模型文件，复制到：`Models/PaddleOCR/en/cls_model/`

### 第三步：验证文件结构
安装完成后，文件夹结构应该类似：
```
Models/PaddleOCR/
├── ch/
│   ├── det_model/
│   │   ├── inference.pdiparams
│   │   ├── inference.pdiparams.info  
│   │   └── inference.pdmodel
│   ├── rec_model/
│   │   ├── inference.pdiparams
│   │   ├── inference.pdiparams.info
│   │   └── inference.pdmodel
│   └── cls_model/ (可选)
│       ├── inference.pdiparams
│       ├── inference.pdiparams.info
│       └── inference.pdmodel
├── en/
│   ├── det_model/
│   │   ├── inference.pdiparams
│   │   ├── inference.pdiparams.info  
│   │   └── inference.pdmodel
│   ├── rec_model/
│   │   ├── inference.pdiparams
│   │   ├── inference.pdiparams.info
│   │   └── inference.pdmodel
│   └── cls_model/ (可选)
│       ├── inference.pdiparams
│       ├── inference.pdiparams.info
│       └── inference.pdmodel
├── ppocr_keys_v1.txt
└── en_dict.txt
```

### 第四步：在应用程序中配置
1. 启动OCR应用程序
2. 打开设置窗口
3. 选择"PaddleOCR"引擎
4. 选择识别语言（中文/英文）
5. 模型路径会自动设置为对应语言的文件夹
6. 保存设置

## 优势

### ✅ 多语言支持
- 可以同时安装中文和英文模型
- 避免文件名冲突
- 根据选择的语言自动切换模型路径

### ✅ 向后兼容
- 保留原有的通用文件夹结构
- 如果没有按语言分类，仍可使用通用路径

## 推荐模型下载
1. **中文模型 (推荐)**:
   - 检测模型: ch_ppocr_mobile_v2.0_det_infer
   - 识别模型: ch_ppocr_mobile_v2.0_rec_infer
   - 分类模型: ch_ppocr_mobile_v2.0_cls_infer

2. **英文模型**:
   - 检测模型: en_ppocr_mobile_v2.0_det_infer
   - 识别模型: en_ppocr_mobile_v2.0_rec_infer

### 下载地址
- 官方模型库: https://github.com/PaddlePaddle/PaddleOCR/blob/main/doc/doc_ch/models_list.md
- 百度网盘链接 (请查看官方文档)

## 注意事项

- det_model 和 rec_model 是必需的，应用程序无法在没有这些模型的情况下使用PaddleOCR
- cls_model 是可选的，用于文字方向检测和纠正
- 模型文件较大 (通常每个模型 8-10MB)，请确保有足够的磁盘空间
- 首次加载模型时可能需要较长时间，请耐心等待
- 选择不同语言时，应用程序会自动切换到对应的模型路径

## 故障排除

如果遇到PaddleOCR初始化失败的问题：
1. 检查模型文件是否完整下载和解压
2. 确认文件夹结构是否正确
3. 验证模型路径设置是否正确
4. 检查选择的语言是否有对应的模型文件
5. 查看应用程序的调试输出以获取详细错误信息
6. 确保已安装正确的PaddleOCRSharp NuGet包

## 版本兼容性

- 本程序基于 PaddleOCRSharp 进行开发
- 推荐使用 PaddleOCR v2.0+ 的模型文件
- 模型版本应与 PaddleOCRSharp 库版本兼容 