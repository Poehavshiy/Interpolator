using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassDrummer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassDrummer.Tests {
    [TestClass()]
    public class SolverTests {
        [TestMethod()]
        public void GetCylIntegrXTest1() {
            var d = 3d;
            var h = 2d;

            var v = Solver.GetCylIntegrX(x => d / 2,0,h);
            var v_right = Math.PI * d * d * 0.25 * h;
            Assert.AreEqual(v_right,v,0.0000001);
        }

    }
}