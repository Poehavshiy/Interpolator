using Sharp3D.Math.Core;
using SimpleIntegrator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace SPH_2D {
    public class GasParticleOptions {
        public bool diffRo { get; }
        public bool diffE { get; }
        public bool diffFullE { get; }
        protected GasParticleOptions(bool diffRo, bool diffE,bool diffFullE) {
            this.diffRo = diffRo;
            this.diffE = diffE;
            this.diffFullE = diffFullE;
        }
        public static GasParticleOptions DiffOnlyE() {
            return new GasParticleOptions(false,true,false);
        }
        public static GasParticleOptions DiffRoDiffE() {
            return new GasParticleOptions(true,true,false);
        }
        public static GasParticleOptions DiffRoDiffFullE() {
            return new GasParticleOptions(true,false,true);
        }
        public static GasParticleOptions DiffAll() {
            return new GasParticleOptions(true,true,true);
        }
    }

    public class GasParticleVer3: Particle2DBase, IGasParticleVer3 {
        public bool isboundary { get; } = false;
        public List<BorderSegment> Boundaries { get; private set; }
        public double h;
        public double M { get; set; }

        public double Ro { get; set; }
        public IScnPrm pRo { get; set; }

        public double dRo { get; set; }
        public IScnPrm pdRo { get; set; }

        public double E { get; set; }
        public IScnPrm pE { get; set; }

        public double dE { get; set; }
        public IScnPrm pdE { get; set; }

        public double FullE { get; set; }
        public IScnPrm pFullE { get; set; }

        public double dFullE { get; set; }
        public IScnPrm pdFullE { get; set; }

        public double P { get; set; }
        public IScnPrm pP { get; set; }

        public double k = 1.4;

        public Position2D Vel { get; private set; }
        
        public Position2D dVdt { get; set; }

        public Vector2D VelVec2D {
            get {
                return Vel.Vec2D;
            }
            set {
                Vel.Vec2D = value;
            }
        }

        public Vector2D dVdtVec2D {
            get {
                return dVdt.Vec2D;
            }
            set {
                dVdt.Vec2D = value;
            }
        }

        public ConcurrentStack<IParticle2D> MiracleStack = new ConcurrentStack<IParticle2D>();
        public List<IGasParticleVer3> BestNeibs;
        GasParticleOptions options;
        public double EpsDot;

        public GasParticleVer3(double h,double hmax, List<BorderSegment> boundaries = null, GasParticleOptions opts = null) : base(hmax) {
            options = opts ?? GasParticleOptions.DiffRoDiffE();
            Boundaries = boundaries ?? new List<BorderSegment>();
            this.h = h;

            Vel = new Position2D();
            Vel.Name = "Vel";
            AddChild(Vel);

            AddDiffVect(Vel);

            dVdt = new Position2D();
            dVdt.Name = "dV";
            AddChild(dVdt);
            Vel.AddDiffVect(dVdt,false);

            if(options.diffRo) {
                AddDiffPropToParam(pRo,pdRo);
            }
            if(options.diffE) {
               AddDiffPropToParam(pE,pdE);
            }
            if(options.diffFullE) {
                AddDiffPropToParam(pFullE,pdFullE);
            }
        }
        
        public override int StuffCount {
            get {
                return 4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void DoStuff(int stuffIndex) {
            switch(stuffIndex) {
                case 0: {
                    SetP();
                    break;
                }
                case 1: {
                    MiracleMe();
                    break;
                }
                case 2: {
                    FillBestNeibsPlusRO();
                    break;
                }
                case 3: {
                    FillDts_disser();
                    //FillDts_Nik();
                    break;
                }

                default:
                break;
            }
        }
        public virtual void SetP() {

            //Ro = Neibs.Cast<IGasParticleVer3>().Sum(n => {
            //    double w = W_func(GetDistTo(n),h);
            //    return n.M * w;

            //});
            P = (k - 1d) * Ro * E;
            FullE = E + VelVec2D.GetLengthSquared() * 0.5;
            dRo = 0d;
            dE = 0d;
            dFullE = 0d;
            EpsDot = 0d;
            dVdt.Vec2D = Vector2D.Zero;
            MiracleStack.Clear();
        }
        private void MiracleMe() {
            foreach(var bound in Boundaries) {
                if(bound.CloseToMe(this,hmax)) {
                    var mirrorClone = CreateMiracleDummy(bound);
                    foreach(var neib in Neibs) {
                        if((neib is GasParticleVer3) && !(neib as GasParticleVer3).isboundary) {
                            (neib as GasParticleVer3).MiracleStack.Push(mirrorClone);
                        }
                    }
                }
            }
        }

        private void FillBestNeibsPlusRO() {
            BestNeibs = Neibs.Where(n => !ReferenceEquals(n,this)).Concat(MiracleStack).Where(n => GetDistTo(n) < hmax).Cast<IGasParticleVer3>().ToList();
            if(!options.diffRo) {
                Ro = BestNeibs.Where(bn => !bn.isboundary).Sum(n => {
                    double w = W_func(GetDistTo(n),h);
                    return n.M * w;
                });
                Ro += M * W_func(0,h);
            }

        }

        public void FillDts_Nik() {

            foreach(var neib in BestNeibs)
            {
                Vector2D deltaV = Vel.Vec2D - neib.VelVec2D;
                Vector2D deltaR = Vec2D - neib.Vec2D;
                if(!neib.isboundary) {

                    double mj = neib.M;
                    double r = deltaR.GetLength();
                    //
                    double H = 0;
                    double scalar = deltaV * deltaR;
                    if(scalar < 0) {
                        double alpha_ = 0.9;
                        double beta_ = 0.9;
                        double phi_ = 0.1;
                        double phi = hmax * scalar / (Math.Pow(r,2) + Math.Pow(phi_ * hmax,2));
                        double Ro_ = (Ro + neib.Ro) / 2;
                        double C_ = (C + neib.C) / 2;

                        H = (-alpha_ * C_ * phi + beta_ * Math.Pow(phi,2)) / Ro_;

                    }
                    //
                    double brackets = mj * (P / (Ro * Ro) + neib.P / (neib.Ro * neib.Ro) + H);
                    if(H > 1) {
                        double res = brackets / H;
                        int a = 0;
                    }
                    Vector2D dW = new Vector2D((deltaR.X / r) * dW_func(r,hmax),(deltaR.Y / r) * dW_func(r,hmax));
                    //плотность
                    if(options.diffRo)
                        dRo += -mj *Ro* dW_func(r, hmax)/neib.Ro;
                    //скорости
                    dVdt.X += -brackets * dW.X;
                    dVdt.Y += -brackets * dW.Y;
                    //энергия
                    //if(options.diffE)
                        dE += 0.5 * brackets * deltaV * dW;
                } else {
                    double r0 = hmax/5;
                    double rij = deltaR.GetLength();
                    if(r0 / rij <= 1) {
                        double n1 = 12;
                        double n2 = 4;
                        double D = (Vel.Vec2D.GetLength() > neib.VelVec2D.GetLength()) ?
                            Vel.Vec2D.GetLength() * Vel.Vec2D.GetLength() :
                            neib.VelVec2D.GetLength() * neib.VelVec2D.GetLength();
                        D = D * 2;
                        double brackets = -D * (Math.Pow(r0 / rij,n1) - Math.Pow(r0 / rij,n2)) / (rij * rij);
                        double PDijx = brackets * deltaR.X;
                        double PDijy = brackets * deltaR.Y;
                        //
                        dVdt.X += PDijx / M;
                        dVdt.Y += PDijy / M;
                    }
                }
            }
        }



        /// <summary>
        /// Получить скорость звука
        /// </summary>
        /// <returns></returns>
        public double C {
            get {
                return Sqrt(P / Ro);
                //return (k * (k - 1) * E);
            }
        }
        

        public GasParticleVer3Dummy CreateMiracleDummy(BorderSegment mirrorSegment) {
            var res = new GasParticleVer3Dummy(hmax);
            res.Name = "MirrorOf" + Name;
            res.Owner = this;
            res.M = M;
            res.Ro = Ro;
            res.E = E;
            res.P = P;
            res.C = C;
            res.Vec2D = mirrorSegment.ReflectPos(Vec2D);
            res.VelVec2D = mirrorSegment.ReflectVel(VelVec2D);

            return res;
        }

        public void FillDts_disser() {
            foreach(var neib in BestNeibs) {
                if(!neib.isboundary) {
                    // double h = 1 * (D + neib.D) * 0.5;
                    double dw = dW_func(GetDistTo(neib),h);
                    if(dw == 0d)
                        continue;
                    double m_j = neib.M;
                    double Ro_j = neib.Ro;
                    double P_j = neib.P;
                    double Cl_j = neib.C; //скорость звука
                    double Cl_i = C;

                    double x = X;

                    Vector2D Rji_norm = (neib.Vec2D - Vec2D).Norm;
                    double U_Ri = Vel.Vec2D * Rji_norm;
                    double U_Rj = neib.VelVec2D * Rji_norm;
                    double U_starRij = (U_Rj * Ro_j * Cl_j + U_Ri * Ro * Cl_i - P_j + P) / (Ro_j * Cl_j + Ro * Cl_i);// 1.20
                    double P_starij = (P_j * Ro * Cl_i + P * Ro * Cl_j + Ro_j * Cl_j * Ro * Cl_i * (U_Rj - U_Ri)) / (Ro_j * Cl_j + Ro * Cl_i); //1.21

                    double mn4VandE = 2d * m_j * P_starij / (Ro * Ro_j) * dw;
                    
                    dRo += -2d * m_j * Ro / Ro_j * dw * (U_Ri - U_starRij); //1.24
                    var dVelVec = mn4VandE * Rji_norm;
                    dVdt.X += dVelVec.X;
                    dVdt.Y += dVelVec.Y;
                    dE += -0.5*mn4VandE * (U_Ri - U_starRij);//1.26
                    dFullE += -0.5 * mn4VandE * U_starRij;
                    if(U_starRij != 0) {
                        int hh = 0;
                    }

                    EpsDot += -2 * m_j * dw * (U_Ri - U_starRij)/ Ro;
                } else {
                    Vector2D deltaR = Vec2D - neib.Vec2D;
                    double r0 = hmax / 15;
                    double rij = deltaR.GetLength();
                    if(r0 / rij <= 1) {
                        double n1 = 12;
                        double n2 = 4;
                        double D = (Vel.Vec2D.GetLength() > neib.VelVec2D.GetLength()) ?
                            Vel.Vec2D.GetLength() * Vel.Vec2D.GetLength() :
                            neib.VelVec2D.GetLength() * neib.VelVec2D.GetLength();
                        D = D * 2;
                        double brackets = -D * (Math.Pow(r0 / rij,n1) - Math.Pow(r0 / rij,n2)) / (rij * rij);
                        double PDijx = brackets * deltaR.X;
                        double PDijy = brackets * deltaR.Y;
                        //
                        dVdt.X += PDijx / M;
                        dVdt.Y += PDijy / M;
                    }
                }
            }



        }
    }

    
}
