using static JitTools.NativeMethods;

namespace JitTools.Runtime {
	internal sealed unsafe class CorMethodInfo {
		private readonly void* _pData;

		public void* PointerOfData => _pData;

		public CORINFO_METHOD_INFO_20* MethodInfo20 => (CORINFO_METHOD_INFO_20*)_pData;

		public CORINFO_METHOD_INFO_40* MethodInfo40 => (CORINFO_METHOD_INFO_40*)_pData;

		public void* MethodHandle => RuntimeEnvironment.IsClr4x ? MethodInfo40->ftn : MethodInfo20->ftn;

		public void* ModuleHandle => RuntimeEnvironment.IsClr4x ? MethodInfo40->scope : MethodInfo20->scope;

		public byte* ILCode => RuntimeEnvironment.IsClr4x ? MethodInfo40->ILCode : MethodInfo20->ILCode;

		public uint ILCodeSize => RuntimeEnvironment.IsClr4x ? MethodInfo40->ILCodeSize : MethodInfo20->ILCodeSize;

		public uint MaxStack => RuntimeEnvironment.IsClr4x ? MethodInfo40->maxStack : MethodInfo20->maxStack;

		public uint ExceptionHandlerCount => RuntimeEnvironment.IsClr4x ? MethodInfo40->EHcount : MethodInfo20->EHcount;

		public CorInfoOptions Options => RuntimeEnvironment.IsClr4x ? MethodInfo40->options : MethodInfo20->options;

		public CorSigInfo Locals => new CorSigInfo(RuntimeEnvironment.IsClr4x ? (void*)&MethodInfo40->locals : &MethodInfo20->locals);

		public CorMethodInfo(void* pData) {
			_pData = pData;
		}
	}

	internal sealed unsafe class CorSigInfo {
		private readonly void* _pData;

		public void* PointerOfData => _pData;

		public CORINFO_SIG_INFO_20* SigInfo20 => (CORINFO_SIG_INFO_20*)_pData;

		public CORINFO_SIG_INFO_40* SigInfo40 => (CORINFO_SIG_INFO_40*)_pData;

		public ushort ArgumentCount => RuntimeEnvironment.IsClr4x ? SigInfo40->numArgs : SigInfo20->numArgs;

		public void* Arguments => RuntimeEnvironment.IsClr4x ? SigInfo40->args : SigInfo20->args;

		public CorSigInfo(void* pData) {
			_pData = pData;
		}
	}
}
