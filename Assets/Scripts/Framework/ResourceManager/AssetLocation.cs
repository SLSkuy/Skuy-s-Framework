using System;

namespace Framework
{
    /// <summary>
    /// 一次加载请求的身份：位置必填，包裹可空
    /// </summary>
    public readonly struct AssetLocation : IEquatable<AssetLocation>
    {
        public string Location { get; }
        public string Package { get; }

        public AssetLocation(string location, string package = null)
        {
            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("Asset location is required.", nameof(location));

            Location = location;
            Package = package;
        }

        public bool Equals(AssetLocation other) =>
            Location == other.Location && Package == other.Package;

        public override bool Equals(object obj) => obj is AssetLocation other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Location, Package);

        public static bool operator ==(AssetLocation left, AssetLocation right) => left.Equals(right);

        public static bool operator !=(AssetLocation left, AssetLocation right) => !left.Equals(right);
    }
}
