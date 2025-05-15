using MapLib.GdalSupport;
using MapLib.Geometry;

namespace MapLib.DataSources;

public abstract class BaseDataSource<TData>
{
    public abstract string Name { get; }

    public abstract string Srs { get; }

    /// <summary>
    /// Bounds of full data set. In source (dataset) SRS. If known/applicable.
    /// </summary>
    /// <remarks>
    /// For a file, this is the extent of the data in the file.
    /// For an online data source, this returns the full applicable
    /// bounds for that source.
    /// </remarks>
    public abstract Bounds? Bounds { get; }

    /// <summary>
    /// Bounds of full data set, in lon/lat WGS84. If known/applicable.
    /// </summary>
    public Bounds? BoundsWgs84 => Bounds?.ToWgs84(Srs);
    

    /// <summary>
    /// Returns true if this source is strictly bounded (beyond the
    /// entire earth or data set),
    /// e.g. vector data in a file. False otherwise, such as an online
    /// data source like OSM, SRTM or USGS 3DEP.
    /// </summary>
    public abstract bool IsBounded { get; }

    
    /// <summary>
    /// Return all data from the specified source.
    /// Supported for bounded sources only.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called on an unbounded source.
    /// </exception>
    public abstract TData GetData();

    /// <summary>
    /// See GetData(). Returns data in the specified SRS,
    /// reprojecting/warping if needed.
    /// </summary>
    public abstract TData GetData(string destSrs);

    /// <summary>
    /// Return data for (at least) the specified bounds from
    /// this source.
    /// </summary>
    /// <remarks>
    /// May return less if specified bounds are beyond those of
    /// this data source.
    /// May return more if needed (e.g. may not crop/trim a file data source).
    /// </remarks>
    public abstract TData GetData(Bounds boundsWgs84);

    /// <summary>
    /// See GetData(). Returns data in the specified SRS,
    /// reprojecting/warping if needed.
    /// </summary>
    public abstract TData GetData(Bounds boundsWgs84, string destSrs);
}



public abstract class BaseVectorDataSource : BaseDataSource<VectorData>
{
    public override VectorData GetData(string destSrs)
        => Reproject(GetData(), destSrs);

    public override VectorData GetData(Bounds boundsWgs84, string destSrs)
        => Reproject(GetData(boundsWgs84), destSrs);

    public VectorData Reproject(VectorData data, string destSrs)
    {
        Transformer transformer = new(this.Srs, destSrs);
        return data.Transform(transformer);
    }
}

[Obsolete]
public abstract class BaseRasterDataSource : BaseDataSource<RasterData>
{
}

public abstract class BaseRasterDataSource2 : BaseDataSource<RasterData2>
{
}