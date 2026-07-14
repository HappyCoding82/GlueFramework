# Australia Post Calculator API

一个完整的Australia Post邮费计算系统，支持国内和国际邮费计算，包含MyPost Business折扣功能。

## 功能特性

- ✅ **国内邮费计算** - 支持Parcel Post和Express Post
- ✅ **国际邮费计算** - 按国家和分区计算
- ✅ **体积重量计算** - 自动计算实际重量vs体积重量
- ✅ **MyPost Business折扣** - 支持0-5级折扣
- ✅ **扁平包装计费** - 支持Australia Post标准扁平包装
- ✅ **分区定价** - 按邮编自动确定国内分区
- ✅ **完整验证** - 尺寸、重量、包装限制验证
- ✅ **RESTful API** - 完整的REST API接口
- ✅ **Swagger文档** - 自动生成的API文档

## 核心计算规则

### 重量计算
- **体积重量公式**: 长(m) × 宽(m) × 高(m) × 250
- **计费重量**: 实际重量 vs 体积重量，取较大值
- **最大重量**: 国内22kg，国际20kg

### 包装类型
- **扁平包装** (≤5kg): 按尺寸计费，有5种标准尺寸
- **自有包装**: 按计费重量和目的地计费

### 重量分级
- ≤500g
- >500g - ≤1kg  
- >1kg - ≤3kg
- >3kg - ≤5kg
- >5kg - ≤22kg

### MyPost Business折扣等级
- Band 0: 无折扣 (标准零售价)
- Band 1: 5% 折扣
- Band 2: 10% 折扣
- Band 3: 12.5% 折扣
- Band 4: 15% 折扣
- Band 5: 17.5% 折扣

## 快速开始

### 1. 运行API服务

```bash
dotnet run
```

服务将在 `http://localhost:5000` 启动

### 2. 访问Swagger文档

打开浏览器访问 `http://localhost:5000` 查看交互式API文档

## API 端点

### 主要计算端点

#### 计算邮费 (通用)
```http
POST /api/postage/calculate
```

#### 计算国内邮费
```http
POST /api/postage/calculate-domestic
```

#### 计算国际邮费
```http
POST /api/postage/calculate-international
```

### 辅助端点

#### 验证包裹
```http
GET /api/postage/validate
```

#### 获取分区信息
```http
GET /api/postage/zones/domestic
GET /api/postage/zones/international
```

#### 获取重量分级
```http
GET /api/postage/weight-tiers
```

#### 获取MyPost折扣等级
```http
GET /api/postage/mypost-bands
```

## 使用示例

### 国内邮费计算请求

```json
{
  "length": 30,
  "width": 20,
  "height": 10,
  "weight": 2.0,
  "fromPostcode": "2000",
  "toPostcode": "3000",
  "packagingType": "OwnPackaging",
  "serviceType": "ParcelPost",
  "myPostBand": 2,
  "isInternational": false
}
```

### 国际邮费计算请求

```json
{
  "length": 30,
  "width": 20,
  "height": 10,
  "weight": 2.0,
  "destinationCountry": "UNITED STATES",
  "packagingType": "OwnPackaging",
  "serviceType": "ExpressPost",
  "myPostBand": 3,
  "isInternational": true
}
```

### 扁平包装请求

```json
{
  "length": 20,
  "width": 15,
  "height": 5,
  "weight": 1.0,
  "fromPostcode": "2000",
  "toPostcode": "2001",
  "packagingType": "FlatRate",
  "serviceType": "ParcelPost",
  "myPostBand": 1,
  "isInternational": false
}
```

## 响应示例

```json
{
  "success": true,
  "data": {
    "chargeableWeight": 6.0,
    "physicalWeight": 2.0,
    "cubicWeight": 6.0,
    "basePrice": 23.30,
    "discountAmount": 2.33,
    "finalPrice": 20.97,
    "discountPercentage": 10.0,
    "serviceType": "ParcelPost",
    "destinationZone": "N2",
    "weightTier": ">5kg - ≤22kg",
    "isFlatRate": false,
    "myPostBand": 2,
    "calculationSteps": [
      "Physical weight: 2.00kg",
      "Cubic weight: 6.00kg (L30×W20×H10cm × 250)",
      "Chargeable weight: 6.00kg (greater of physical/cubic)",
      "Destination zone: N2 (Interstate - Regional)",
      "Base price: $23.30",
      "MyPost Band 2 discount: 10.0% = -$2.33",
      "Final price: $20.97"
    ]
  },
  "message": "Postage calculated successfully"
}
```

## 项目结构

```
AustraliaPostCalculator/
├── Controllers/
│   └── PostageController.cs      # API控制器
├── Models/
│   ├── ParcelRequest.cs           # 请求模型
│   └── PostageResult.cs           # 响应模型
├── Services/
│   ├── IPostageCalculator.cs      # 计算器接口
│   ├── PostageCalculator.cs       # 主计算器
│   ├── IWeightCalculator.cs       # 重量计算接口
│   ├── WeightCalculator.cs        # 重量计算实现
│   ├── IZoneService.cs            # 分区服务接口
│   ├── ZoneService.cs             # 分区服务实现
│   ├── IPricingService.cs         # 定价服务接口
│   └── PricingService.cs          # 定价服务实现
├── Program.cs                     # 应用程序入口
└── AustraliaPostCalculator.csproj # 项目文件
```

## 运行测试

```bash
dotnet test
```

## 技术栈

- **.NET 8.0** - 主要框架
- **ASP.NET Core** - Web API框架
- **XUnit** - 单元测试框架
- **Moq** - Mock框架
- **Swagger/OpenAPI** - API文档

## 配置说明

### 价格更新
价格数据在 `PricingService.cs` 中的 `InitializeBasePrices()` 方法中定义。要更新价格，请修改此方法中的价格表。

### 分区配置
国内邮编分区在 `ZoneService.cs` 中的 `InitializeDomesticZones()` 方法中定义。

### 国际分区
国际国家分区在 `ZoneService.cs` 中的 `InitializeInternationalZones()` 方法中定义。

## 注意事项

1. **价格准确性**: 示例中的价格为演示价格，实际使用时需要更新为当前Australia Post官方价格
2. **分区规则**: 分区规则基于Australia Post的公开文档，可能需要根据最新政策调整
3. **尺寸限制**: 确保包裹尺寸符合Australia Post的限制要求
4. **重量精度**: 重量计算保留两位小数，符合邮费计算标准

## 许可证

本项目仅供学习和参考使用。
