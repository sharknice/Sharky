using System;
using System.Numerics;

namespace Sharky
{
    public class CollisionCalculator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="C">Center of target</param>
        /// <param name="r">radius of target</param>
        /// <param name="E">starting point of attack</param>
        /// <param name="L">end point of attack</param>
        /// <returns></returns>
        public bool Collides(Vector2 C, float r, Vector2 E, Vector2 L)
        {
            var d = L - E;
            var f = E - C;

            float a = Vector2.Dot(d, d);
            float b = 2 * Vector2.Dot(f, d);
            float c = Vector2.Dot(f, f) - r * r;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
            {
                return false;
            }
            else
            {
                // ray didn't totally miss sphere,
                // so there is a solution to
                // the equation.

                discriminant = (float)Math.Sqrt(discriminant);

                // either solution may be on or off the ray so need to test both
                // t1 is always the smaller value, because BOTH discriminant and
                // a are nonnegative.
                float t1 = (-b - discriminant) / (2 * a);
                float t2 = (-b + discriminant) / (2 * a);

                // 3x HIT cases:
                //          -o->             --|-->  |            |  --|->
                // Impale(t1 hit,t2 hit), Poke(t1 hit,t2>1), ExitWound(t1<0, t2 hit), 

                // 3x MISS cases:
                //       ->  o                     o ->              | -> |
                // FallShort (t1>1,t2>1), Past (t1<0,t2<0), CompletelyInside(t1<0, t2>1)

                if (t1 >= 0 && t1 <= 1)
                {
                    // t1 is the intersection, and it's closer than t2
                    // (since t1 uses -b - discriminant)
                    // Impale, Poke
                    return true;
                }

                // here t1 didn't intersect so we are either started
                // inside the sphere or completely past it
                if (t2 >= 0 && t2 <= 1)
                {
                    // ExitWound
                    return true;
                }

                // no intn: FallShort, Past, CompletelyInside
                return false;
            }
        }
    }
}
