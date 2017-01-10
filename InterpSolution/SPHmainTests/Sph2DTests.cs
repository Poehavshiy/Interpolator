using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPH_2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPH_2D.Tests {
    [TestClass()]
    public class Sph2DTests {
        class ParticleDummy : Particle2DBase {
            public ParticleDummy(double hmax) : base(hmax) {
            }

            public override int StuffCount {
                get {
                    return 1;
                }
            }

            public override void DoStuff(int stuffIndex) {
                //
            }
        }

        [TestMethod()]
        public void Sph2DTest() {
            var part = new List<IsotropicGasParticle>();
            double hmax = 0.499;
            double shagX = 0.25, shagY = 0.25;
            for(int i = 0; i < 30; i++) {
                for(int j = 0; j < 20; j++) {
                    var p = new IsotropicGasParticle(0.1, hmax);
                    p.X = i * shagX;
                    p.Y = j * shagY;
                   // p.Name = $"p{i * j}";
                    part.Add(p);
                }
            }
            var wall = new List<IsotropicGasParticle>();
            for(int i = 0; i < 30; i++) {
                for(int j = 0; j < 5; j++) {
                    var p = new IsotropicGasParticle(0.1, hmax);
                    p.X = i * shagX;
                    p.Y = -shagY - j * shagY;
                   // p.Name = $"w{i * j}";
                    wall.Add(p);
                }
            }


            var sph = new Sph2D(part,wall);
            var v0 = sph.Rebuild();
            sph.FillCells();
            sph.FillNeibs();

            var maxNeibs = sph.AllParticles.Max(p => p.Neibs.Where(n => p.GetDistTo(n)<hmax).Count());
            var minNeibs = sph.AllParticles.Min(p => p.Neibs.Where(n => p.GetDistTo(n) < hmax).Count());
            var gr = from p in sph.AllParticles
                     group p by p.Neibs.Where(n => p.GetDistTo(n) < hmax).Count() into groups
                     select new {
                         groups.Key,
                         N = groups.Count()
                     };
            var dict = gr.ToDictionary(g => g.Key);

            Assert.AreEqual(8+1,maxNeibs);
            Assert.AreEqual(3+1,minNeibs);

        }
    }
}