using Microsoft.Research.Oslo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace MassDrummer {
    public class Solver {
        public static double GetCylIntegrX(Func<double, double> f_ot_x, double a, double b) {
            var f2 = new Func<double,Vector,Vector>((x, v) => new Vector(Pow(f_ot_x(x),2)));
            var v0 = new Vector(0);
            var sol = Ode.Adams4(a,v0,f2,(b - a) / 300).SolveTo(b);
            return PI*sol.Last().X[0];
        }


    }
}
