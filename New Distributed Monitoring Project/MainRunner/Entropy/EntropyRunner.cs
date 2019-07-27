﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataParsing;
using DataParsing.Databse;
using MathNet.Numerics;
using Monitoring.Data;
using Monitoring.GeometricMonitoring;
using Monitoring.GeometricMonitoring.Approximation;
using Monitoring.GeometricMonitoring.MonitoringType;
using Monitoring.GeometricMonitoring.Running;
using Monitoring.GeometricMonitoring.VectorType;
using Monitoring.Nodes;
using Monitoring.Servers;
using MoreLinq;
using Parsing;
using SecondMomentSketch;
using SecondMomentSketch.Hashing;
using Utils.AiderTypes;
using Utils.SparseTypes;
using Utils.TypeUtils;

namespace Entropy
{
    public static class EntropyRunner
    {

        public static void RunStocks(Random rnd, int numOfNodes, int window, DateTime startingDateTime, int minAmount , ApproximationType approximation,
                                               string stocksDirPath, string resultDir)
        {
            var resultPath =
                PathBuilder.Create(resultDir, "Entropy")
                           .AddProperty("Dataset", "Stocks")
                           .AddProperty("Nodes", numOfNodes.ToString())
                           .AddProperty("Window", window.ToString())
                           .AddProperty("StartingTime", startingDateTime.ToShortDateString())
                           .AddProperty("MinAmountAtDay", minAmount.ToString())
                           .AddProperty("Approximation", approximation.AsString())
                           .ToPath("csv");

            using (var resultCsvFile = AutoFlushedTextFile.Create(resultPath, AccumaltedResult.Header(numOfNodes)))
            using (var stocksProbabilityWindow = StocksProbabilityWindow.Init(stocksDirPath, startingDateTime, minAmount, numOfNodes, window))
            {
                var vectorLength = stocksProbabilityWindow.VectorLength;
                var entropy = new EntropyFunction(vectorLength);
                var initProbabilityVectors = stocksProbabilityWindow.CurrentProbabilityVector();
                if (!initProbabilityVectors.All(v => v.Sum().AlmostEqual(1.0, 0.000001)))
                    throw new Exception();
                var multiRunner = MultiRunner.InitAll(initProbabilityVectors, numOfNodes, vectorLength,
                                                      approximation, entropy.MonitoredFunction);
                while (stocksProbabilityWindow.MoveNext())
                {
                    var changeProbabilityVectors = stocksProbabilityWindow.CurrentChangeProbabilityVector();

                    if (!changeProbabilityVectors.All(v => v.Sum().AlmostEqual(0.0, 0.000001)))
                        throw new Exception();
                    multiRunner.Run(changeProbabilityVectors, rnd, true)
                               .Select(r => r.AsCsvString())
                               .ForEach(resultCsvFile.WriteLine);
                }
            }
        }


        public static void RunDatabaseAccesses(Random      rnd, int numOfNodes,
                                               int         window,
                                               ApproximationType approximation, int vectorLength,
                                               UsersDistributing distributing,
                                               string      databaseAccessesPath, string            resultDir)
        {
            var resultPath =
                PathBuilder.Create(resultDir, "Entropy")
                           .AddProperty("Dataset",            "DatabaseAccesses")
                           .AddProperty("Nodes",              numOfNodes.ToString())
                           .AddProperty("Window",             window.ToString())
                           .AddProperty("Approximation",            approximation.AsString())
                           .AddProperty("DistributingMethod", distributing.Name)
                           .ToPath("csv");
            var hashUser      = new Func<int, int>(userId => userId % numOfNodes);
            var entropy = new EntropyFunction(vectorLength);

            using (var resultCsvFile = AutoFlushedTextFile.Create(resultPath, AccumaltedResult.Header(numOfNodes)))
            using (var databaseAccessesStatistics = DatabaseAccessesStatistics.Init(databaseAccessesPath, numOfNodes, window, distributing.DistributeFunc))
            {
                var initProbabilityVectors = databaseAccessesStatistics.InitProbabilityVectors();
                if (!initProbabilityVectors.All(v => v.Sum().AlmostEqual(1.0, 0.000001)))
                    throw new Exception();
                var multiRunner = MultiRunner.InitAll(initProbabilityVectors, numOfNodes, vectorLength,
                                                      approximation, entropy.MonitoredFunction);
                while (databaseAccessesStatistics.TakeStep())
                {
                    var changeProbabilityVectors = databaseAccessesStatistics.GetChangeProbabilityVectors();

                    if (!changeProbabilityVectors.All(v => v.Sum().AlmostEqual(0.0, 0.000001)))
                        throw new Exception();
                    multiRunner.Run(changeProbabilityVectors, rnd, true)
                               .Select(r => r.AsCsvString())
                               .ForEach(resultCsvFile.WriteLine);
                }
            }
        }
   
    }
}
