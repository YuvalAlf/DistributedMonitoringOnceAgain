﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Utils.MathUtils;
using Utils.SparseTypes;
using Utils.TypeUtils;

namespace Entropy
{
    public sealed partial class EntropyFunction
    {
        public static double Approximation => 0.0000001;

        // Entropy > Threshold
        // Decrease entropy to reach threshold
        public Vector ClosestL1PointFromAbove(double desiredEntropy, Vector point)
        {
            Func<Vector, double> entropyFunction = LowerBoundEntropy;
            Predicate<double> l1DistanceOk = l1 => entropyFunction(DecreaseEntropy(l1, point.Clone())) >= desiredEntropy;
            var minL1Distance = 0.0;
            var maxL1Distance = point.L1Norm() + 1.0 - 2 * point.IndexedValues.Values.MaximumAbsolute();
            var distanceL1 = BinarySearch.FindWhere(minL1Distance, maxL1Distance, l1DistanceOk, Approximation);
            return DecreaseEntropy(distanceL1, point.Clone());
        }
        private Vector DecreaseEntropy(double l1Distance, Vector vec)
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
        public Vector ClosestL1PointFromBelow(double desiredEntropy, Vector point)
        {
            var maxEntropyVector = Enumerable.Repeat(1.0 / Dimension, Dimension).ToVector();
            if (desiredEntropy >= MaxEntropy)
                return maxEntropyVector;

            Func<Vector, double> entropyFunction = LowerBoundEntropy;
            Predicate<Vector> pointOk = vec => entropyFunction(vec) <= desiredEntropy;
            var minL1Distance = 0.0;
            var maxL1Distance = point.IndexedValues.Values.Select(v => v - 1.0 / Dimension).Sum(v => Math.Abs(v));
            Action<Vector, double> move = (vec, l1Distance) =>
                                          {
                                              var vecArray = vec.ToArray(Dimension);
                                              EntropyMathematics.Entropy.increaseEntropy(l1Distance, vecArray);
                                              for (int i = 0; i < vecArray.Length; i++)
                                                  vec[i] = vecArray[i];
                                          };
            Func<Vector, Vector> deepCopy = vec => vec.Clone();
            Action<Vector, Vector> copyInPlace = (from, to) => from.CopyTo(to);
          //  var distanceL1 = BinarySearch.FindWhere(minL1Distance, maxL1Distance, l1DistanceOk, Approximation);
            var result = point.Clone();
            BinarySearch.GoUpIncreasing(minL1Distance, maxL1Distance, pointOk, Approximation, move, result, result.Clone(), deepCopy, copyInPlace);
            return result;
            //return EntropyMathematics.Entropy.increaseEntropy(distanceL1, point.Clone());
        }
    }
}