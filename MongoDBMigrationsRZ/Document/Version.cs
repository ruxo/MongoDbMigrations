using System;

namespace MongoDBMigrations
{
    /// <summary>
    /// Semantic versioning
    /// </summary>
    public struct Version : IComparable<Version>
    {
        private const char VERSION_SPLITTER = '.';
        private const int MAX_LENGTH = 3;
        public readonly int Major;
        public readonly int Minor;
        public readonly int Revision;


        public static Version Zero()
        {
            return new Version(0, 0, 0);
        }

        public Version(string version)
        {
            string[] parts = version.Split(VERSION_SPLITTER);

            if (parts.Length > MAX_LENGTH)
            {
                throw new VersionStringTooLongException(version);
            }

            ParseVersionPart(parts[0], out Major);
            ParseVersionPart(parts[1], out Minor);
            ParseVersionPart(parts[2], out Revision);
        }

        public Version(int major, int minor, int revision)
        {
            Major = major;
            Minor = minor;
            Revision = revision;
        }

        public static implicit operator Version(string version)
        {
            return new Version(version);
        }

        public static implicit operator string(Version version)
        {
            return version.ToString();
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}";
        }

        #region Compare
        public int CompareTo(Version other)
        {
            if (Equals(other))
                return 0;

            return this > other ? 1 : -1;
        }

        public static bool operator ==(Version a, Version b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Version a, Version b)
        {
            return !(a == b);
        }

        public static bool operator >(Version a, Version b)
        {
            return a.Major > b.Major
                || (a.Major == b.Major && a.Minor > b.Minor)
                || (a.Major == b.Major && a.Minor == b.Minor && a.Revision > b.Revision);
        }

        public static bool operator <(Version a, Version b)
        {
            return a != b && !(a > b);
        }

        public static bool operator <=(Version a, Version b)
        {
            return a == b || a < b;
        }

        public static bool operator >=(Version a, Version b)
        {
            return a == b || a > b;
        }

        public bool Equals(Version other)
        {
            return other.Major == Major && other.Minor == Minor && other.Revision == Revision;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (obj.GetType() != typeof(Version))
                return false;

            return Equals((Version)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Major;
                result = (result * 397) ^ Minor;
                result = (result * 397) ^ Revision;
                return result;
            }
        }

        #endregion

        private static void ParseVersionPart(string value, out int target)
        {
            if (!int.TryParse(value, out target))
            {
                throw new InvalidVersionException(value);
            }
        }
    }
}