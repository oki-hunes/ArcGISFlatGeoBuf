using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;

namespace ArcGISFlatGeoBuf
{
    /// <summary>
    /// FlatGeoBuf (.fgb) ファイルを ArcGIS Pro に公開する PluginDatasource。
    /// <para>
    /// 接続パスに .fgb ファイルのフルパスまたはそのフォルダパスを指定します。
    /// フォルダパスを指定した場合、フォルダ内の全 .fgb ファイルが利用可能になります。
    /// </para>
    /// </summary>
    public class FgbPluginDatasource : PluginDatasourceTemplate
    {
        // キー: テーブル名（拡張子なしファイル名）, 値: テーブルテンプレート
        private readonly Dictionary<string, FgbPluginTableTemplate> _tables = new(StringComparer.OrdinalIgnoreCase);
        private string _rootPath = string.Empty;

        // ----------------------------------------------------------------
        // PluginDatasourceTemplate オーバーライド
        // ----------------------------------------------------------------

        public override void Open(Uri connectionPath)
        {
            _rootPath = connectionPath.LocalPath;

            if (File.Exists(_rootPath) &&
                _rootPath.EndsWith(".fgb", StringComparison.OrdinalIgnoreCase))
            {
                // 単一ファイルモード
                RegisterFile(_rootPath);
            }
            else if (Directory.Exists(_rootPath))
            {
                // フォルダモード：フォルダ内の全 .fgb を登録
                foreach (var fgbFile in Directory.EnumerateFiles(_rootPath, "*.fgb", SearchOption.TopDirectoryOnly))
                    RegisterFile(fgbFile);
            }
        }

        public override void Close()
        {
            _tables.Clear();
        }

        public override PluginTableTemplate OpenTable(string name)
        {
            if (_tables.TryGetValue(name, out var tbl))
                return tbl;
            throw new InvalidOperationException($"テーブル '{name}' は存在しません。");
        }

        public override IReadOnlyList<string> GetTableNames() =>
            new List<string>(_tables.Keys);

        public override bool IsQueryLanguageSupported() => false;

        public override bool CanOpen(Uri connectionPath)
        {
            string localPath = connectionPath.LocalPath;
            if (File.Exists(localPath) &&
                localPath.EndsWith(".fgb", StringComparison.OrdinalIgnoreCase))
                return true;
            if (Directory.Exists(localPath))
                return Directory.EnumerateFiles(localPath, "*.fgb", SearchOption.TopDirectoryOnly).Any();
            return false;
        }

        public override string GetDatasourceDescription(bool inPluralForm) =>
            inPluralForm ? "FlatGeoBuf Files" : "FlatGeoBuf File";

        public override string GetDatasetDescription(DatasetType datasetType) =>
            datasetType == DatasetType.FeatureClass
                ? "FlatGeoBuf Feature Class"
                : "FlatGeoBuf Table";

        // ----------------------------------------------------------------
        // 書き込み用公開 API
        // ----------------------------------------------------------------

        /// <summary>
        /// 新しい .fgb ファイルをデータソースに追加（存在しない場合は作成）します。
        /// </summary>
        /// <param name="name">テーブル名（拡張子なし）</param>
        /// <returns>作成された <see cref="FgbPluginTableTemplate"/></returns>
        public FgbPluginTableTemplate CreateTable(string name)
        {
            string filePath = Path.Combine(_rootPath, name + ".fgb");
            RegisterFile(filePath);
            return _tables[name];
        }

        /// <summary>
        /// 指定テーブルにフィーチャを追加します。
        /// </summary>
        public long AddFeature(
            string tableName,
            System.Collections.Generic.Dictionary<string, object?> attributes,
            ArcGIS.Core.Geometry.Geometry? shape)
        {
            if (!_tables.TryGetValue(tableName, out var tbl))
                throw new InvalidOperationException($"テーブル '{tableName}' は存在しません。先に CreateTable を呼び出してください。");
            return tbl.AddFeature(attributes, shape);
        }

        // ----------------------------------------------------------------
        // 内部ヘルパー
        // ----------------------------------------------------------------

        private void RegisterFile(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            if (!_tables.ContainsKey(name))
                _tables[name] = new FgbPluginTableTemplate(filePath);
        }
    }
}