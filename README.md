# ArcGISFlatGeoBuf

[日本語版 README はこちら](README-ja.md)

A Plugin Datasource + AddIn for reading [FlatGeoBuf](https://flatgeobuf.org/) (.fgb) files directly in ArcGIS Pro.

## Features

- **Catalog recognition**: `.fgb` files are shown in the Catalog pane as a "FlatGeoBuf Feature Class"
- **Add to map**: add the file as a layer via drag & drop, or through the "Add Data" dialog
- **Automatic CRS detection**: reads the CRS information (EPSG code) from the FlatGeoBuf file and creates the layer with the correct spatial reference
- **Feature selection**: view attribute tables and select features
- **Spatial filter**: supports spatial filtering based on the current map extent
- **Localization**: UI strings are available in English and Japanese (`Config.daml` / `Config.ja.daml`, `Strings.resx` / `Strings.ja.resx`)

## Supported environment

- ArcGIS Pro 3.7 or later
- .NET 10.0 (Windows)

## Project structure

```
ArcGISFlatGeoBuf/
├── ArcGISFlatGeoBuf/           # AddIn project (catalog display, drag & drop)
│   ├── Config.daml
│   ├── Config.ja.daml
│   ├── FgbCatalogItem.cs       # Custom catalog item (CustomItemBase + IMappableItem)
│   ├── FgbPluginDatasource.cs  # Plugin Datasource implementation
│   ├── FgbPluginTableTemplate.cs
│   ├── FgbPluginCursorTemplate.cs
│   ├── FgbGeometryConverter.cs # NTS <-> ArcGIS geometry conversion
│   └── Module1.cs
└── ArcGISFlatGeoBufPlugin/     # Plugin project (Plugin Datasource registration)
    ├── Config.xml
    └── ArcGISFlatGeoBufPlugin.csproj
```

### Why two projects?

The ArcGIS Pro SDK requires the Plugin Datasource and the AddIn to be registered as **separate packages**.

| Project | PackageType | Registration file | Role |
|---|---|---|---|
| `ArcGISFlatGeoBufPlugin` | `Plugin` | `.esriPlugin` | Registers `FgbPluginDatasource` as a Plugin |
| `ArcGISFlatGeoBuf` | `AddIn` | `.esriAddinX` | `FgbCatalogItem` for catalog display and adding to map |

## Build & install

### Prerequisites

- Visual Studio 2026 or later
- ArcGIS Pro SDK for .NET 3.7 or later (installed under `D:\Program Files\ArcGIS\Pro\bin\`)

> **Note**: update the ArcGIS Pro paths in `ArcGISFlatGeoBufPlugin.csproj` and `ArcGISFlatGeoBuf.csproj` to match your environment.

### Build steps

1. **Build the Plugin project** (registers the Plugin Datasource)

   ```
   MSBuild ArcGISFlatGeoBufPlugin\ArcGISFlatGeoBufPlugin.csproj /p:Configuration=Debug
   ```

   After building, the `.esriPlugin` is automatically deployed to `%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro3.0\`.

2. **Build the AddIn project** (catalog and add-to-map features)

   ```
   MSBuild ArcGISFlatGeoBuf\ArcGISFlatGeoBuf\ArcGISFlatGeoBuf.csproj /p:Configuration=Debug
   ```

   After building, the `.esriAddinX` is automatically deployed to `%USERPROFILE%\Documents\ArcGIS\AddIns\ArcGISPro\`.

3. **Start ArcGIS Pro**

   Both files are loaded and `.fgb` files become available.

### NuGet packages

- [FlatGeobuf](https://www.nuget.org/packages/FlatGeobuf/) 3.26.0

## Usage

1. In the ArcGIS Pro Catalog pane, navigate to the folder containing your `.fgb` file
2. The `.fgb` file is shown with a "FlatGeoBuf Feature Class" icon
3. Drag & drop the file onto the map, or right-click it and choose "Add To Current Map" to display it as a layer

## Limitations

- **Read-only**: ArcGIS Pro editing tools (such as vertex editing) are not supported due to Plugin Datasource limitations. Use an external tool such as QGIS if editing is required
- In folder mode (when a folder path is specified), all `.fgb` files in the folder are loaded

## Dependencies

- [FlatGeobuf (.NET)](https://github.com/flatgeobuf/flatgeobuf) - reading/writing the FlatGeoBuf format
- [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite) - geometry processing
- ArcGIS Pro SDK for .NET

## License

MIT License
