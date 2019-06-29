using JitTools.Runtime;

namespace JitTools.Unpackers {
	internal static unsafe class UnpackerUtils {
		public static void CallJit(void* methodHandle) {
			RuntimeFunctions.Reset(methodHandle);
			RuntimeFunctions.DoPrestub(methodHandle);
			if (RuntimeFunctions.IsUnboxingStub(methodHandle)) {
				// CLR内部存在UnboxingStub的方法不会被JIT直接编译，所以需要对UnboxingStub进行编译
				void* pUnboxingStub;

				pUnboxingStub = RuntimeFunctions.GetWrappedMethodDesc(methodHandle);
				RuntimeFunctions.Reset(pUnboxingStub);
				RuntimeFunctions.DoPrestub(pUnboxingStub);
				RuntimeFunctions.Reset(pUnboxingStub);
			}
			RuntimeFunctions.Reset(methodHandle);
		}
	}
}
