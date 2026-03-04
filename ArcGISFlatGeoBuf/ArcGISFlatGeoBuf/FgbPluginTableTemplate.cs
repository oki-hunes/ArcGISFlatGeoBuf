using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using FlatGeobuf;
using FlatGeobuf.NTS;
using ArcGISGeometryType = ArcGIS.Core.Geometry.GeometryType;
using ArcGISGeometry = ArcGIS.Core.Geometry.Geometry;

namespace ArcGISFlatGeoBuf
{
    /// <summary>
    /// 1つの .fgb ファイルに対応する PluginTableTemplate 実装。
    /// </summary>
    public class FgbPluginTableTemplate : PluginTableTemplate
    {
        private readonly string _filePath;
        private List<PluginField>? _fields;
        private const string OidFieldName = FgbPluginCursorTemplate.OidFieldName;
        private const string ShapeFieldName = FgbPluginCursorTemplate.ShapeFieldName;

        private ArcGISGeometryType _geometryType = ArcGISGeometryType.Unknown;
        private SpatialReference? _spatialReference;
        private Envelope? _extent;

        // 読み込み済みフィーチャキャッシュ
        private List<FgbRow>? _cachedRows;

        public FgbPluginTableTemplate(string filePath)
        {
            _filePath = filePath;
        }

        // ----------------------------------------------------------------
        // PluginTableTemplate オーバーライド
        // ----------------------------------------------------------------

        public override string GetName() =>
            Path.GetFileNameWithoutExtension(_filePath);

        public override IReadOnlyList<PluginField> GetFields()
        {
            if (_fields != null) return _fields;
            EnsureMetadata();
            return _fields!;
        }

        public override ArcGISGeometryType GetShapeType() => _geometryType;

        /// <summary>
        /// フィーチャデータセットの空間範囲と座標参照系を返す。
        /// ArcGIS Pro はこのメソッドでレイヤーの SpatialReference を決定する。
        /// </summary>
        public override Envelope GetExtent()
        {
            EnsureMetadata();
            return _extent ?? new EnvelopeBuilderEx(SpatialReferences.WGS84).ToGeometry();
        }

