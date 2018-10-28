﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using Monitoring.GeometricMonitoring;
using Monitoring.GeometricMonitoring.VectorType;
using Utils.TypeUtils;

namespace ClassLibrary1
{
    public static partial class SpectralGapFunction
    {
        private static Random Rnd     { get; } = new Random(324234);
        private static double Epsilon { get; } = 0.00001;

        public static double Compute(Vector<double> vector)
        {
            var matrix = vector.AsMatrix();
            var (eigenvector1, eigenvalue1) = matrix.PowerIterationMethod(Epsilon, Rnd);
            var (eigenvector2, eigenvalue2) = matrix.PowerIterationMethod2(Epsilon, eigenvector1, Rnd);
            return eigenvalue1 - eigenvalue2;
        }

        public static MonitoredFunction MonitoredFunction = new MonitoredFunction(Compute, UpperBound, LowerBound, GlobalVectorType.Average, 2);

    }
}
