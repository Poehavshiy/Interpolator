using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sharp3D.Math.Core;
using SPH_2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPH_2D.Tests {
    [TestClass()]
    public class SegmentTests {
        [TestMethod()]
        public void GetNormalToMeTest1() {
            var segm = new BorderSegment(1,1,2,2);
            var p = new Particle2DDummyBase(1) {
                X = 2,
                Y = 1
            };

            var res = segm.GetNormalToMe(p);

            Assert.AreEqual(-0.5,res.X,0.000001);
            Assert.AreEqual(0.5,res.Y,0.000001);
        }

        [TestMethod()]
        public void GetNormalToMeTest2() {
            var segm = new BorderSegment(1,1,2,1);
            var p = new Particle2DDummyBase(1) {
                X = 3,
                Y = 4
            };

            var res = segm.GetNormalToMe(p);

            Assert.AreEqual(0d,res.X,0.000001);
            Assert.AreEqual(-3d,res.Y,0.000001);
        }

        [TestMethod()]
        public void GetNormalToMeTest3() {
            var segm = new BorderSegment(1,1,1,2);
            var p = new Particle2DDummyBase(1) {
                X = 0,
                Y = 0.5
            };

            var res = segm.GetNormalToMe(p);

            Assert.AreEqual(1d,res.X,0.000001);
            Assert.AreEqual(0d,res.Y,0.000001);
        }

        [TestMethod()]
        public void GetNormalToMeTest4() {
            var segm = new BorderSegment(1,1,2,2);
            var p = new Particle2DDummyBase(1) {
                X = 1.5,
                Y = 1.5
            };

            var res = segm.GetNormalToMe(p);

            Assert.AreEqual(0d,res.X,0.000001);
            Assert.AreEqual(0d,res.Y,0.000001);
        }

        [TestMethod()]
        public void GetNormalToMeTest5() {
            var segm = new BorderSegment(1,1,2,2);
            var p = new Particle2DDummyBase(1) {
                X = 0,
                Y = 0
            };

            var res = segm.GetNormalToMe(p);

            Assert.AreEqual(0d,res.X,0.000001);
            Assert.AreEqual(0d,res.Y,0.000001);
        }

        [TestMethod()]
        public void CloseToMeTest1() {
            var segm = new BorderSegment(1,1,2,2);
            var p = new Particle2DDummyBase(1) {
                X = 1,
                Y = 0.5
            };
            Assert.IsTrue(segm.CloseToMe(p,0.5));
        }

        [TestMethod()]
        public void CloseToMeTest2() {
            var segm = new BorderSegment(1,1,2,2);
            var p = new Particle2DDummyBase(1) {
                X = 1,
                Y = 0.1
            };
            Assert.IsFalse(segm.CloseToMe(p,0.5));
        }

        [TestMethod()]
        public void CloseToMeTest3() {
            var segm = new BorderSegment(1,1,2,1);
            var p = new Particle2DDummyBase(1) {
                X = 2.499,
                Y = 0.5
            };
            Assert.IsTrue(segm.CloseToMe(p,0.5));
        }

        [TestMethod()]
        public void CloseToMeTest4() {
            var segm = new BorderSegment(1,1,2,1);
            var p = new Particle2DDummyBase(1) {
                X = 2.5001,
                Y = 0.5
            };
            Assert.IsFalse(segm.CloseToMe(p,0.5));
        }

        [TestMethod()]
        public void CloseToMeTest5() {
            var segm = new BorderSegment(1,1,2,1);
            var p = new Particle2DDummyBase(1) {
                X = 0.4999,
                Y = 0.5
            };
            Assert.IsFalse(segm.CloseToMe(p,0.5));
        }

        [TestMethod()]
        public void ReflectPosTest1() {
            var segm = new BorderSegment(1,1,2,2);
            var pos = new Vector2D(10,0);

            var reflectpos = segm.ReflectPos(pos);
            var rightReflectPOs = new Vector2D(0,10);
            Assert.IsTrue(Vector2D.ApproxEqual(rightReflectPOs,reflectpos,0.0000001));
        }

        [TestMethod()]
        public void ReflectVelTest() {
            var segm = new BorderSegment(1,1,2,2);
            var vel = new Vector2D(10,5);

            var reflectVel = segm.ReflectVel(vel);
            var rightReflectVel = new Vector2D(5,10);
            Assert.IsTrue(Vector2D.ApproxEqual(rightReflectVel,reflectVel,0.0000001));
        }
    }
}