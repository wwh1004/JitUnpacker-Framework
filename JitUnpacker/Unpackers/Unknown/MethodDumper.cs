using JitTools.Runtime;
using static JitTools.NativeMethods;

namespace JitTools.Unpackers.Unknown {
	internal sealed unsafe class MethodDumper : MethodDumperBase {
		public MethodDumper(UnpackerContext context) : base(context) {
		}

		protected override bool OnJitCompilation(JitCompilationInfo compilationInfo) {
			void* pICorJitInfo;
			CorMethodInfo methodInfo;
			byte[] byteILs;
			CorSigInfo locals;
			byte[] byteLocalSig;
			CORINFO_EH_CLAUSE[] clauses;

			pICorJitInfo = compilationInfo.PointerOfICorJitInfo;
			methodInfo = compilationInfo.MethodInfo;
			EnsueGetEHInfo(pICorJitInfo);
			byteILs = ReadILs(methodInfo);
			locals = methodInfo.Locals;
			byteLocalSig = locals.ArgumentCount == 0 ? null : BuildLocalSig(GetVariables(locals.Arguments, locals.ArgumentCount), locals.ArgumentCount);
			clauses = GetAllExceptionHandlers(pICorJitInfo, methodInfo);
			RestoreMethod(_targetIndex, byteILs, (ushort)methodInfo.MaxStack, byteLocalSig, clauses, null);
			_state = DumpingState.Finished;
			_dumpCount++;
			return true;
		}
	}
}
