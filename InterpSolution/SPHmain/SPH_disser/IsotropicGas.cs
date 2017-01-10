using Sharp3D.Math.Core;
using SimpleIntegrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPH_2D {
    public class IsotropicGasParticle : Particle2DBase {
        public bool isWall = false;

        public double D;
        public double alpha = 1.4d; //для формулы 1.19

        public double M { get; set; }

        public double Ro { get; set; }
        public IScnPrm pRo { get; set; }

        public double dRo { get; set; }
        public IScnPrm pdRo { get; set; }

        public double E { get; set; }
        public IScnPrm pE { get; set; }

        public double dE { get; set; }
        public IScnPrm pdE { get; set; }

        public double P { get; set; }
        public IScnPrm pP { get; set; }

        public Position2D dV { get; set; }

        public double k = 1.4;

        public IPosition2D Vel { get; private set; }
        public double dX {
            get {
                return Vel.X;
            }

            set {
                Vel.X = value;
            }
        }

        public double dY {
            get {
                return Vel.Y;
            }

            set {
                Vel.Y = value;
            }
        }

        #region Constructor + Abstracts realiz
        public IsotropicGasParticle(double d, double hmax) : base(hmax) {

            Vel = new Position2D();
            Vel.Name = "Vel";
            AddChild(Vel);

            AddDiffVect(Vel);
            this.D = d;

            dV = new Position2D();
            dV.Name = "dV";
            AddChild(dV);
            Vel.AddDiffVect(dV,false);
           

           // AddDiffPropToParam(pRo,pdRo);
            AddDiffPropToParam(pE,pdE);
        }

        public override int StuffCount {
            get {
                return 2;
            }
        }

        public override void DoStuff(int stuffIndex) {
            switch(stuffIndex) {
                case 0: {
                    SetP();
                    break;
                }
                case 1: {
                    if(!isWall)
                        FillDts();
                    break;
                }

                default:
                break;
            }
        }

        public virtual void FillDts() {
            foreach(var neib in Neibs.Where(n=>GetDistTo(n) < hmax).Cast<IsotropicGasParticle>()) {
                double h = alpha * (D + neib.D) * 0.5;
                double dw = dW_func(GetDistTo(neib),h);
                if(dw == 0d)
                    continue;
                double m_j = neib.M;
                double Ro_j = neib.Ro;
                double P_j = neib.P;
                double Cl_j = neib.GetCl(); //скорость звука
                double Cl_i = neib.GetCl();

                double x = X;

                Vector2D Rji_norm = (neib.Vec2D - Vec2D).Norm;
                double U_Ri = Vel.Vec2D * Rji_norm;
                double U_Rj = neib.Vel.Vec2D * Rji_norm;
                double U_starRij = (U_Rj * Ro_j * Cl_j + U_Ri * Ro * Cl_i - P_j + P) / (Ro_j * Cl_j + Ro * Cl_i);// 1.20
                double P_starij = (P_j * Ro * Cl_i + P * Ro * Cl_j + Ro_j * Cl_j * Ro * Cl_i * (U_Rj - U_Rj)) / (Ro_j * Cl_j + Ro * Cl_i); //1.21

                double mn4VandE = 2d * m_j * P_starij / (Ro * Ro_j)* dw;

                dRo += -2d * m_j * Ro / Ro_j * dw * (U_Ri - U_starRij); //1.24
                var dVelVec = mn4VandE  * Rji_norm;
                dV.X += dVelVec.X;
                dV.Y += dVelVec.Y;
                dE += -0.5*mn4VandE * (U_Ri - U_starRij);//1.26
                if(X == 0d) {
                    int i = 1;
                }
            }



        }

        public virtual void SetP() {

            //Ro = Neibs.Cast<IsotropicGasParticle>().Sum(n => {
            //    double h = alpha * (D + n.D) * 0.5;
            //    double w = W_func(GetDistTo(n),h);
            //    return n.M * w;

            //}) + M* W_func(0,1);
            P = (k - 1d) * Ro * E;
            dRo = 0d;
            dE = 0d;
            dV.Vec2D = Vector2D.Zero;
        }
    
        /// <summary>
        /// Получить скорость звука
        /// </summary>
        /// <returns></returns>
        public double GetCl() {
            return 340;
        }
        #endregion
    }   
}
