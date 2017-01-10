using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobotSim;
using Sharp3D.Math.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotSim.Tests {
    [TestClass()]
    public class RbSurfFloorTests {

        [TestMethod()]
        public void GetNForceTest1() {
            var surf = new RbSurfFloor(77,44,new Vector3D(10,20,30));

            var localPos = new Vector3D(-10,19,33);
            var localVel = new Vector3D(0,0,0);
            var answ = surf.GetNForce(localPos,localVel);
            var correctansw = new Vector3D(0,77,0);
            Assert.IsTrue(Vector3D.ApproxEqual(correctansw,answ));
        }
        [TestMethod()]
        public void GetNForceTest2() {
            var surf = new RbSurfFloor(77,44,new Vector3D(10,20,30));

            var localPos = new Vector3D(-10,21,33);
            var localVel = new Vector3D(0,0,0);
            var answ = surf.GetNForce(localPos,localVel);
            var correctansw = new Vector3D(0,0,0);
            Assert.IsTrue(Vector3D.ApproxEqual(correctansw,answ));
        }
        [TestMethod()]
        public void GetNForceTest3() {
            var surf = new RbSurfFloor(77,44,new Vector3D(10,20,30));

            var localPos = new Vector3D(-10,19,33);
            var localVel = new Vector3D(0,1000,0);
            var answ = surf.GetNForce(localPos,localVel);
            var correctansw = new Vector3D(0,77,0);
            Assert.IsTrue(Vector3D.ApproxEqual(correctansw,answ));
        }
        [TestMethod()]
        public void GetNForceTest4() {
            var surf = new RbSurfFloor(77,44,new Vector3D(10,20,30));

            var localPos = new Vector3D(-10,19,33);
            var localVel = new Vector3D(0,-1000,0);
            var answ = surf.GetNForce(localPos,localVel);
            var correctansw = new Vector3D(0,77 + 1000 * 44,0);
            Assert.IsTrue(Vector3D.ApproxEqual(correctansw,answ));
        }
    }
}