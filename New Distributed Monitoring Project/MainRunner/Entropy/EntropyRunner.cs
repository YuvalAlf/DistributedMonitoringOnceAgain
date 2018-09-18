﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataParsing;
using Monitoring.Data;
using Monitoring.GeometricMonitoring.Epsilon;
using Monitoring.GeometricMonitoring.MonitoringType;
using Monitoring.GeometricMonitoring.Running;
using Monitoring.GeometricMonitoring.VectorType;
using Monitoring.Nodes;
using Monitoring.Servers;
using MoreLinq;
using Utils.TypeUtils;

namespace Entropy
{
    public static class EntropyRunner
    {
        public static void RunBagOfWords(Random rnd, int vectorLength, string wordsPath, string resultDir, string[] textFilesPathes)
        {
            var globalVectorType   = GlobalVectorType.Average;
            var epsilon            = new MultiplicativeEpsilon(0.01);
            var numOfNodes         = textFilesPathes.Length;
            var windowSize         = 10000;
            var amountOfIterations = 1000;
            var stepSize           = 100;
            var optionalWords      = File.ReadLines(wordsPath).Take(vectorLength).ToArray();
            var optionalStrings    = new SortedSet<string>(optionalWords, StringComparer.OrdinalIgnoreCase);
            var fileName = $"Entropy_VecSize_{vectorLength}_WindowSize_{windowSize}_Iters_{amountOfIterations}_StepSize_{stepSize}_Nodes_{textFilesPathes.Length}_Epsilon_{epsilon.EpsilonValue}.csv";
            var resultPath = Path.Combine(resultDir, fileName);

            using (var resultCsvFile = File.CreateText(resultPath))
            {
                resultCsvFile.AutoFlush = true;
                resultCsvFile.WriteLine(AccumaltedResult.Header(numOfNodes));

                using (var stringDataParser = DataParser<string>.Init(StreamReaderUtils.EnumarateWords, windowSize, optionalStrings, textFilesPathes))
                {
                    var initVectors = stringDataParser.Histograms.Map(h => h.CountVector() / windowSize);
                    var multiRunner = MultiRunner.InitAll(initVectors, numOfNodes, vectorLength, globalVectorType,
                                                          epsilon, EntropyFunction.MonitoredFunction);
                    var changes = stringDataParser.AllCountVectors(stepSize).Select(c => c.Map(v => v / windowSize)).Take(amountOfIterations);
                    multiRunner.RunAll(changes, rnd, true)
                               .Select(r => r.AsCsvString())
                               .ForEach((Action<string>)resultCsvFile.WriteLine);
                }
            }

            Process.Start(resultPath);
        }
    }
}
