using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;

namespace ArcGISFlatGeoBuf
{
    /// <summary>
    /// FlatGeoBuf フィーチャを1行ずつ返すカーソル実装。
    /// </summary>
    internal class FgbPluginCursorTemplate : PluginCursorTemplate
    {
        private readonly List<FgbRow> _rows;
        private int _index = -1;
        private readonly IReadOnlyList<string> _requestedFields;
        private readonly List<PluginField> _allFields;

        public FgbPluginCursorTemplate(
            List<FgbRow> rows,
            IReadOnlyList<string> requestedFields,
            List<PluginField> allFields)
        {
            _rows = rows;
            _requestedFields = requestedFields;
            _allFields = allFields;
        }

        // フィールド名定数 - FgbPluginTableTemplate と一致させること
        internal const string OidFieldName = "OBJECTID";
        internal const string ShapeFieldName = "SHAPE";

        public override PluginRow GetCurrentRow()
        {
            var row = _rows[_index];
            var values = new List<object>();

            // Values must map 1:1 to _allFields (= GetFields() order).
            // Non-requested fields still need a DBNull placeholder.
            foreach (var field in _allFields)
            {
                bool requested = _requestedFields.Contains(
                    field.Name, StringComparer.OrdinalIgnoreCase);

                if (!requested)
                {
                    values.Add(DBNull.Value);
                    continue;
                }

                if (string.Equals(field.Name, OidFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    values.Add((long)row.Oid);
                }
                else if (string.Equals(field.Name, ShapeFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    values.Add(row.Shape ?? (object)DBNull.Value);
                }
                else if (row.Attributes.TryGetValue(field.Name, out object? val))
                {
                    values.Add(val ?? DBNull.Value);
                }
                else
                {
                    values.Add(DBNull.Value);
                }
            }

            return new PluginRow() { Values = values };
        }

        public override bool MoveNext()
        {
            _index++;
            return _index < _rows.Count;
        }
    }

    /// <summary>
    /// FlatGeoBuf から読み込んだ1フィーチャ分のデータを保持する DTO。
    /// </summary>
    internal class FgbRow
    {
        public long Oid { get; set; }
        public Geometry? Shape { get; set; }
        public Dictionary<string, object?> Attributes { get; } = new();
    }
}