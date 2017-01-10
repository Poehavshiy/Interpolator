using Sharp3D.Math.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SPH_2D {
    /// <summary>
    /// отрезок
    /// </summary>
    public class BorderSegment {
        public Vector2D p1, p2;
        public double A, B, C;
        public BorderSegment(double x1,double y1,double x2,double y2) {
            p1 = new Vector2D(x1,y1);
            p2 = new Vector2D(x2,y2);
            CalcABC();
        }
        public BorderSegment(Vector2D p1,Vector2D p2) {
            this.p1 = p1;
            this.p2 = p2;
            CalcABC();
        }
        public void CalcABC() {
            A = p1.Y - p2.Y;
            B = p2.X - p1.X;
            C = p1.X * p2.Y - p2.X * p1.Y;
        }

        /// <summary>
        /// Возвращает вектор, перпендикулярный прямой, начало которого в точке fromMe.Vec2D, а конец на прямой. 
        /// </summary>
        /// <param name="fromMe"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2D GetNormalToMe(Vector2D fromMe) {
            double A_ = -B, B_ = A, C_ = B * fromMe.X - A * fromMe.Y;
            double znam = A * B_ - A_ * B;
            if(Math.Abs(znam) > 1E-12) {
                double X = -(C * B_ - C_ * B) / znam - fromMe.X;
                double Y = -(A * C_ - A_ * C) / znam - fromMe.Y;
                return new Vector2D(X,Y);
            }
            return new Vector2D(fromMe.X,fromMe.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2D GetNormalToMe(IParticle2D fromMe) {
            return GetNormalToMe(fromMe.Vec2D);
        }

        /// <summary>
        /// Показывает, находится ли точка в окрестности отрезка
        /// </summary>
        /// <param name="particle"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public bool CloseToMe(IParticle2D particle,double h) {
            var H = GetNormalToMe(particle);
            if(H.GetLength() > h)
                return false;

            var normalGlobal = H + particle.Vec2D;
            var vHloc = normalGlobal - p1;
            var p2loc = p2 - p1;
            var vdelta = p2loc.Norm * h;
            vHloc += vdelta;
            p2loc += 2 * vdelta;
            return p2loc * vHloc > 0 && p2loc.GetLengthSquared() > vHloc.GetLengthSquared();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2D ReflectPos(Vector2D pos) {
            return pos + 2 * GetNormalToMe(pos);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2D ReflectVel(Vector2D vel) {
            return vel + 2 * GetNormalToMe(p1 + vel);
        }
    }
}
