﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Utils.MathUtils;
using Utils.TypeUtils;

namespace Entropy
{
    public static class EntropyMath
    {
        public static double Approximation => 0.0000001;

        // Entropy > Threshold
        // Decrease entropy to reach threshold
        public static Vector<double> ClosestL1PointFromAbove(double desiredEntropy, Vector<double> point)
        {
            Func<Vector<double>, double> entropyFunction = EntropyFunction.ComputeEntropy;
            Predicate<double> l1DistanceOk = l1 => entropyFunction(DecreaseEntropy(l1, point.Clone())) >= desiredEntropy;
            var minL1Distance = 0.0;
            var maxL1Distance = point.L1Norm() + 1.0 - 2 * point.AbsoluteMaximum();
            var distanceL1 = BinarySearch.FindWhere(minL1Distance, maxL1Distance, l1DistanceOk, Approximation);
            return DecreaseEntropy(distanceL1, point.Clone());
        }
        private static Vector<double> DecreaseEntropy(double l1Distance, Vector<double> vec)
        {
            var minIndex = vec.MinimumIndex();
            var maxIndex = vec.MaximumIndex();
            if (minIndex == maxIndex)
            {
                minIndex = 0;
                maxIndex = 1;
            }
            vec[minIndex] -= l1Distance / 2.0;
            vec[maxIndex] += l1Distance / 2.0;
            return vec;
        }

        // Entropy < Threshold
        // Increase entropy to reach threshold
        public static Vector<double> ClosestL1PointFromBelow(double desiredEntropy, Vector<double> point)
        {
            var length = point.Count;
            var maxEntropyVector = Enumerable.Repeat(1.0 / length, length).ToVector();
            var maxEntropy = EntropyFunction.ComputeEntropy(maxEntropyVector);
            if (desiredEntropy >= maxEntropy)
                return maxEntropyVector;

            Func<Vector<double>, double> entropyFunction = EntropyFunction.ComputeEntropy;
            Predicate<Vector<double>> pointOk = vec => entropyFunction(vec) <= desiredEntropy;
            var minL1Distance = 0.0;
            var maxL1Distance = (point - maxEntropyVector).L1Norm();
            Action<Vector<double>, double> move = (vec, l1Distance) => EntropyMathematics.Entropy.increaseEntropy(l1Distance, vec);
            Func<Vector<double>, Vector<double>> deepCopy = vec => vec.Clone();
            Action<Vector<double>, Vector<double>> copyInPlace = (from, to) => from.CopyTo(to);
          //  var distanceL1 = BinarySearch.FindWhere(minL1Distance, maxL1Distance, l1DistanceOk, Approximation);
            var result = point.Clone();
            BinarySearch.GoUpIncreasing(minL1Distance, maxL1Distance, pointOk, Approximation, move, result, result.Clone(), deepCopy, copyInPlace);
            return result;
            //return EntropyMathematics.Entropy.increaseEntropy(distanceL1, point.Clone());
        }
    }
}