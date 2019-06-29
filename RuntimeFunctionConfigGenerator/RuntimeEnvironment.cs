using System;
using static JitTools.NativeMethods;

namespace JitTools {
	internal static unsafe class RuntimeEnvironment {
		private static readonly Version Clr45DevPreviewVersion = new Version(4, 0, 30319, 17020);
		private static readonly Version _clrVersion;
		private static readonly void* _clrModuleHandle;
		private static readonly void* _jitModuleHandle;
		private static bool? _isClr45x;

		public static bool Is32Bit => IntPtr.Size == 4;

		public static Version ClrVersion => _clrVersion;

		public static string ClrModuleName => IsClr2x ? "mscorwks.dll" : "clr.dll";

		public static string JitModuleName => IsClr2x ? "mscorjit.dll" : "clrjit.dll";

		public static void* ClrModuleHandle => _clrModuleHandle;

		public static void* JitModuleHandle => _jitModuleHandle;

		public static bool IsClr2x => _clrVersion.Major == 2;

		public static bool IsClr4x => _clrVersion.Major == 4;

		public static bool IsClr45x {
			get {
				if (_isClr45x == null)
					_isClr45x = _clrVersion >= Clr45DevPreviewVersion;
				return _isClr45x.Value;
			}
		}

		static RuntimeEnvironment() {
			_clrVersion = Environment.Version;
			_clrModuleHandle = GetModuleHandle(ClrModuleName);
			_jitModuleHandle = GetModuleHandle(JitModuleName);
		}
	}
}
