﻿using Microsoft.Research.Oslo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Oslo.Tests {
    [TestClass]
    public class GaussianEliminationTest {
        [TestMethod]
        public void SolverCoreTest3d() {
            var a = new double[][] {
                new double[] { 2,1,-1 },
                new double[] { -3,-1,2 },
                new double[] { -2,1,2 }
            };
            var b = new Vector(8, -11, -3);
            var x = Gauss.SolveCore(a, b);
            var answer = new Vector(2, 3, -1);
            Assert.IsTrue(Vector.GetLInfinityNorm(x, answer) < 1e-10);

        }

        [TestMethod]
        public void SolverCoreTest2d() {
            var a = new double[][] {
                new double[] { 2,1 },
                new double[] { -1,1 }
            };
            var b = new Vector(1, -2);
            var x = Gauss.SolveCore(a, b);
            var answer = new Vector(1, -1);
            Assert.IsTrue(Vector.GetLInfinityNorm(x, answer) < 1e-10);
        }

        [TestMethod]
        public void SolverCoreTestMatrixNd() {
            const int N = 50;
            Matrix a = new Matrix(N, N);
            // Make matrix diagonal
            for (int i = 0; i < N; i++)
                a[i, i] = 1;
            // Apply random rotations around each pair of axes. This will keep det(A) ~ 1
            Random rand = new Random();
            for (int i = 0; i < N; i++)
                for (int j = i + 1; j < N; j++) {
                    double angle = rand.NextDouble() * 2 * Math.PI;
                    Matrix r = new Matrix(N, N);
                    for (int k = 0; k < N; k++)
                        r[k, k] = 1;
                    r[i, i] = r[j, j] = Math.Cos(angle);
                    r[i, j] = Math.Sin(angle);
                    r[j, i] = -Math.Sin(angle);
                    a = a * r;
                }

            // Generate random vector
            Vector b = Vector.Zeros(N);
            for (int i = 0; i < N; i++)
                b[i] = rand.NextDouble();

            // Solve system
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Vector x = Gauss.Solve(a, b);
            sw.Stop();
            Trace.WriteLine("Gaussian elimination took: " + sw.ElapsedTicks);

            // Solve system
            sw.Start();
            Vector x2 = a.SolveGE(b);
            sw.Stop();
            Trace.WriteLine("Jama solve elimination took: " + sw.ElapsedTicks);

            Trace.WriteLine("Difference is " + Vector.GetLInfinityNorm(x, x2));

            // Put solution into system
            Vector b2 = a * x;

            // Verify result is the same
            Assert.IsTrue(Vector.GetLInfinityNorm(b, b2) < 1e-6);
        }

        [TestMethod]
        public void SolverCoreTestSparseMatrixNd() {
            const int N = 15;
            SparseMatrix a = new SparseMatrix(N, N);
            // Make matrix diagonal
            for (int i = 0; i < N; i++)
                a[i, i] = 1;
            // Apply random rotations around each pair of axes. This will keep det(A) ~ 1
            Random rand = new Random();
            for (int i = 0; i < N; i++)
                for (int j = i + 1; j < N; j++) {
                    double angle = rand.NextDouble() * 2 * Math.PI;
                    SparseMatrix r = new SparseMatrix(N, N);
                    for (int k = 0; k < N; k++)
                        r[k, k] = 1;
                    r[i, i] = r[j, j] = Math.Cos(angle);
                    r[i, j] = Math.Sin(angle);
                    r[j, i] = -Math.Sin(angle);
                    a = a * r;
                }

            var ainit = a.Copy();
            // Generate random vector
            Vector b = Vector.Zeros(N);
            for (int i = 0; i < N; i++)
                b[i] = rand.NextDouble();

            var binit = b.Clone();
            // Solve system
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Vector x = new Vector(Gauss.Solve(a, b));
            sw.Stop();
            Trace.WriteLine("Gaussian elimination took: " + sw.ElapsedTicks);
            // Put solution into system
            Vector b2 = ainit * x;

            // Verify result is the same
            Assert.IsTrue(Vector.GetLInfinityNorm(binit, b2) < 1e-6);
        }
    }
}