        public override PluginCursorTemplate Search(QueryFilter? queryFilter)
        {
            EnsureRows();

            // 返却フィールドリスト
            IReadOnlyList<string> requestedFields;
            if (queryFilter?.SubFields != null && queryFilter.SubFields.Trim() != "*")
            {
                requestedFields = queryFilter.SubFields
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            else
            {
                requestedFields = _fields!.Select(f => f.Name).ToList();
            }

            // 空間フィルタ
            Envelope? envelope = queryFilter is SpatialQueryFilter sqf ? sqf.FilterGeometry?.Extent : null;

            var result = _cachedRows!.AsEnumerable();

            // OID フィルタ（1件選択など）
            if (queryFilter?.ObjectIDs != null && queryFilter.ObjectIDs.Count > 0)
            {
                var oidSet = new HashSet<long>(queryFilter.ObjectIDs);
                result = result.Where(r => oidSet.Contains(r.Oid));
            }

            if (envelope != null)
            {
                result = result.Where(r =>
                {
                    if (r.Shape == null || r.Shape.IsEmpty) return false;
                    return !GeometryEngine.Instance.Disjoint(r.Shape.Extent, envelope);
                });
            }

            // WhereClause は未サポート（Plugin Datasource は通常 SDK 側でフィルタ適用）
            return new FgbPluginCursorTemplate(result.ToList(), requestedFields, _fields!);
        }

        public override PluginCursorTemplate Search(SpatialQueryFilter spatialQueryFilter)
            => Search((QueryFilter)spatialQueryFilter);

        // ----------------------------------------------------------------
        // 書き込みサポート
        // ----------------------------------------------------------------

        /// <summary>
        /// フィーチャを追加して .fgb ファイルに書き出す。
        /// </summary>
        public long AddFeature(Dictionary<string, object?> attributes, ArcGISGeometry? shape)
        {
            EnsureRows();
            var row = new FgbRow { Shape = shape };
            foreach (var kv in attributes)
                row.Attributes[kv.Key] = kv.Value;
            _cachedRows!.Add(row);
            Flush();
            return _cachedRows.Count; // 新しい OID
        }

        /// <summary>
        /// キャッシュの内容を .fgb ファイルに書き出す。
        /// </summary>
        public void Flush()
        {
            EnsureRows();
            var features = new List<NetTopologySuite.Features.Feature>();
            foreach (var row in _cachedRows!)
            {
                var ntsGeom = FgbGeometryConverter.ToNts(row.Shape);
                var attrTable = new NetTopologySuite.Features.AttributesTable();
                foreach (var kv in row.Attributes)
                    attrTable.Add(kv.Key, kv.Value);
                features.Add(new NetTopologySuite.Features.Feature(ntsGeom, attrTable));
            }
            var featureCollection = new NetTopologySuite.Features.FeatureCollection();
            foreach (var f in features)
                featureCollection.Add(f);

            byte[] bytes = FlatGeobuf.NTS.FeatureCollectionConversions.Serialize(
                featureCollection,
                FlatGeobuf.GeometryType.Unknown);

            File.WriteAllBytes(_filePath, bytes);
        }

        // ----------------------------------------------------------------
        // 内部ヘルパー
        // ----------------------------------------------------------------

        private void EnsureMetadata()
        {
            if (_fields != null) return;
            _fields = new List<PluginField>();

            if (!File.Exists(_filePath)) return;

            using var fs = File.OpenRead(_filePath);
            var header = FlatGeobuf.Helpers.ReadHeader(fs);

            // ジオメトリタイプ
            _geometryType = FgbGeometryConverter.ToArcGISGeometryType(header.GeometryType);

            // 空間参照
            if (header.Crs is FlatGeobuf.Crs crs && crs.Code > 0)
            {
                _spatialReference = SpatialReferenceBuilder.CreateSpatialReference(crs.Code);
            }
            _spatialReference ??= SpatialReferences.WGS84;

            // 空間範囲（FlatGeoBuf ヘッダーの bounding box: [minX, minY, maxX, maxY]）
            if (header.EnvelopeLength >= 4)
            {
                double minX = header.Envelope(0);
                double minY = header.Envelope(1);
                double maxX = header.Envelope(2);
                double maxY = header.Envelope(3);
                _extent = EnvelopeBuilderEx.CreateEnvelope(minX, minY, maxX, maxY, _spatialReference);
            }

            // OIDShape フィールドを先頭に追加（SDK パターン）
            _fields.Add(new PluginField(OidFieldName, OidFieldName, FieldType.OID));
            if (_geometryType != ArcGISGeometryType.Unknown)
                _fields.Add(new PluginField(ShapeFieldName, ShapeFieldName, FieldType.Geometry));

            // FlatGeoBuf ヘッダーから属性フィールドを追加
            for (int i = 0; i < header.ColumnsLength; i++)
            {
                var col = header.Columns(i);
                if (col == null) continue;
                var fldType = ToPluginFieldType(col.Value.Type);
                _fields.Add(new PluginField(col.Value.Name, col.Value.Name, fldType));
            }
        }

        private void EnsureRows()
        {
            if (_cachedRows != null) return;
            EnsureMetadata();
            _cachedRows = new List<FgbRow>();

            if (!File.Exists(_filePath)) return;

            using var fs = File.OpenRead(_filePath);
            var featureCollection = FlatGeobuf.NTS.FeatureCollectionConversions.Deserialize(fs);

            long oid = 1;
            foreach (var feature in featureCollection)
            {
                var row = new FgbRow
                {
                    Oid = oid++,
                    Shape = feature.Geometry != null
                        ? FgbGeometryConverter.ToArcGIS(feature.Geometry, _spatialReference)
                        : null
                };
                if (feature.Attributes != null)
                {
                    foreach (var name in feature.Attributes.GetNames())
                        row.Attributes[name] = feature.Attributes[name];
                }
                _cachedRows.Add(row);
            }
        }

        private static FieldType ToPluginFieldType(FlatGeobuf.ColumnType colType) =>
            colType switch
            {
                FlatGeobuf.ColumnType.Int or FlatGeobuf.ColumnType.UInt
                    => FieldType.Integer,
                FlatGeobuf.ColumnType.Long or FlatGeobuf.ColumnType.ULong
                    => FieldType.BigInteger,
                FlatGeobuf.ColumnType.Float or FlatGeobuf.ColumnType.Double
                    => FieldType.Double,
                FlatGeobuf.ColumnType.Bool
                    => FieldType.Integer,
                FlatGeobuf.ColumnType.DateTime
                    => FieldType.Date,
                _ => FieldType.String
            };
    }
}