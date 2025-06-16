using System.IO;
using MapLib.Geometry;

namespace MapLib.FileFormats;

public interface IRasterFormatReader
{
    /// <summary>
    /// Reads the specified file and returns its data.
    /// </summary>
    /// <exception cref="IOException">
    /// Thrown if file was not found, readable or on other
    /// i/o related errors.
    /// </exception>
    /// <exception cref="ApplicationException">
    /// The data is invalid or other format-specific errors. See message
    /// for details.
    /// </exception>
    // TODO: remove requirement on bounds (read full file if null)
    public RasterData2 ReadFile(string filePath, Bounds bounds);
}