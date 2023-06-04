using System;
using System.Collections.Generic;
using System.Text;

namespace BanCa.Libs
{
    public class Vector
    {
        public static readonly Vector Right = new Vector(1, 0);

        public float X;
        public float Y;

        public Vector()
        {
            X = Y = 0;
        }

        public Vector(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector Rotate(float rad)
        {
            var sin = Util.Sin(rad);
            var cos = Util.Cos(rad);
            var X2 = X * cos - Y * sin;
            var Y2 = X * sin + Y * cos;
            X = X2;
            Y = Y2;
            return this;
        }

        public Vector Translate(Vector v)
        {
            X += v.X;
            Y += v.Y;
            return this;
        }

        public Vector Translate(float x, float y)
        {
            X += x;
            Y += y;
            return this;
        }

        public float SquareLength
        {
            get
            {
                return X * X + Y * Y;
            }
        }

        public Vector Set(Vector v)
        {
            X = v.X;
            Y = v.Y;
            return this;
        }

        public Vector Set(float x, float y)
        {
            X = x;
            Y = y;
            return this;
        }

        public float SquareDistance(float x, float y)
        {
            var a = X - x;
            var b = Y - y;
            return a * a + b * b;
        }

        public float SquareDistance(Vector v)
        {
            var a = X - v.X;
            var b = Y - v.Y;
            return a * a + b * b;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", X, Y);
        }

        public Vector Normalize()
        {
            var l = Math.Sqrt(SquareLength);
            X = (float)(X / l);
            Y = (float)(Y / l);
            return this;
        }

        public Vector Mul(float k)
        {
            X *= k;
            Y *= k;
            return this;
        }

        public float GetAngle()
        {
            return -(float)Math.Atan2(perpDot(Right), Dot(Right));
        }

        public float Dot(Vector v)
        {
            var dot = (this.X * v.X + this.Y * v.Y);
            return dot;
        }

        public float perpDot(Vector v)
        {
            var pdot = this.X * v.Y - this.Y * v.X;
            return pdot;
        }

        public SimpleJSON.JSONNode ToJson()
        {
            var data = new SimpleJSON.JSONObject();
            data["x"] = X;
            data["y"] = Y;
            return data;
        }

        public void ParseJson(SimpleJSON.JSONNode data)
        {
            X = data["x"].AsFloat;
            Y = data["y"].AsFloat;
        }

        public Vector Add(Vector v)
        {
            X += v.X;
            Y += v.Y;
            return this;
        }

        public Vector Add(float x, float y)
        {
            X += x;
            Y += y;
            return this;
        }
    }
}
