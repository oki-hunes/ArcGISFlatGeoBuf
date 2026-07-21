using System.Globalization;
using System.Resources;

namespace ArcGISFlatGeoBuf.Properties
{
    /// <summary>
    /// ローカライズ済み文字列へのアクセサ。
    /// <para>
    /// 実体は Strings.resx（既定 = 英語）と Strings.ja.resx（日本語）などの
    /// カルチャ別サテライトリソースであり、ArcGIS Pro の表示言語設定
    /// （<see cref="CultureInfo.CurrentUICulture"/>）に応じて自動的に切り替わる。
    /// 新しい言語を追加する場合は Strings.&lt;culture&gt;.resx を追加するだけでよい。
    /// </para>
    /// </summary>
    internal static class Strings
    {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("ArcGISFlatGeoBuf.Properties.Strings", typeof(Strings).Assembly);

        private static string Get(string name) =>
            ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

        public static string DatasourceDescriptionSingular => Get(nameof(DatasourceDescriptionSingular));

        public static string DatasourceDescriptionPlural => Get(nameof(DatasourceDescriptionPlural));

        public static string DatasetDescriptionFeatureClass => Get(nameof(DatasetDescriptionFeatureClass));

        public static string DatasetDescriptionTable => Get(nameof(DatasetDescriptionTable));

        public static string ErrorTableNotFound(string tableName) =>
            string.Format(CultureInfo.CurrentUICulture, Get(nameof(ErrorTableNotFound)), tableName);

        public static string ErrorTableNotFoundCreateFirst(string tableName) =>
            string.Format(CultureInfo.CurrentUICulture, Get(nameof(ErrorTableNotFoundCreateFirst)), tableName);
    }
}
