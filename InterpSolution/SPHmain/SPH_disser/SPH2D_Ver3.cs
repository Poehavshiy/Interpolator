using Microsoft.Research.Oslo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace SPH_2D {
    public static class SPH2D_Ver3 {
        public static double h_default = 1d / 300;
        public static GasParticleOptions options = null;
        public static double dt = 1E-6;
        public static double k = 1.4;
        #region Helpers
        struct W_helper {
            public double x, y, w;
            public W_helper(double x,double y,double w) {
                this.x = x;
                this.y = y;
                this.w = w;
            }
        }
        public static double GetMass(double delta,double h,double ro) {
            double x = 0;
            double y = 0;
            double f;
            var lst = new List<W_helper>();

            do {
                do {
                    f = Particle2DBase.W_func(Sqrt(x * x + y * y),h);
                    lst.Add(new W_helper(x,y,f));
                    x += delta;
                } while(f > 0d);
                y += delta;
                x = 0;
                f = Particle2DBase.W_func(Sqrt(x * x + y * y),h);
            } while(f > 0d);

            double sum = 0;
            sum += lst.Where(wh => wh.x == 0 ^ wh.y == 0).Sum(wh => wh.w * 2);
            sum += lst.Where(wh => wh.x == 0 && wh.y == 0).Sum(wh => wh.w);
            sum += lst.Where(wh => wh.x != 0 && wh.y != 0).Sum(wh => wh.w * 4);

            return ro / sum;
        }
        #endregion

        /// <summary>
        /// Test 1D tube
        /// </summary>
        /// <param name="initcond"> [0]длина трубы;
        /// [1]высота трубы;
        /// [2]отностиельная граница по Икс
        /// [3]Давление слева
        /// [4]Плотность слева
        /// [5]Давление справа
        /// [6]Плотность справа
        /// [7]Интервал между частицами</param>
        /// <returns></returns>
        public static Sph2D TestTruba(params double[] initcond) {
            var t = TestTrubaParticles(initcond);
            return new Sph2D(t.Item1,t.Item2);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="initcond">[0]длина трубы;
        /// [1]высота трубы;
        /// [2]отностиельная граница по Икс
        /// [3]Давление слева
        /// [4]Плотность слева
        /// [5]Давление справа
        /// [6]Плотность справа
        /// [7]Интервал между частицами
        /// </param>
        /// <returns></returns>
        public static Tuple<IEnumerable<GasParticleVer3>,IEnumerable<IGasParticleVer3>> TestTrubaParticles(params double[] initcond) {
            double lt = initcond[0]; //длина трубы
            double ht = initcond[1]; //высота трубы
            double x0t = initcond[2];//отностиельная граница по Икс
            double p1t = initcond[3]; //Давление слева
            double ro1t = initcond[4]; //Плотность слева
            double p2t = initcond[5]; //Давление справа
            double ro2t = initcond[6]; //Плотность справа
            double delta0 = initcond[7]; //Плотность справа

            options = options ?? GasParticleOptions.DiffOnlyE();

            var bounds = new List<BorderSegment>(4);
            bounds.Add(new BorderSegment(0,0,0,ht));
            bounds.Add(new BorderSegment(0,ht,lt,ht));
            bounds.Add(new BorderSegment(lt,ht,lt,0));
            bounds.Add(new BorderSegment(lt,0,0,0));

            int Nx = (int)Round(lt / delta0);
            double dx = lt / Nx;

            int Ny = (int)Round(ht / delta0);
            double dy = ht / Ny;

            double delta = Min(dx,dy);
            double m1 = GetMass(delta,h_default,ro1t);
            double m2 = GetMass(delta,h_default,ro2t);

            var particles = new List<GasParticleVer3>(Nx * Ny + 10);

            double x = 0.5 * delta;
            do {
                double y = 0.5 * delta;
                do {
                    var p = new GasParticleVer3(h_default,2 * h_default,bounds);
                    p.X = x;
                    p.Y = y;
                    p.P = x < lt * x0t ? p1t : p2t;
                    p.Ro = x < lt * x0t ? ro1t : ro2t;
                    p.M = x < lt * x0t ? m1 : m2;
                    p.k = k;
                    particles.Add(p);

                    y += delta;
                } while(y < ht);
                x += delta;

            } while(x < lt);

            particles.ForEach(p => {
                p.E = p.P / ((p.k - 1d) * p.Ro);
            });
            return new Tuple<IEnumerable<GasParticleVer3>,IEnumerable<IGasParticleVer3>>(particles,null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="initcond"> [0]давление внутри шара;
        /// [1]плотность внутри шара;
        /// [2]скорость шара (мах)</param>
        /// <returns></returns>
        public static Sph2D TestGasBall(params double[] initcond) {
            var t = TestGasBallParticles(initcond);
            return new Sph2D(t.Item1,t.Item2);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="initcond"> [0]давление внутри шара;
        /// [1]плотность внутри шара;
        /// [2]скорость шара (мах)</param>
        /// <returns></returns>
        public static Tuple<IEnumerable<GasParticleVer3>,IEnumerable<IGasParticleVer3>> TestGasBallParticles(params double[] initcond) {
            options = options ?? GasParticleOptions.DiffOnlyE();
            double p1 = initcond[0];
            double ro1 = initcond[1];
            double mach1 = initcond[2];
            //
            double e_check = 4.29 * 10E6;
            ro1 = 1.63;
            p1 = 0.65 * e_check;

            double E = p1 / ((k - 1d) * ro1);
            double Vx = (k * (k - 1) * E) * mach1;

            double ht = 1;
            double lt = 1;

            int Nx = (int)Round(lt / h_default);
            double delta = lt / Nx;

            int Ny = Nx;
            double m1 = GetMass(delta,h_default,ro1);

            var particles = new List<GasParticleVer3>(Nx * Ny + 10);
            var center = new Particle2DDummyBase(1) {
                X = lt * 0.5,
                Y = lt * 0.5
            };

            var bounds = new List<BorderSegment>(1);
           // bounds.Add(new BorderSegment(lt + 0.5 * delta,-0.25 * lt,lt + 0.5 * delta,1.5 * lt));

            double x = 0.5 * delta;
            double y;
            do {
                y = 0.5 * delta;
                do {
                    var p = new GasParticleVer3(h_default,2.01 * h_default,bounds);
                    p.X = x;
                    p.Y = y;
                    p.P = p1;
                    p.Ro = ro1;
                    p.M = m1;
                    p.E = E;
                    p.Vel.X = Vx;
                    p.k = k;
                    if(p.GetDistTo(center) < lt * 0.5)
                        particles.Add(p);

                    y += delta;
                } while(y < lt);
                x += delta;

            } while(x < lt);

            var borders = new List<IGasParticleVer3>(400);
            borders.Add(new GasParticleVer3Dummy(2 * h_default) { X = -0.25 * lt*5,Y = -0.25 * lt*5,isboundary = true });
            borders.Add(new GasParticleVer3Dummy(2 * h_default) { X = -0.25 * lt*5,Y = 1.25 * lt*5,isboundary = true });

            //borders.Add(new GasParticleVer3Dummy(2 * h_default) { X = 1.25 * lt,Y = -0.25 * lt,isboundary = true });
            //borders.Add(new GasParticleVer3Dummy(2 * h_default) { X = 1.25 * lt,Y = 1.25 * lt,isboundary = true });

            y = -0.25 * lt;
            //do {
            //    borders.Add(new GasParticleVer3Dummy(2 * h_default) { X = bounds[0].p1.X + h_default,Y = y,isboundary = true });
            //    y += (delta / 5);
            //} while(y < 1.25 * lt);

            return new Tuple<IEnumerable<GasParticleVer3>,IEnumerable<IGasParticleVer3>>(particles,borders);

        }

        public static IEnumerable<SolPoint> CoolIntegration(Tuple<IEnumerable<GasParticleVer3>,IEnumerable<IGasParticleVer3>> tuple) {
            return CoolIntegration(tuple.Item1,tuple.Item2);
        }
        public static IEnumerable<SolPoint> CoolIntegration(IEnumerable<GasParticleVer3> particles,IEnumerable<IGasParticleVer3> wall) {
            var sph = new Sph2D_improoveIntegr(particles,wall);
            return CoolIntegration(sph);
        }
        public static IEnumerable<SolPoint> CoolIntegration(Sph2D_improoveIntegr sph) {
            if(!(options.diffRo && options.diffE && options.diffFullE))
                throw new Exception("неправильные частицы!");

            double t = sph.TimeSynch;
            Vector x = sph.Rebuild(sph.TimeSynch);
            int n = x.Length;

            // Output initial point
            yield return new SolPoint(t,x.Clone());

            while(true) // Can produce any number of solution points
            {
                yield return sph.StepUpNplus1(dt);
            }


        }
    }
}
