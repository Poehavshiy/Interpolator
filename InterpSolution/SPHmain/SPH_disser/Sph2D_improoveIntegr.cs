using Microsoft.Research.Oslo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SPH_2D {
    public class Sph2D_improoveIntegr : Sph2D {
        public SolPoint StepUpNplus1(double dt, ref SolPoint spN) {
            SynchMeTo(spN);
            return StepUpNplus1(dt,false);
        }
        public SolPoint StepUpNplus1(double dt, bool needSynchBefore = true) {
            if(needSynchBefore)
                SynchMe(TimeSynch);
            foreach(var pp in Particles) {
                if(pp.Name == "particle0") {
                    int g = 0;
                }
                    
                var p = pp as GasParticleVer3;
                if(p == null)
                    continue;
                var VelNplus1 = p.VelVec2D + dt * p.dVdtVec2D;
                var kinEnergyN = 0.5 * p.VelVec2D.GetLengthSquared();
                var kinEnergyNplus1 = 0.5 * VelNplus1.GetLengthSquared();
                var deltaFullEnergy = p.dFullE * dt;//1.80

                p.E += deltaFullEnergy - (kinEnergyNplus1 - kinEnergyN);
                p.Ro *= (2d - p.EpsDot * dt) / (2d + p.EpsDot * dt);

                p.Vec2D += 0.5 * (VelNplus1 + p.Vel.Vec2D)*dt;
                p.Vel.Vec2D = 0.5 * (VelNplus1 + p.Vel.Vec2D);
            }
            TimeSynch += dt;
            return new SolPoint(TimeSynch,VectorCurrent);
        }
        

        public Sph2D_improoveIntegr(IEnumerable<IParticle2D> integrParticles,IEnumerable<IParticle2D> wall) : base(integrParticles,wall) {
        }

        public Sph2D_improoveIntegr(Tuple<IEnumerable<GasParticleVer3>,IEnumerable<IGasParticleVer3>> tuple): this(tuple.Item1, tuple.Item2) {

        }
    }
}