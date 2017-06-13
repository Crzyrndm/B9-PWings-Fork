using System;
using UnityEngine;

namespace WingProcedural
{
    // Credit goes to R4m0n
    // Originally released with Procedural Dynamics mod
    // Used in aerodynamic stats calculation as all FAR values are stored in doubles

    public struct MathD
    {
        public const double Deg2Rad = Math.PI / 180;
        public const double Rad2Deg = 180 / Math.PI;

        public static double Min(params double[] values)
        {
            int length = values.Length;
            if (length == 0)
            {
                return 0.0d;
            }

            double num = values[0];
            for (int index = 1; index < length; ++index)
            {
                if (values[index] < num)
                {
                    num = values[index];
                }
            }
            return num;
        }

        public static int Min(params int[] values)
        {
            int length = values.Length;
            if (length == 0)
            {
                return 0;
            }

            int num = values[0];
            for (int index = 1; index < length; ++index)
            {
                if (values[index] < num)
                {
                    num = values[index];
                }
            }
            return num;
        }

        public static double Max(params double[] values)
        {
            int length = values.Length;
            if (length == 0)
            {
                return 0d;
            }

            double num = values[0];
            for (int index = 1; index < length; ++index)
            {
                if ((double)values[index] > (double)num)
                {
                    num = values[index];
                }
            }
            return num;
        }

        public static int Max(params int[] values)
        {
            int length = values.Length;
            if (length == 0)
            {
                return 0;
            }

            int num = values[0];
            for (int index = 1; index < length; ++index)
            {
                if (values[index] > num)
                {
                    num = values[index];
                }
            }
            return num;
        }

        public static int CeilToInt(double d)
        {
            return (int)Math.Ceiling(d);
        }

        public static int FloorToInt(double d)
        {
            return (int)Math.Floor(d);
        }

        public static int RoundToInt(double d)
        {
            return (int)Math.Round(d);
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
            {
                value = min;
            }
            else if (value.CompareTo(max) > 0)
            {
                value = max;
            }

            return value;
        }

        public static double Clamp01(double value)
        {
            if (value < 0.0)
            {
                return 0.0d;
            }

            if (value > 1.0)
            {
                return 1d;
            }
            else
            {
                return value;
            }
        }

        public static double Lerp(double from, double to, double t)
        {
            return from + (to - from) * MathD.Clamp01(t);
        }

        public static double LerpAngle(double a, double b, double t)
        {
            double num = MathD.Repeat(b - a, 360d);
            if (num > 180.0d)
            {
                num -= 360d;
            }

            return a + num * MathD.Clamp01(t);
        }

        public static double MoveTowards(double current, double target, double maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
            {
                return target;
            }
            else
            {
                return current + Math.Sign(target - current) * maxDelta;
            }
        }

        public static double MoveTowardsAngle(double current, double target, double maxDelta)
        {
            target = current + MathD.DeltaAngle(current, target);
            return MathD.MoveTowards(current, target, maxDelta);
        }

        public static double SmoothStep(double from, double to, double t)
        {
            t = MathD.Clamp01(t);
            t = (-2.0 * t * t * t + 3.0 * t * t);
            return to * t + from * (1.0 - t);
        }

        public static double Gamma(double value, double absmax, double gamma)
        {
            bool flag = false;
            if (value < 0.0)
            {
                flag = true;
            }

            double num1 = Math.Abs(value);
            if (num1 > absmax)
            {
                if (flag)
                {
                    return -num1;
                }
                else
                {
                    return num1;
                }
            }
            else
            {
                double num2 = Math.Pow(num1 / absmax, gamma) * absmax;
                if (flag)
                {
                    return -num2;
                }
                else
                {
                    return num2;
                }
            }
        }

        public static bool Approximately(double a, double b)
        {
            return Math.Abs(b - a) < MathD.Max(1E-06d * MathD.Max(Math.Abs(a), Math.Abs(b)), double.Epsilon);
        }

        public static double SmoothDamp(double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed)
        {
            return MathD.SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);
        }

        public static double SmoothDamp(double current, double target, ref double currentVelocity, double smoothTime)
        {
            return MathD.SmoothDamp(current, target, ref currentVelocity, smoothTime, double.PositiveInfinity, Time.deltaTime);
        }

        public static double SmoothDamp(double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed, double deltaTime)
        {
            smoothTime = MathD.Max(0.0001d, smoothTime);
            double num1 = 2d / smoothTime;
            double num2 = num1 * deltaTime;
            double num3 = (1.0d / (1.0d + num2 + 0.479999989271164d * num2 * num2 + 0.234999999403954d * num2 * num2 * num2));
            double num4 = current - target;
            double num5 = target;
            double max = maxSpeed * smoothTime;
            double num6 = MathD.Clamp(num4, -max, max);
            target = current - num6;
            double num7 = (currentVelocity + num1 * num6) * deltaTime;
            currentVelocity = (currentVelocity - num1 * num7) * num3;
            double num8 = target + (num6 + num7) * num3;
            if (num5 - current > 0.0 == num8 > num5)
            {
                num8 = num5;
                currentVelocity = (num8 - num5) / deltaTime;
            }
            return num8;
        }

        public static double SmoothDampAngle(double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed)
        {
            return MathD.SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);
        }

        public static double SmoothDampAngle(double current, double target, ref double currentVelocity, double smoothTime)
        {
            return MathD.SmoothDampAngle(current, target, ref currentVelocity, smoothTime, double.PositiveInfinity, Time.deltaTime);
        }

        public static double SmoothDampAngle(double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed, double deltaTime)
        {
            return MathD.SmoothDamp(current, current + MathD.DeltaAngle(current, target), ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static double Repeat(double t, double length)
        {
            return t - Math.Floor(t / length) * length;
        }

        public static double PingPong(double t, double length)
        {
            t = MathD.Repeat(t, length * 2d);
            return length - Math.Abs(t - length);
        }

        public static double InverseLerp(double from, double to, double value)
        {
            if (from < to)
            {
                if (value < from)
                {
                    return 0d;
                }

                if (value > to)
                {
                    return 1d;
                }

                value -= from;
                value /= to - from;
                return value;
            }
            else
            {
                if (from <= to)
                {
                    return 0d;
                }

                if (value < to)
                {
                    return 1d;
                }

                if (value > from)
                {
                    return 0d;
                }
                else
                {
                    return (1.0d - (value - to) / (from - to));
                }
            }
        }

        public static double DeltaAngle(double current, double target)
        {
            double num = MathD.Repeat(target - current, 360d);
            if (num > 180.0d)
            {
                num -= 360d;
            }

            return num;
        }
    }
}