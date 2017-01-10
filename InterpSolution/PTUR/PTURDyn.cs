using Sharp3D.Math.Core;
using SimpleIntegrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace PTUR {
    public class PTURDyn: Position2D {
        double p1, p2, t1, t2 ,mass0;
        double GetP(double t) {
            if(t < 0)
                return 0;
            if(t < t1)
                return p1;
            if(t < t2)
                return p2;
            return 0;
        }
        public Func<double,Vector2D> targFunc;

        public double V_val { get; set; }
        public IScnPrm pV_val { get; set; }

        public double dV_val { get; set; }
        public IScnPrm pdV_val { get; set; }

        public Position2D Vel { get; set; }
        public string Filter { get; internal set; }

        public static Vector2D TargetPosition1(double t) {
            var res = new Vector2D();
    
            res.X = -10*t + 100;
            res.Y = 20*t + 600;

            return res;
        }
        public static Vector2D TargetPosition2(double t) {
            var res = new Vector2D();

            res.X = 10 * t;
            res.Y = 20 * t;

            return res;
        }
        public static Vector2D TargetPosition3(double t) {
            var res = new Vector2D();

            res.X = 10 * t;
            res.Y = 20 * t;

            return res;
        }


        void FillPrav(double t) {
            dV_val = 0;
            Vel.Vec2D = (targFunc(t) - Vec2D).Norm * V_val;
        }

        public PTURDyn():this(1,1,1,1,TargetPosition1) {

        }

        public PTURDyn(double p1, double p2, double t1, double t2, Func<double, Vector2D> targFunc) {
            this.p1 = p1;
            this.p2 = p2;
            this.t1 = t1;
            this.t2 = t2;
            this.targFunc = targFunc;
            mass0 = 10 + (p1 * t1 / 2600 + p2 * t2 / 2600) * 1.2 + 2;

            Vel = new Position2D();
            AddChild(Vel);
            AddDiffVect(Vel);

            AddDiffPropToParam(pV_val,pdV_val);
            SynchMeAfter += FillPrav;
        }
    }
}
