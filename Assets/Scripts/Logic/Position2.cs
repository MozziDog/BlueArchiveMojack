using System;
using System.Runtime.InteropServices;

namespace Logic
{
    // Representation of 2D vectors and points.
    [StructLayout(LayoutKind.Sequential)]
    [System.Serializable]
    public struct Position2 : IEquatable<Position2>
    {
        // X component of the vector.
        public float x;
        // Y component of the vector.
        public float y;

        // Access the /x/ or /y/ component using [0] or [1] respectively.
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2 index!");
                }
            }

            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2 index!");
                }
            }
        }

        // Constructs a new vector with given x, y components.
        public Position2(float x, float y) { this.x = x; this.y = y; }

        // Set x and y components of an existing Vector2.
        public void Set(float newX, float newY) { x = newX; y = newY; }

        // Linearly interpolates between two vectors.
        public static Position2 Lerp(Position2 a, Position2 b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Position2(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
            );
        }

        // Linearly interpolates between two vectors without clamping the interpolant

        public static Position2 LerpUnclamped(Position2 a, Position2 b, float t)
        {
            return new Position2(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
            );
        }

        // Moves a point /current/ towards /target/.
        public static Position2 MoveTowards(Position2 current, Position2 target, float maxDistanceDelta)
        {
            // avoid vector ops because current scripting backends are terrible at inlining
            float toVector_x = target.x - current.x;
            float toVector_y = target.y - current.y;

            float sqDist = toVector_x * toVector_x + toVector_y * toVector_y;

            if (sqDist == 0 || (maxDistanceDelta >= 0 && sqDist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            float dist = (float)Math.Sqrt(sqDist);

            return new Position2(current.x + toVector_x / dist * maxDistanceDelta,
                current.y + toVector_y / dist * maxDistanceDelta);
        }

        // Multiplies two vectors component-wise.
        public static Position2 Scale(Position2 a, Position2 b) { return new Position2(a.x * b.x, a.y * b.y); }

        // Multiplies every component of this vector by the same component of /scale/.
        public void Scale(Position2 scale) { x *= scale.x; y *= scale.y; }

        // Makes this vector have a ::ref::magnitude of 1.
        public void Normalize()
        {
            float mag = magnitude;
            if (mag > kEpsilon)
                this = this / mag;
            else
                this = zero;
        }

        // Returns this vector with a ::ref::magnitude of 1 (RO).
        public Position2 normalized
        {
            get
            {
                Position2 v = new Position2(x, y);
                v.Normalize();
                return v;
            }
        }

        /// *listonly*
        public override string ToString()
        {
            return String.Format("({0}, {1})", x, y);
        }

        // used to allow Vector2s to be used as keys in hash tables
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2);
        }

        // also required for being able to use Vector2s as keys in hash tables
        public override bool Equals(object other)
        {
            if (other is Position2 v)
                return Equals(v);
            return false;
        }

        public bool Equals(Position2 other)
        {
            return x == other.x && y == other.y;
        }

        // Returns the length of this vector (RO).
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        // Returns the squared length of this vector (RO).
        public float sqrMagnitude { get { return x * x + y * y; } }

        // Returns the distance between /a/ and /b/.
        public static float Distance(Position2 a, Position2 b)
        {
            float diff_x = a.x - b.x;
            float diff_y = a.y - b.y;
            return (float)Math.Sqrt(diff_x * diff_x + diff_y * diff_y);
        }

        public static float SqrMagnitude(Position2 a) { return a.x * a.x + a.y * a.y; }

        public float SqrMagnitude() { return x * x + y * y; }

        // Adds two vectors.
        public static Position2 operator +(Position2 a, Position2 b) { return new Position2(a.x + b.x, a.y + b.y); }
        // Subtracts one vector from another.

        public static Position2 operator -(Position2 a, Position2 b) { return new Position2(a.x - b.x, a.y - b.y); }
        // Multiplies one vector by another.

        public static Position2 operator *(Position2 a, Position2 b) { return new Position2(a.x * b.x, a.y * b.y); }
        // Divides one vector over another.

        public static Position2 operator /(Position2 a, Position2 b) { return new Position2(a.x / b.x, a.y / b.y); }
        // Negates a vector.

        public static Position2 operator -(Position2 a) { return new Position2(-a.x, -a.y); }
        // Multiplies a vector by a number.

        public static Position2 operator *(Position2 a, float d) { return new Position2(a.x * d, a.y * d); }
        // Multiplies a vector by a number.

        public static Position2 operator *(float d, Position2 a) { return new Position2(a.x * d, a.y * d); }
        // Divides a vector by a number.

        public static Position2 operator /(Position2 a, float d) { return new Position2(a.x / d, a.y / d); }
        // Returns true if the vectors are equal.

        public static bool operator ==(Position2 lhs, Position2 rhs)
        {
            // Returns false in the presence of NaN values.
            float diff_x = lhs.x - rhs.x;
            float diff_y = lhs.y - rhs.y;
            return (diff_x * diff_x + diff_y * diff_y) < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.

        public static bool operator !=(Position2 lhs, Position2 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        static readonly Position2 zeroVector = new Position2(0F, 0F);
        static readonly Position2 oneVector = new Position2(1F, 1F);
        static readonly Position2 forwardVector = new Position2(0F, 1F);
        static readonly Position2 backwardVector = new Position2(0F, -1F);
        static readonly Position2 leftVector = new Position2(-1F, 0F);
        static readonly Position2 rightVector = new Position2(1F, 0F);
        static readonly Position2 positiveInfinityVector = new Position2(float.PositiveInfinity, float.PositiveInfinity);
        static readonly Position2 negativeInfinityVector = new Position2(float.NegativeInfinity, float.NegativeInfinity);


        // Shorthand for writing @@Vector2(0, 0)@@
        public static Position2 zero { get { return zeroVector; } }
        // Shorthand for writing @@Vector2(1, 1)@@
        public static Position2 one { get { return oneVector; } }
        // Shorthand for writing @@Vector2(0, 1)@@
        public static Position2 forward { get { return forwardVector; } }
        // Shorthand for writing @@Vector2(0, -1)@@
        public static Position2 backward { get { return backwardVector; } }
        // Shorthand for writing @@Vector2(-1, 0)@@
        public static Position2 left { get { return leftVector; } }
        // Shorthand for writing @@Vector2(1, 0)@@
        public static Position2 right { get { return rightVector; } }
        // Shorthand for writing @@Vector2(float.PositiveInfinity, float.PositiveInfinity)@@
        public static Position2 positiveInfinity { get { return positiveInfinityVector; } }
        // Shorthand for writing @@Vector2(float.NegativeInfinity, float.NegativeInfinity)@@
        public static Position2 negativeInfinity { get { return negativeInfinityVector; } }

        // *Undocumented*
        public const float kEpsilon = 0.00001F;
        // *Undocumented*
        public const float kEpsilonNormalSqrt = 1e-15f;
    }
}