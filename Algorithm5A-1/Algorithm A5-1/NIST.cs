﻿// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Algorithm5A_1.Extensions;
using MathNet.Numerics;

namespace Algorithm5A_1.Algorithm_A5_1 {
	public class NIST {
		private const double SignificanceLevel = 0.01;
		
		private readonly byte[] bytes;
		private readonly int n;
		private readonly int lastByteIndex;
		private readonly int lastBitIndexInLastByte;
		
		public NIST(byte[] bytes, int n) {
			Debug.Assert(n <= bytes.Length * Bits.InByte);
			
			this.bytes = bytes;
			this.n = n;

			lastBitIndexInLastByte = n % Bits.InByte - 1;
			if (lastBitIndexInLastByte == -1) 
				lastBitIndexInLastByte = Bits.LastIndexInByte;
			lastByteIndex = bytes.Length - 1;
		}

		public static bool FrequencyTest(NIST nist) => nist.FrequencyP_value() >= SignificanceLevel;

		public double FrequencyP_value() => SpecialFunctions.Erfc(Sobs() / Math.Sqrt(2));
		
		private double Sobs() => Math.Abs(Sn()) / Math.Sqrt(n);
		
		private int Sn() {
			int result = 0;
			for (int i = 0; i < n; i++) 
				result += IsBitOne(i) ? 1 : -1;
			return result;
		}

		private bool IsBitOne(int bit) {
			var (byteIndex, bitIndex) = GetIndexesForBit(bit);
			return bytes[byteIndex].GetBit(bitIndex) == 1;
		}
		
		private (int byteIndex, int bitIndex) GetIndexesForBit(int bit) {
			// Calculate the byte index containing the bit
			int byteIndex = bit / Bits.InByte;

			// Calculate the bit index within the byte, accounting for Little Endian
			int lastBitIndex = byteIndex == lastByteIndex ? lastBitIndexInLastByte : Bits.LastIndexInByte;
			int bitIndex = lastBitIndex - bit % Bits.InByte;

			return (byteIndex, bitIndex);
		}

		public static bool BlockFrequencyTest(NIST nist, int M) => nist.BlockFrequencyP_value(M) >= SignificanceLevel;
		
		public double BlockFrequencyP_value(int M) {
			int N = n / M;
			
			var pis = Pis(N, M);
			double chiSquared = ChiSquaredBF(pis, M);
			return SpecialFunctionsExtensions.Igamc(N / 2.0, chiSquared / 2.0);
		}
		
		private IEnumerable<double> Pis(int N, int M) {
			double[] pis = new double[N];
			for (int i = 0; i < N; i++)
				pis[i] = Pi(i, M);
			return pis;
		}

		private double Pi() => Pi(0, n);
		
		private double Pi(int quantityOfPrevBlocks, int M) {
			int onesCount = 0;
			for (int j = 0; j < M; j++) 
				if (IsBitOne(quantityOfPrevBlocks * M + j))
					onesCount++;
			return (double)onesCount / M;
		}
		
		private static double ChiSquaredBF(IEnumerable<double> pis, int M) {
			return 4 * M * pis.Sum(pi => (pi - 0.5).Sqr());
		}
		
		public static bool RunsTest(NIST nist) => nist.RunsP_value() >= SignificanceLevel;

		public double RunsP_value() {
			if (!FrequencyTest(this))
				return 0.0;
				
			double pi = Pi();
			double dividend = Vobs() - 2 * n * pi * (1 - pi);
			double divisor = 2 * Math.Sqrt(2 * n) * pi * (1 - pi);
			return SpecialFunctions.Erfc(Math.Abs(dividend) / divisor);
		}
		
		private int Vobs() {
			int Vobs = 1;
			for (int i = 1; i < n; i++) 
				if (IsDifferentStates(i - 1, i))
					Vobs++;
			return Vobs;
		}

		private bool IsDifferentStates(int bit1, int bit2) => IsBitOne(bit1) != IsBitOne(bit2);
		
		public static bool LongestRunOfOnesTest(NIST nist, int M) => nist.LongestRunOfOnesP_value() >= SignificanceLevel;

		public double LongestRunOfOnesP_value() {
			if (n < 128)
				throw new ArgumentException("Not enough data to run test.");
			
			var lrtp = LongestRunTestParameters.GetParameters(n);
			int N = n / lrtp.M;
			double chiSquared = ChiSquaredLR(lrtp, N, Vs(lrtp, N));
			
			return SpecialFunctionsExtensions.Igamc(lrtp.K / 2.0, chiSquared / 2.0);
		}
		
		private struct LongestRunTestParameters {
			public int initialRun;
			public int K;
			public int M;
			public double[] Pis;
			public static LongestRunTestParameters GetParameters(int length) {
				LongestRunTestParameters parameters = new LongestRunTestParameters();
				if (length < 6272) {
					parameters.initialRun = 1;
					parameters.K = 3;
					parameters.M = 8;
					parameters.Pis = new [] { 0.21484375, 0.3671875, 0.23046875, 0.1875 };
				}
				else if (length < 75000) {
					parameters.initialRun = 4;
					parameters.K = 5;
					parameters.M = 128;
					parameters.Pis = new [] { 0.1174035788, 0.242955959, 0.249363483, 0.17517706, 0.102701071, 0.112398847 };
				}
				else {
					parameters.initialRun = 10;
					parameters.K = 6;
					parameters.M = 10000;
					parameters.Pis = new [] { 0.0882, 0.2092, 0.2483, 0.1933, 0.1208, 0.0675, 0.0727 };
				}
				return parameters;
			}
		}

		private int[] Vs(in LongestRunTestParameters lrtp, int N) {
			int[] vs = new int[lrtp.K + 1];
			for (int i = 0; i < N; i++) {
				int longestRun = LongestRun(i, lrtp.M);
				if (longestRun <= lrtp.initialRun)
					vs[0]++;
				else if (longestRun >= lrtp.initialRun + lrtp.K)
					vs[lrtp.K]++;
				else
					vs[longestRun - lrtp.initialRun]++;
			}
			return vs;
		}
		
		private int LongestRun(int quantityOfPrevBlocks, int M) {
			int currentRun = 0;
			int longestRun = 0;
			for (int j = 0; j < M; j++) {
				if (IsBitOne(quantityOfPrevBlocks * M + j)) {
					currentRun++;
					longestRun = Math.Max(longestRun, currentRun);
				}
				else
					currentRun = 0;
			}
			return longestRun;
		}
		
		private static double ChiSquaredLR(in LongestRunTestParameters lrtp, int N, int[] Vs)
		{
			double chiSquared = 0;
			for (int i = 0; i < Vs.Length; i++) 
				chiSquared += (Vs[i] - N * lrtp.Pis[i]).Sqr() / (N * lrtp.Pis[i]);
			return chiSquared;
		}
	}
}