using System;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using NtsGeometry = NetTopologySuite.Geometries;

namespace ArcGISFlatGeoBuf
{
    /// <summary>
    /// NetTopologySuite ジオメトリ と ArcGIS Core ジオメトリを相互変換するユーティリティ。
    /// </summary>
    internal static class FgbGeometryConverter
    {
        // ----------------------------------------------------------------
        // NTS → ArcGIS Core
        // ----------------------------------------------------------------

        public static Geometry ToArcGIS(NtsGeometry.Geometry? nts, SpatialReference? sr)
        {
            if (nts == null || nts.IsEmpty)
                return GeometryEngine.Instance.ImportFromWKT(0, "POINT EMPTY", sr);

            return nts switch
            {
                NtsGeometry.Point pt => ToMapPoint(pt, sr),
                NtsGeometry.MultiPoint mp => ToMultiPoint(mp, sr),
                NtsGeometry.LineString ls => ToPolyline(ls, sr),
                NtsGeometry.MultiLineString mls => ToPolylineMulti(mls, sr),
                NtsGeometry.Polygon pg => ToPolygon(pg, sr),
                NtsGeometry.MultiPolygon mpg => ToPolygonMulti(mpg, sr),
                _ => GeometryEngine.Instance.ImportFromWKB(WkbImportFlags.WkbImportDefaults,
                         nts.AsBinary(), sr)
            };
        }

        private static MapPoint ToMapPoint(NtsGeometry.Point pt, SpatialReference? sr)
        {
            bool hasZ = !double.IsNaN(pt.Z);
            return hasZ
                ? MapPointBuilderEx.CreateMapPoint(pt.X, pt.Y, pt.Z, sr)
                : MapPointBuilderEx.CreateMapPoint(pt.X, pt.Y, sr);
        }

        private static Multipoint ToMultiPoint(NtsGeometry.MultiPoint mp, SpatialReference? sr)
        {
            var bldr = new MultipointBuilderEx(sr);
            foreach (NtsGeometry.Point pt in mp.Geometries)
                bldr.AddPoint(ToMapPoint(pt, sr));
            return bldr.ToGeometry();
        }

        private static Polyline ToPolyline(NtsGeometry.LineString ls, SpatialReference? sr)
        {
            var bldr = new PolylineBuilderEx(sr);
            bldr.AddPart(ToMapPoints(ls.CoordinateSequence, sr));
            return bldr.ToGeometry();
        }

        private static Polyline ToPolylineMulti(NtsGeometry.MultiLineString mls, SpatialReference? sr)
        {
            var bldr = new PolylineBuilderEx(sr);
            foreach (NtsGeometry.LineString ls in mls.Geometries)
                bldr.AddPart(ToMapPoints(ls.CoordinateSequence, sr));
            return bldr.ToGeometry();
        }

        private static Polygon ToPolygon(NtsGeometry.Polygon pg, SpatialReference? sr)
        {
            var bldr = new PolygonBuilderEx(sr);
            bldr.AddPart(ToMapPoints(pg.ExteriorRing.CoordinateSequence, sr));
            foreach (NtsGeometry.LinearRing hole in pg.InteriorRings)
                bldr.AddPart(ToMapPoints(hole.CoordinateSequence, sr));
            return bldr.ToGeometry();
        }

        private static Polygon ToPolygonMulti(NtsGeometry.MultiPolygon mpg, SpatialReference? sr)
        {
            var bldr = new PolygonBuilderEx(sr);
            foreach (NtsGeometry.Polygon pg in mpg.Geometries)
            {
                bldr.AddPart(ToMapPoints(pg.ExteriorRing.CoordinateSequence, sr));
                foreach (NtsGeometry.LinearRing hole in pg.InteriorRings)
                    bldr.AddPart(ToMapPoints(hole.CoordinateSequence, sr));
            }
            return bldr.ToGeometry();
        }

        private static List<MapPoint> ToMapPoints(NtsGeometry.CoordinateSequence seq, SpatialReference? sr)
        {
            bool hasZ = seq.HasZ;
            var pts = new List<MapPoint>(seq.Count);
            for (int i = 0; i < seq.Count; i++)
            {
                double x = seq.GetX(i);
                double y = seq.GetY(i);
                pts.Add(hasZ
                    ? MapPointBuilderEx.CreateMapPoint(x, y, seq.GetZ(i), sr)
                    : MapPointBuilderEx.CreateMapPoint(x, y, sr));
            }
            return pts;
        }

