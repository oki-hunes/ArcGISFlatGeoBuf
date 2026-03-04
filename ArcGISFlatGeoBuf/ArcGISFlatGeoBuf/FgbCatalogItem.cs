using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ESRI.ArcGIS.ItemIndex;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArcGISFlatGeoBuf
{
    /// <summary>
    /// ArcGIS Catalog カスタムアイテム。
    /// .fgb ファイルを認識し、マップへのドラッグ＆ドロップおよび
    /// [データの追加] ダイアログからの追加を可能にします。
    /// </summary>
    internal class FgbCatalogItem : CustomItemBase, IMappableItem
    {
        // CustomItemBase はパラメータなしコンストラクタを要求する
        protected FgbCatalogItem() : base()
        {
        }

        // DAML が ItemInfoValue を渡す際に呼ばれるコンストラクタ
        protected FgbCatalogItem(ItemInfoValue iiv) : base(FlipBrowseDialogOnly(iiv))
        {
        }

        /// <summary>
        /// browseDialogOnly を false にして、カタログペインでも表示されるようにする。
        /// </summary>
        private static ItemInfoValue FlipBrowseDialogOnly(ItemInfoValue iiv)
        {
            iiv.browseDialogOnly = "FALSE";
            return iiv;
        }

        /// <summary>
        /// .fgb ファイルは単一ファイルであり、子アイテムを持たない。
        /// </summary>
        public override bool IsContainer => false;

        /// <summary>
        /// カタログペインのアイコン（大）
        /// </summary>
        public override ImageSource LargeImage
        {
            get
            {
                return new BitmapImage(new Uri(
                    "pack://application:,,,/ArcGISFlatGeoBuf;component/Images/AddInDesktop32.png"));
            }
        }

        /// <summary>
        /// カタログペインのアイコン（小）
        /// </summary>
        public override Task<ImageSource> SmallImage
        {
            get
            {
                ImageSource img = new BitmapImage(new Uri(
                    "pack://application:,,,/ArcGISFlatGeoBuf;component/Images/AddInDesktop16.png"));
                return Task.FromResult(img);
            }
        }

        // -----------------------------------------------------------------------
        // IMappableItem の実装
        // -----------------------------------------------------------------------

        /// <summary>
        /// 2D マップへの追加のみサポート。
        /// </summary>
        public bool CanAddToMap(MapType? mapType)
        {
            if (mapType.HasValue && mapType.Value != MapType.Map)
                return false;
            return true;
        }

        /// <summary>
        /// .fgb ファイルをプラグインデータソース経由でマップに追加する。
        /// このメソッドはすでに MCT (Map Construction Thread / QueuedTask) 上で呼ばれる。
        /// </summary>
        public List<string> OnAddToMap(Map map)
        {
            return AddFgbToMap(map, null, -1);
        }

        /// <summary>
        /// グループレイヤー指定付きの追加オーバーロード。
        /// </summary>
        public List<string> OnAddToMap(Map map, ILayerContainerEdit groupLayer, int index)
        {
            return AddFgbToMap(map, groupLayer, index);
        }

        // -----------------------------------------------------------------------
        // プライベートヘルパー
        // -----------------------------------------------------------------------

        private List<string> AddFgbToMap(Map map, ILayerContainerEdit groupLayer, int index)
        {
            var result = new List<string>();

            // .fgb ファイルへのプラグインデータソース接続パスを構築
            var connPath = new PluginDatasourceConnectionPath(
                "ArcGISFlatGeoBuf_FgbPluginDatasource",
                new Uri(this.Path, UriKind.Absolute));

            using var datastore = new PluginDatastore(connPath);

            // テーブル名を列挙してフィーチャレイヤーを追加
            foreach (var tableName in datastore.GetTableNames())
            {
                using var table = datastore.OpenTable(tableName);
                if (table is FeatureClass fc)
                {
                    var lyrParams = new FeatureLayerCreationParams(fc)
                    {
                        MapMemberIndex = index >= 0 ? index : 0
                    };

                    FeatureLayer layer;
                    if (groupLayer != null)
                        layer = LayerFactory.Instance.CreateLayer<FeatureLayer>(lyrParams, groupLayer);
                    else
                        layer = LayerFactory.Instance.CreateLayer<FeatureLayer>(lyrParams, map);

                    if (layer != null)
                        result.Add(layer.URI);
                }
            }

            return result;
        }
    }
}
