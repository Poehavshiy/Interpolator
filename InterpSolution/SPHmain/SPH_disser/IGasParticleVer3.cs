using Sharp3D.Math.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPH_2D {
    public interface IGasParticleVer3 : IParticle2D {
        Vector2D VelVec2D { get; set; }
        bool isboundary { get; }
        double M { get; set; }
        double Ro { get; set; }
        double E { get; set; }
        double P { get; set; }
        double C { get; }
    }

    public class GasParticleVer3Dummy : Particle2DDummyBase, IGasParticleVer3 {
        public GasParticleVer3Dummy(double hmax) : base(hmax) {
        }

        public bool isboundary { get; set; } = false;
        public double M { get; set; }

        public double Ro { get; set; }

        public double E { get; set; }

        public double P { get; set; }

        public Vector2D VelVec2D { get; set; }

        public double C { get; set; }
    }
}
