using System.Runtime.InteropServices;
using static JitTools.NativeMethods;
using static JitTools.Runtime.RuntimeConstants;

namespace JitTools.Runtime {
	internal static unsafe class RuntimeFunctions {
		private static readonly ResetDelegate _reset;
		private static readonly DoPrestubDelegate _doPrestub;
		private static readonly GetWrappedMethodDescDelegate _getWrappedMethodDesc;

		static RuntimeFunctions() {
			_reset = MarshalEx.CreateDelegate<ResetDelegate>((byte*)RuntimeEnvironment.ClrModuleHandle + METHODDESC_RESET_RVA);
			_doPrestub = MarshalEx.CreateDelegate<DoPrestubDelegate>((byte*)RuntimeEnvironment.ClrModuleHandle + METHODDESC_DOPRESTUB_RVA);
			_getWrappedMethodDesc = MarshalEx.CreateDelegate<GetWrappedMethodDescDelegate>((byte*)RuntimeEnvironment.ClrModuleHandle + METHODDESC_GETWRAPPEDMETHODDESC_RVA);
		}

		public static void* GetJit() {
			return RuntimeEnvironment.IsClr2x ? GetJit2() : GetJit4();
		}

		public static bool IsUnboxingStub(void* pMethodDesc) {
			return (*((byte*)pMethodDesc + 3) & 0x4) != 0;
			// return (m_bFlags2 & enum_flag2_IsUnboxingStub) != 0;
		}

		public static void Reset(void* pMethodDesc) {
			_reset(pMethodDesc);
		}

		public static void* DoPrestub(void* pMethodDesc) {
			try {
				return _doPrestub(pMethodDesc, null);
			}
			catch (SEHException) {
				return null;
			}
		}

		public static void* GetWrappedMethodDesc(void* pMethodDesc) {
			return _getWrappedMethodDesc(pMethodDesc);
		}
	}
}
