﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataParsing;
using MathNet.Numerics.LinearAlgebra;
using Monitoring.Data;
using Monitoring.GeometricMonitoring.Epsilon;
using Monitoring.GeometricMonitoring.MonitoringType;
using Monitoring.GeometricMonitoring.Running;
using Monitoring.GeometricMonitoring.VectorType;
using Monitoring.Nodes;
using Monitoring.Servers;
using MoreLinq;
using Utils.MathUtils.Sketches;
using Utils.TypeUtils;

namespace InnerProduct
{
    public static class InnerProductRunner
    {
        private static Vector<double>[] PadWithZeros(this Vector<double>[] vectors, Func<int, bool> isLeft)
        {
            var zeroArray = ArrayUtils.Init(vectors[0].Count, _ => 0.0).ToVector();
            Vector<double> PadLeft(Vector<double> v) => zeroArray.VConcat(v);
            Vector<double> PadRight(Vector<double> v) => v.VConcat(zeroArray);
            return vectors.Select((v, i) => isLeft(i) ? PadLeft(v) : PadRight(v)).ToArray();
        }

        public static void RunBagOfWords(Random rnd, string wordsPath, string resultPath, Func<int, bool> isLeft, string[] textFilesPathes)
        {
            var globalVectorType   = GlobalVectorType.Sum;
            var epsilon            = new MultiplicativeEpsilon(0.025);
            var numOfNodes         = textFilesPathes.Length;
            var windowSize         = 100000;
            var amountOfIterations = 500;
            var vectorLength       = 500;
            var stepSize           = 1000;
            var optionalWords = File.ReadLines(wordsPath).Take(vectorLength).ToArray();
            var optionalStrings = new SortedSet<string>(optionalWords, StringComparer.OrdinalIgnoreCase);

            using (var resultCsvFile = File.CreateText(resultPath))
            {
                resultCsvFile.AutoFlush = true;
                resultCsvFile.WriteLine(AccumaltedResult.Header(numOfNodes));

                using (var stringDataParser = DataParser<string>.Init(StreamReaderUtils.EnumarateWords, windowSize, optionalStrings, textFilesPathes))
                {
                    var initVectors = stringDataParser.Histograms.Map(h => h.CountVector()).PadWithZeros(isLeft);
                    var multiRunner = MultiRunner.InitAll(initVectors, numOfNodes, vectorLength * 2, globalVectorType,
                                                          epsilon, InnerProductFunction.MonitoredFunction, 2);
                    var changes = stringDataParser.AllCountVectors(stepSize).Select(ch => ch.PadWithZeros(isLeft)).Take(amountOfIterations);
                    multiRunner.RunAll(changes, rnd, false)
                               .Select(r => r.AsCsvString())
                               .ForEach((Action<string>)resultCsvFile.WriteLine);
                }
            }

            Process.Start(resultPath);
        }
    }
}
