using System.IO;

namespace MapLib.FileFormats;

public interface IVectorFormatReader
{
    /// <summary>
    /// Reads the specified file and returns its features.
    /// </summary>
    /// <exception cref="IOException">
    /// Thrown if file was not found, readable or on other
    /// i/o related errors.
    /// </exception>
    /// <exception cref="ApplicationException">
    /// The data is invalid or other format-specific errors. See message
    /// for details.
    /// </exception>
    public VectorData ReadFile(string filePath);
}
