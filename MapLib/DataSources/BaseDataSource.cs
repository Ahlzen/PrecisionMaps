using MapLib.GdalSupport;
using MapLib.Geometry;

namespace MapLib.DataSources;

/// <remarks>
/// All bounds are lat/lon WGS84.
/// </remarks>
public abstract class BaseDataSource
{
    public abstract string Name { get; }

    public abstract string Srs { get; }

    /// <summary>
    /// Bounds of full data set. In source (dataset) SRS. If known.
    /// </summary>
    public abstract Bounds? Bounds { get; }

    /// <summary>
    /// Bounds of full data set, in lon/lat WGS84. If known.
    /// </summary>
    public Bounds? BoundsWgs84
    {
        get
        {
            if (Bounds == null) return null;
            Transformer sourceToWgs84 = new(Srs, Transformer.WktWgs84);
            return sourceToWgs84.Transform(Bounds.Value);
        }
    }
}

public abstract class BaseRasterDataSource : BaseDataSource
{
    /// <summary>
    /// Return all raster data for the specified source.
    /// </summary>
    public abstract RasterData GetData();

    /// <summary>
    /// Return all raster data for the specified source
    /// in the specfied srs/projection, reprojecting (warping) the
    /// raster data if necessary.
    /// </summary>
    public abstract RasterData GetData(string destSrs);
}

public abstract class BaseRasterDataSource2 : BaseDataSource
{
    /// <summary>
    /// Return all raster data for the specified source.
    /// </summary>
    public abstract RasterData2 GetData();

    /// <summary>
    /// Return all raster data for the specified source
    /// in the specfied srs/projection, reprojecting (warping) the
    /// raster data if necessary.
    /// </summary>
    public abstract RasterData2 GetData(string destSrs);
}

public abstract class BaseVectorDataSource : BaseDataSource
{
    /// <summary>
    /// Returns true if this source is naturally bounded (beyond the entire earth),
    /// e.g. vector data in a file. False otherwise, such as an online
    /// data source like OSM.
    /// </summary>
    public abstract bool IsBounded { get; }

    /// <summary>
    /// Return all vector data from the specified source.
    /// Supported for bounded sources only.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called on an unbounded source.
    /// </exception>
    public abstract VectorData GetData();
    /// <summary>
    /// See GetData(). Returns data in the specified SRS, reprojecting if needed.
    /// </summary>
    public virtual VectorData GetData(string destSrs)
    {
        Transformer transformer = new(this.Srs, destSrs);
        return GetData().Transform(transformer);
    }

    /// <summary>
    /// Return vector data for (at least) the specified bounds from
    /// this source.
    /// </summary>
    /// <remarks>
    /// May return less if specified bounds are beyond those of
    /// this data source.
    /// May return more if needed (e.g. may not crop/trim a file data source).
    /// </remarks>
    public abstract VectorData GetData(Bounds boundsWgs84);
    /// <summary>
    /// See GetData(). Returns data in the specified SRS, reprojecting if needed.
    /// </summary>
    public virtual VectorData GetData(Bounds boundsWgs84, string destSrs)
    {
        Transformer transformer = new(this.Srs, destSrs);
        return GetData(boundsWgs84).Transform(transformer);
    }


}