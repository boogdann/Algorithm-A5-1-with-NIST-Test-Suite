﻿namespace Algorithm5A_1.Extensions {
	public static class UIntExtensions {
		public static uint GetBit(this uint value, int num) => (value >> num) & 1;
	}
}