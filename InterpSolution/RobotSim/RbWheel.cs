using Sharp3D.Math.Core;
using SimpleIntegrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace RobotSim {
    public class RbWheel: Orient3D {
        public double Ix { get; set; }
        public double Mass { get; set; }
        public double Mx { get; set; }

        public Vector3D localPos;

        public double R;
        public double R_max;
        public double H_wheel;
        public double kR;
        public double muR;
        public double H_zac;
        public double kH;
        public double muH;

        public int n_shag;
        public double gamma;

        public double OmegaX { get; set; }
        public IScnPrm pOmegaX { get; set; }

        public double Betta { get; set; }
        public IScnPrm pBetta { get; set; }

        public RbWheel(
            int n, 
            double R, 
            double R_max,
            double H_wheel, 
            double H_zac,
            double kR = 1E5, 
            double muR = 1E4, 
            double kH = 1E5,
            double muH = 1E4):base("Wheel") 
            
        {
            n_shag = n;
            this.H_zac = H_zac;
            this.H_wheel = H_wheel;
            this.R_max = R_max;
            this.R = R;
            this.kR = kR;
            this.muR = muR;
            this.kH = kH;
            this.muH = muH;
            this.localPos = Vector3D.Zero;

            gamma = 2 * PI / n;

            AddDiffPropToParam(pBetta,pOmegaX);
        }
        public static RbWheel GetStandart() {
            return GetStandart(Vector3D.Zero);
        }

        public static RbWheel GetStandart(Vector3D localPos) {
            int n = 11;
            double r_real = 0.015;
            double R_ideal = 0.009 / (2 * Sin(PI / n));
            var mass = PI * r_real * r_real * 0.021 * 1080;
            var ix = mass * r_real * r_real / 2;
            var res = new RbWheel(n,R_ideal,R_ideal + 0.002,0.01,0.009 / 4) {
                Mass = 0.002242,
                Ix = ix
            };
            res.localPos = localPos;
            return res;
        }

        /// <summary>
        /// Вычисляется локальная сила, действующая в радиальном направлении и направлении оси Х
        /// </summary>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3D GetLocalRH_wheelForce(Vector3D localPoint) {
            var rlocalVec = new Vector3D(0,localPoint.Y,localPoint.Z);
            var rp = rlocalVec.GetLength();
            if(rp>R_max)
                return Vector3D.Zero;

            Vector3D f_r = rp > R 
                ? Vector3D.Zero 
                : (R - rp) * kR * rlocalVec.Norm;
            Vector3D f_h = Abs(localPoint.X) > H_wheel * 0.5
                ? Vector3D.Zero 
                : -kH * (new Vector3D(localPoint.X,0,0));

            return f_r + f_h;
        }

        /// <summary>
        /// Вычисляется локальная сила, действующая в тангенциальном направлении (только в окрестностях "зубцов")
        /// </summary>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3D GetlocalTauForce(Vector3D localPoint) {
            
            var rlocalVec = new Vector3D(0,localPoint.Y,localPoint.Z);
            var rp = rlocalVec.GetLength();
            if(rp > R_max || Abs(localPoint.X) > H_wheel * 0.5)
                return Vector3D.Zero;

            int closestInd = GetClosestInd(localPoint);
            var angle0 = Betta % (2 * PI) + gamma * closestInd;
            
            var n0 = new Vector3D(0,Cos(angle0+0.5*PI),Sin(angle0 + 0.5 * PI));
            var dh = n0 * rlocalVec;

            if(Abs(dh) > H_zac)
                return Vector3D.Zero;

            return -kH * dh * n0;
        }

        /// <summary>
        /// Получить индекс ближайшего зубца
        /// </summary>
        /// <param name="localPoint"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetClosestInd(Vector3D localPoint) {
            const double _2PI = 2 * PI;
            var angle = Atan2(localPoint.Z,localPoint.Y);
            angle += angle < 0 ? _2PI : 0;
            var betta = Betta % _2PI;
            var bettaOtn = angle - betta;
            bettaOtn += bettaOtn < 0 ? _2PI : 0;
            int answ = (int)Round(bettaOtn / gamma);
            answ = answ == n_shag ? 0 : answ;
            return answ;
        }

        //public Vector3D GetWorldPos(int spikeIndex) {

        //}
    }
}
