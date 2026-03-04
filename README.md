# ArcGISFlatGeoBuf

ArcGIS Pro で [FlatGeoBuf](https://flatgeobuf.org/) (.fgb) ファイルを直接読み込むための Plugin Datasource + AddIn です。

## 機能

- **カタログペインでの認識**: `.fgb` ファイルがカタログペインに「FlatGeoBuf Feature Class」として表示されます
- **マップへの追加**: ドラッグ＆ドロップ または「データの追加」ダイアログからマップにレイヤーとして追加できます
- **座標参照系の自動認識**: FlatGeoBuf ファイルの CRS 情報（EPSG コード）を読み取り、正しい座標系でレイヤーを作成します
- **フィーチャの選択**: 属性テーブルの表示、フィーチャの選択が可能です
- **空間フィルタ**: 表示範囲に基づいた空間フィルタリングに対応しています

## 対応環境

- ArcGIS Pro 3.2 以降
- .NET 6.0 (Windows)

## プロジェクト構成

```
ArcGISFlatGeoBuf/
├── ArcGISFlatGeoBuf/           # AddIn プロジェクト（カタログ表示・D&D）
│   ├── Config.daml
│   ├── FgbCatalogItem.cs       # カスタムカタログアイテム (CustomItemBase + IMappableItem)
│   ├── FgbPluginDatasource.cs  # Plugin Datasource 実装
│   ├── FgbPluginTableTemplate.cs
│   ├── FgbPluginCursorTemplate.cs
│   ├── FgbGeometryConverter.cs # NTS ↔ ArcGIS ジオメトリ変換
│   └── Module1.cs
└── ArcGISFlatGeoBufPlugin/     # Plugin プロジェクト（Plugin Datasource 登録）
    ├── Config.xml
    └── ArcGISFlatGeoBufPlugin.csproj
```

### 2プロジェクト構成の理由

ArcGIS Pro SDK では Plugin Datasource と AddIn は**別々のパッケージ**として登録する必要があります。

| プロジェクト | PackageType | 登録ファイル | 役割 |
|---|---|---|---|
| `ArcGISFlatGeoBufPlugin` | `Plugin` | `.esriPlugin` | FgbPluginDatasource を Plugin として登録 |
| `ArcGISFlatGeoBuf` | `AddIn` | `.esriAddinX` | FgbCatalogItem でカタログ表示・マップ追加 |

## ビルド・インストール方法

### 前提条件

- Visual Studio 2022 以降
- ArcGIS Pro SDK for .NET 3.2 以降（`D:\Program Files\ArcGIS\Pro\bin\` にインストール済み）

> **注意**: `ArcGISFlatGeoBufPlugin.csproj` および `ArcGISFlatGeoBuf.csproj` 内の ArcGIS Pro のパスを環境に応じて変更してください。

### ビルド手順

1. **Plugin プロジェクトをビルド**（Plugin Datasource の登録）

   ```
   MSBuild ArcGISFlatGeoBufPlugin\ArcGISFlatGeoBufPlugin.csproj /p:Configuration=Debug
   ```

   ビルド後、`%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro3.0\` に `.esriPlugin` が自動展開されます。

2. **AddIn プロジェクトをビルド**（カタログ＆マップ追加機能）

   ```
   MSBuild ArcGISFlatGeoBuf\ArcGISFlatGeoBuf\ArcGISFlatGeoBuf.csproj /p:Configuration=Debug
   ```

   ビルド後、`%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro\` に `.esriAddinX` が自動展開されます。

3. **ArcGIS Pro を起動**

   両ファイルが読み込まれ、`.fgb` ファイルが利用可能になります。

### NuGet パッケージ

- [FlatGeobuf](https://www.nuget.org/packages/FlatGeobuf/) 3.26.0

## 使い方

1. ArcGIS Pro のカタログペインで `.fgb` ファイルが存在するフォルダに移動します
2. `.fgb` ファイルが「FlatGeoBuf Feature Class」アイコンで表示されます
3. ファイルをマップにドラッグ＆ドロップ、または右クリック →「マップに追加」でレイヤーとして表示できます

## 制限事項

- **読み取り専用**: ArcGIS Pro の編集ツール（頂点編集など）は Plugin Datasource の制限により非対応です。編集が必要な場合は QGIS などの外部ツールをご利用ください
- フォルダモード（フォルダパスを指定した場合）ではフォルダ内の全 `.fgb` ファイルを読み込みます

## 依存ライブラリ

- [FlatGeobuf (.NET)](https://github.com/flatgeobuf/flatgeobuf) - FlatGeoBuf 形式の読み書き
- [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite) - ジオメトリ処理
- ArcGIS Pro SDK for .NET

## ライセンス

MIT License
