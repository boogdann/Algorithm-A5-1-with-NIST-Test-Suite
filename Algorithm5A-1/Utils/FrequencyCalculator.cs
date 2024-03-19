﻿using System.Collections.Generic;
using System.Text;

namespace Algorithm5A_1.Utils {
	public static class FrequencyAnalyzer {
		public static Dictionary<char, int> Calculate(byte[] bytes) {
			var freq = new Dictionary<char, int>();
			
			char[] textBuffer = Encoding.ASCII.GetChars(bytes);
			foreach (char b in textBuffer)
				if (freq.ContainsKey(b))
					freq[b]++;
				else
					freq[b] = 1;

			return freq;
		}
	}
}