using MapLib.RasterOps;

namespace MapLib.Tests.RasterOps;

[TestFixture]
public class PadAndCropFixture : BaseFixture
{
    [Test]
    public void TestCrop()
    {
        SingleBandRasterData source = GetSingleBandTestImage();
        float[] croppedData = PadAndCrop.Crop(source, 20, 20, 100, 150);
        SingleBandRasterData cropped =
            new SingleBandRasterData(source.Srs, source.Bounds,
            100, 150, croppedData, source.NoDataValue);
        SaveTempBitmap(cropped.ToImageRasterData().Bitmap, "TestCrop", ".jpg");
    }

    [Test]
    public void TestPadWithSingleValue()
    {
        SingleBandRasterData source = GetSingleBandTestImage();
        float[] paddedData = PadAndCrop.PadWithSingleValue(source, 20, 30, 40, 50, 0.5f);
        SingleBandRasterData padded  =
            new SingleBandRasterData(source.Srs, source.Bounds,
            source.WidthPx + 30 + 50,
            source.HeightPx + 20 + 40,
            paddedData, source.NoDataValue);
        SaveTempBitmap(padded.ToImageRasterData().Bitmap, "TestPadSingleValue", ".jpg");
    }

    [Test]
    public void TestPadExtendingEdges()
    {
        SingleBandRasterData source = GetSingleBandTestImage();
        //SingleBandRasterData padded = source.PadExtendingEdges(80, 100, 120, 150);
        float[] paddedData = PadAndCrop.PadExtendingEdges(source, 80, 100, 120, 150);
        SingleBandRasterData padded  =
            new SingleBandRasterData(source.Srs, source.Bounds,
            source.WidthPx + 100 + 150,
            source.HeightPx + 80 + 120,
            paddedData, source.NoDataValue);
        SaveTempBitmap(padded.ToImageRasterData().Bitmap, "TestPadExtendingEdges", ".jpg");
    }
}
