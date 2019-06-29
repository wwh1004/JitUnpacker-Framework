using System;

namespace JitTools.Runtime {
	public enum JitHookType {
		Inline,
		VTable,
		Int3,
		Hardware
	}

	internal static class JitHookFactory {
		public static IJitHook Create(JitHookType type) {
			switch (type) {
			case JitHookType.Inline:
				return new JitHookInlineImpl();
			case JitHookType.VTable:
				return new JitHookVTableImpl();
			case JitHookType.Int3:
				throw new NotImplementedException();
			case JitHookType.Hardware:
				throw new NotImplementedException();
			default:
				throw new ArgumentOutOfRangeException(nameof(type));
			}
		}
	}
}
