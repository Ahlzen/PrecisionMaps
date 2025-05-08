namespace MapLib.ColorSpace;

public class Gradient
{
    public struct Stop : IComparable<Stop>
    {
        internal Stop(Gradient gradient, float position, (float r, float g, float b) rgb)
        {
            _gradient = gradient;
            _position = position;
            Rgb = rgb;
        }

        private Gradient _gradient;
        public (float r, float g, float b) Rgb { get; set; }

        private float _position;
        public float Position
        {
            get => _position;
            set
            {
                _position = value;
                _gradient.EnsureStopsAreSorted();
            }
        }

        public int CompareTo(Stop other)
            => Position.CompareTo(other.Position);
    }

    private readonly List<Stop> _stops = new(); // sorted list

    public Gradient()
    {
    }

    public int Count => _stops.Count;
    public Stop this[int n] => _stops[n];

    public Stop Add(float position, (float r, float g, float b) rgb)
    {
        Stop stop = new Stop(this, position, rgb);
        _stops.Add(stop);
        return stop;
    }

    public (float r, float g, float b) ColorAt(float position)
    {
        // Naive RGB interpolation. 
        // TODO: Support more suitable color interpolation methods

        if (_stops.Count == 0)
        {
            // No stops
            return (0, 0, 0);
        }
        else if (_stops.Count == 1)
        {
            // 1 stop
            return _stops[0].Rgb;
        }
        else
        {
            // At least 2 stops
            if (position < _stops[0].Position)
            {
                // Position is prior to the first stop
                return _stops[0].Rgb;
            }
            else if (position > _stops[^1].Position)
            {
                // Position is past the last stop
                return _stops[^1].Rgb;
            }
            else
            {
                // Position is between two stops. Interpolate!
                int rightIndex = 1;
                while (_stops[rightIndex].Position < position)
                    rightIndex++;
                float lPos = _stops[rightIndex - 1].Position;
                float rPos = _stops[rightIndex].Position;
                if (lPos == rPos)
                    return _stops[rightIndex].Rgb;
                float intraPosition = (position - lPos) / (rPos - lPos);
                return Lerp(
                    _stops[rightIndex - 1].Rgb,
                    _stops[rightIndex].Rgb,
                    intraPosition);
            }
        }
    }

    public void GetColorSamples((float r, float g, float b)[] destination)
    {
        // Naive RGB interpolation. 
        // TODO: Support more suitable color interpolation methods

        // TODO: This can be optimized. We'll use this for now
        int n = destination.Length;
        for (int i = 0; i < n; i++)
        {
            float position = i / (n - 1);
            destination[i] = ColorAt(position);
        }
    }

    #region Helpers

    internal void EnsureStopsAreSorted() => _stops.Sort();

    private static (float r, float g, float b) Lerp(
        (float r, float g, float b) rgb1,
        (float r, float g, float b) rgb2,
        float position) => (
            rgb1.r + position * (rgb2.r - rgb1.r),
            rgb1.g + position * (rgb2.g - rgb1.g),
            rgb1.b + position * (rgb2.b - rgb1.b));

    #endregion
}
