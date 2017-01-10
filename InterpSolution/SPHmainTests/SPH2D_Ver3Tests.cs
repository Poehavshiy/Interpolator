using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPH_2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPH_2D.Tests {
    [TestClass()]
    public class SPH2D_Ver3Tests {
        [TestMethod()]
        public void TestTrubaTest() {
            //double lt = initcond[0]; //длина трубы
            //double ht = initcond[1]; //высота трубы
            //double x0t = initcond[2];//отностиельная граница по Икс
            //double p1t = initcond[3]; //Давление слева
            //double ro1t = initcond[4]; //Плотность слева
            //double p2t = initcond[5]; //Давление справа
            //double ro2t = initcond[6]; //Плотность справа
            var tst = SPH2D_Ver3.TestTruba(1,0.3,0.5,1,1,0.1,0.125);
            //System.Core.dll!System.Linq.Enumerable.ConcatIterator<SimpleIntegrator.IScnPrm>(System.Collections.Generic.IEnumerable < SimpleIntegrator.IScnPrm > first,System.Collections.Generic.IEnumerable < SimpleIntegrator.IScnPrm > second)    Unknown

        }

        [TestMethod()]
        public void GetMassTest() {
            double delta = 1, h = 2, ro = 10;
            var m = SPH2D_Ver3.GetMass(delta,h,ro);
            var ro_answ = 0d;
            for(int i = -10; i <= 10; i++) {
                for(int j = -10; j <= 10; j++) {
                    ro_answ += m*Particle2DBase.W_func(Math.Sqrt(i * delta * i * delta + j * delta * j * delta),h);
                }
            }
            Assert.AreEqual(ro,ro_answ,0.0000001);
        }
    }
}