        // ----------------------------------------------------------------
        // ArcGIS Core → NTS
        // ----------------------------------------------------------------

        public static NtsGeometry.Geometry? ToNts(Geometry? arcGis)
        {
            if (arcGis == null || arcGis.IsEmpty)
                return null;

            return arcGis switch
            {
                MapPoint pt => FromMapPoint(pt),
                Multipoint mp => FromMultipoint(mp),
                Polyline pl => FromPolyline(pl),
                Polygon pg => FromPolygon(pg),
                _ => null
            };
        }

        private static NtsGeometry.Point FromMapPoint(MapPoint pt)
        {
            var factory = NtsGeometry.GeometryFactory.Default;
            return pt.HasZ
                ? factory.CreatePoint(new NtsGeometry.CoordinateZ(pt.X, pt.Y, pt.Z))
                : factory.CreatePoint(new NtsGeometry.Coordinate(pt.X, pt.Y));
        }

        private static NtsGeometry.MultiPoint FromMultipoint(Multipoint mp)
        {
            var factory = NtsGeometry.GeometryFactory.Default;
            var pts = new List<NtsGeometry.Point>();
            foreach (var pt in mp.Points)
                pts.Add(FromMapPoint(pt));
            return factory.CreateMultiPoint(pts.ToArray());
        }

        private static NtsGeometry.Geometry FromPolyline(Polyline pl)
        {
            var factory = NtsGeometry.GeometryFactory.Default;
            var lines = new List<NtsGeometry.LineString>();
            foreach (var part in pl.Parts)
            {
                var coords = new List<NtsGeometry.Coordinate>();
                foreach (var seg in part)
                {
                    coords.Add(CoordFrom(seg.StartPoint));
                    coords.Add(CoordFrom(seg.EndPoint));
                }
                if (coords.Count >= 2)
                    lines.Add(factory.CreateLineString(coords.ToArray()));
            }
            if (lines.Count == 1) return lines[0];
            return factory.CreateMultiLineString(lines.ToArray());
        }

        private static NtsGeometry.Geometry FromPolygon(Polygon pg)
        {
            var factory = NtsGeometry.GeometryFactory.Default;
            var rings = new List<NtsGeometry.LinearRing>();
            foreach (var part in pg.Parts)
            {
                var coords = new List<NtsGeometry.Coordinate>();
                foreach (var seg in part)
                {
                    coords.Add(CoordFrom(seg.StartPoint));
                    coords.Add(CoordFrom(seg.EndPoint));
                }
                if (coords.Count >= 3)
                {
                    if (!coords[0].Equals2D(coords[^1]))
                        coords.Add(coords[0]);
                    rings.Add(factory.CreateLinearRing(coords.ToArray()));
                }
            }
            if (rings.Count == 0) return factory.CreatePolygon();
            if (rings.Count == 1) return factory.CreatePolygon(rings[0]);

            // 外環(面積最大)を exterior、残りを hole と見なす
            NtsGeometry.LinearRing exterior = rings[0];
            double maxArea = 0;
            foreach (var r in rings)
            {
                double a = Math.Abs(factory.CreatePolygon(r).Area);
                if (a > maxArea) { maxArea = a; exterior = r; }
            }
            var holes = rings.FindAll(r => r != exterior);
            return factory.CreatePolygon(exterior, holes.ToArray());
        }

        private static NtsGeometry.Coordinate CoordFrom(MapPoint pt) =>
            pt.HasZ
                ? new NtsGeometry.CoordinateZ(pt.X, pt.Y, pt.Z)
                : new NtsGeometry.Coordinate(pt.X, pt.Y);

        // ----------------------------------------------------------------
        // GeometryType 変換
        // ----------------------------------------------------------------

        public static GeometryType ToArcGISGeometryType(FlatGeobuf.GeometryType fgbType) =>
            fgbType switch
            {
                FlatGeobuf.GeometryType.Point or FlatGeobuf.GeometryType.MultiPoint
                    => GeometryType.Point,
                FlatGeobuf.GeometryType.LineString or FlatGeobuf.GeometryType.MultiLineString
                    => GeometryType.Polyline,
                FlatGeobuf.GeometryType.Polygon or FlatGeobuf.GeometryType.MultiPolygon
                    => GeometryType.Polygon,
                _ => GeometryType.Unknown
            };
    }
}
