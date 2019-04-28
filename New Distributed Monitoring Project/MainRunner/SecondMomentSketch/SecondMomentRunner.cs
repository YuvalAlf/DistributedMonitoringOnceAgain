﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Random;
using Monitoring.Data;
using Monitoring.GeometricMonitoring.Epsilon;
using Monitoring.GeometricMonitoring.MonitoringType;
using Monitoring.GeometricMonitoring.Running;
using Monitoring.GeometricMonitoring.VectorType;
using MoreLinq.Extensions;
using SecondMomentSketch.Hashing;
using Utils.MathUtils;
using Utils.SparseTypes;
using Utils.TypeUtils;

namespace SecondMomentSketch
{

    public static class SecondMomentRunner
    {
        public static void RunRandomly(Random rnd, int width, int height, int numOfNodes,
                                       string resultDir)
        {
            var vectorLength = width * height;
            var iterations   = 1000;
            var epsilonValue = 300;
            var epsilon      = new AdditiveEpsilon(epsilonValue);
            var fileName =
                $"F2_VecSize_{vectorLength}_Iters_{iterations}_Nodes_{numOfNodes}_Epsilon_{epsilon.EpsilonValue}.csv";
            var resultPath           = Path.Combine(resultDir, fileName);
            var secondMomentFunction = new SecondMoment(width, height);

            using (var resultCsvFile = AutoFlushedTextFile.Create(resultPath, AccumaltedResult.Header(numOfNodes)))
            {
                var initVectors =
                    ArrayUtils.Init(numOfNodes,
                                    _ => ArrayUtils.Init(vectorLength, __ => (double) rnd.Next(-1, 1)).ToVector());

                var multiRunner = MultiRunner.InitAll(initVectors, numOfNodes, vectorLength,
                                                      epsilon, secondMomentFunction.MonitoredFunction);

                for (int i = 0; i < iterations; i++)
                {
                    var changes = ArrayUtils.Init(numOfNodes,
                                                  _ => ArrayUtils
                                                      .Init(vectorLength, __ => (double) rnd.Next(-1, 1)).ToVector());
                    multiRunner.Run(changes, rnd, false)
                               .Select(r => r.AsCsvString())
                               .ForEach((Action<string>) resultCsvFile.WriteLine);
                }
            }
        }

        public static void RunDatabaseAccesses(Random rnd,          int    numOfNodes, int window,
                                               double epsilonValue, int    width,
                                               int    height,       string databaseAccessesPath, string resultDir)
        {
            var vectorLength       = width * height;
            var epsilon            = new AdditiveEpsilon(epsilonValue);
            var hashFunction       = FourwiseIndepandantFunction.Init(rnd);
            var hashFunctionsTable = HashFunctionTable.Init(numOfNodes, vectorLength, hashFunction);
            var fileName = $"F2_Width_{width}_Height_{height}_Window_{window}_Nodes_{numOfNodes}_Epsilon_{epsilon.EpsilonValue}.csv";
            var resultPath           = Path.Combine(resultDir, fileName);
            var secondMomentFunction = new SecondMoment(width, height);

            using (var resultCsvFile = AutoFlushedTextFile.Create(resultPath, AccumaltedResult.Header(numOfNodes)))
            using (var databaseAccessesStatistics = DatabaseAccessesStatistics.Init(databaseAccessesPath, numOfNodes, window))
            {
                var initCountVectors = databaseAccessesStatistics.InitCountVectors();
                var initVectors = hashFunction.TransformToAMSSketch(initCountVectors, vectorLength, hashFunctionsTable);
                var multiRunner = MultiRunner.InitAll(initVectors, numOfNodes, vectorLength,
                                                      epsilon, secondMomentFunction.MonitoredFunction);
                while (databaseAccessesStatistics.TakeStep())
                {
                    var changeCountVectors = databaseAccessesStatistics.GetChangeCountVectors();
                    var changeVectors = hashFunction.TransformToAMSSketch(changeCountVectors, vectorLength, hashFunctionsTable);
                    multiRunner.Run(changeVectors, rnd, false)
                               .Select(r => r.AsCsvString())
                               .ForEach(resultCsvFile.WriteLine);
                }
            }
        }
    }
}
