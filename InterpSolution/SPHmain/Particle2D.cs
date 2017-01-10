using Sharp3D.Math.Core;
using SimpleIntegrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static System.Math;

namespace SPH_2D {
    public interface IParticle2D: INamedChild  {
        /// <summary>
        /// Х координата частицы
        /// </summary>
        double X { get; }

        /// <summary>
        /// Y координата частицы
        /// </summary>
        double Y { get; }

        Vector2D Vec2D { get; set; } 

        ///// <summary>
        ///// Скорость по X
        ///// </summary>
        //double dX { get; }

        ///// <summary>
        ///// Скорость по Y
        ///// </summary>
        //double dY { get; }

        ///// <summary>
        ///// Скорость
        ///// </summary>
        //IPosition2D Vel { get; }

        /// <summary>
        /// Соседи частицы
        /// </summary>
        IList<IParticle2D> Neibs { get; set; }

        /// <summary>
        /// получить максимальный радиус сглаживания
        /// </summary>
        /// <returns></returns>
        double GetHmax();

        /// <summary>
        /// Расстояние до частицы
        /// </summary>
        /// <param name="particle">до этой частицы</param>
        /// <returns></returns>
        double GetDistTo(IParticle2D particle);

        /// <summary>
        /// Сколько зависимых операций необходимо частице на каждом шаге интегрирования
        /// </summary>
        int StuffCount { get; }

        /// <summary>
        /// Произвести зависимую операцию под индексом stuffIndex
        /// </summary>
        /// <param name="stuffIndex"></param>
        void DoStuff(int stuffIndex);


    }

    /// <summary>
    /// Абстрактный класс представляющий частицу для SPH 2D
    /// </summary>
    public abstract class Particle2DBase: Position2D, IParticle2D {
        #region IParticle 2D impl

        public IList<IParticle2D> Neibs { get; set; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetDistTo(IParticle2D particle) {
            double deltX = X - particle.X;
            double deltY = Y - particle.Y;
            return Sqrt(deltX * deltX + deltY * deltY);
        }
        public double GetHmax() {
            return hmax;
        }
        #endregion

        public double hmax;
        public Particle2DBase(double hmax) {
            this.hmax = hmax;
            Name = "Particle";

            Neibs = new List<IParticle2D>(30);

            
        }

        #region Abstract
        public abstract int StuffCount { get; }

        public abstract void DoStuff(int stuffIndex);



        #endregion

        #region Static
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static double dW_func(double r_shtr,double h) {
        //    double q = Math.Abs(r_shtr) / h;
        //    if(q > 2.0)
        //        return 0.0;

        //    double a = -2.0 / (3.0 * h * h);
        //    double result = 0;

        //    if(q < 0.66666)
        //        result = 1;
        //    else if(q >= 0.66666 && q < 1.0)
        //        result = 3.0 * q * (4.0 - 3.0 * q) / 4.0;
        //    else if(q >= 1.0 && q <= 2.0)
        //        result = 3.0 * (2.0 - q) * (2.0 - q) / 4.0;

        //    return result * a;
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static double W_func(double r_shtr,double h) {
        //    double q = Abs(r_shtr) / h;
        //    if(q > 2.0)
        //        return 0.0;
        //    double a = 2.0 / (3.0 * h);
        //    double result = 0;

        //    if(q >= 0 && q <= 1.0)
        //        result = 0.25 * (4d - 6 * q * q + 3 * q * q * q);
        //    else if(q > 1.0 && q <= 2.0)
        //        result = 0.25 * (2.0 - q) * (2.0 - q) * (2.0 - q);

        //    return result * a;
        //}

        /// <summary>
        /// Из диссертации
        /// </summary>
        /// <param name = "r_shtr" ></ param >
        /// < param name="h"></param>
        /// <returns></returns>
        public static double W_func(double r_shtr,double h) {
            double fi = r_shtr / h;
            if(fi >= 2d)
                return 0d;
            double n = 0.7 * PI * h * h; //2D
            //double n = 1.5 * h;//1D
            if(fi >= 0d) {
                if(fi < 1d)
                    return (1d - 3 * fi * fi / 2d + 3 * fi * fi * fi / 4d) / n;
                var ss = (2d - fi);
                return ss * ss * ss / (4d * n);
            }
            throw new ArgumentException("Baaaad data");

        }

        public static double dW_func(double r_shtr,double h) {
            double fi = r_shtr / h;
            if(fi >= 2d)
                return 0d;
            double n1 = 28d * PI * h * h * h; //2D
            //double n1 = 6d * h * h; //1D
            if(fi >= 0d) {
                if(fi < 1d)
                    return (-12d * fi + 9d * fi * fi) / n1;
                var ss = (2d - fi);
                return -3d * ss * ss / n1;
            }
            throw new ArgumentException("Baaaad data");

        }
        #endregion

    }


    public class Particle2DDummyBase: NamedChild, IParticle2D {
        public double X { get; set; }
        public double Y { get; set; }
        public Vector2D Vec2D {
            get {
                return new Vector2D(X,Y);
            }
            set {
                X = value.X;
                Y = value.Y;
            }
        }
        public IList<IParticle2D> Neibs { get; set; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetDistTo(IParticle2D particle) {
            double deltX = X - particle.X;
            double deltY = Y - particle.Y;
            return Sqrt(deltX * deltX + deltY * deltY);
        }
        public double GetHmax() {
            return hmax;
        }

        public double hmax;
        public Particle2DDummyBase(double hmax) {
            this.hmax = hmax;
            Name = "Dummy";

            Neibs = null;
        }

        public int StuffCount { get; } = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoStuff(int stuffIndex) { }
    }
}
