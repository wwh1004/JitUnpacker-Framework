using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static JitTools.NativeMethods;

namespace JitTools.Runtime {
	internal sealed unsafe class JitHookVTableImpl : IJitHook {
		private static readonly CallingConvention CompileMethodCallingConvention = RuntimeEnvironment.IsCompileMethodThisCall ? CallingConvention.ThisCall : CallingConvention.StdCall;
		private readonly void** _ppCompileMethod;
		private readonly void* _pCompileMethodOriginal;
		private readonly CompileMethodDelegate _compileMethodOriginal;
		private readonly CompileMethodDelegate _compileMethodStub;
		// 我们不会使用到这个字段，为这个字段赋值的目的是防止GC回收委托
		private readonly void* _pCompileMethodStub;
		private bool _isHooked;
		private void* _targetModuleHandle;
		private void* _targetMethodHandle;
		private JitCompilationCallback _callback;
		private uint _depth;

		public void* TargetModuleHandle {
			get => _targetModuleHandle;
			set => _targetModuleHandle = value;
		}

		public void* TargetMethodHandle {
			get => _targetMethodHandle;
			set => _targetMethodHandle = value;
		}

		public JitCompilationCallback Callback { get => _callback; set => _callback = value; }

		static JitHookVTableImpl() {
			// 防止JitHook进入死循环，先编译一些方法
			CORINFO_METHOD_INFO_40 nativeMethodInfo;
			CorMethodInfo methodInfo;

			nativeMethodInfo = new CORINFO_METHOD_INFO_40();
			methodInfo = new CorMethodInfo(&nativeMethodInfo);
			_ = methodInfo.MethodHandle;
			_ = methodInfo.ModuleHandle;
		}

		public JitHookVTableImpl() {
			_ppCompileMethod = *(void***)RuntimeFunctions.GetJit();
			// 虚表第一项是CILJit::compileMethod
			_pCompileMethodOriginal = *_ppCompileMethod;
			_compileMethodOriginal = MarshalEx.CreateDelegate<CompileMethodDelegate>(_pCompileMethodOriginal, CompileMethodCallingConvention);
			// 获取原始compileMethod
			_compileMethodStub = CompileMethodStub;
			_pCompileMethodStub = MarshalEx.GetDelegateAddress(_compileMethodStub, CompileMethodCallingConvention);
			// 调用回调方法的stub
			_compileMethodStub(null, null, null, 0, null, null);
		}

		public bool Hook() {
			if (_isHooked)
				throw new InvalidOperationException(nameof(_isHooked) + " = true");

			MarshalEx.Write(_ppCompileMethod, GetBytes(_pCompileMethodStub));
			_isHooked = true;
			return true;
		}

		public bool Unhook() {
			if (!_isHooked)
				throw new InvalidOperationException(nameof(_isHooked) + " = false");

			MarshalEx.Write(_ppCompileMethod, GetBytes(_pCompileMethodOriginal));
			_isHooked = false;
			return true;
		}

		private static byte[] GetBytes(void* value) {
			return RuntimeEnvironment.Is32Bit ? BitConverter.GetBytes((uint)value) : BitConverter.GetBytes((ulong)value);
		}

		[HandleProcessCorruptedStateExceptions]
		private int CompileMethodStub(void* pThis, void* pICorJitInfo, void* pMethodInfo, uint flags, byte** ppEntryAddress, uint* pNativeSizeOfCode) {
			CorMethodInfo methodInfo;
			JitCompilationInfo compilationInfo;

			if (pThis == null)
				return 0;
			methodInfo = new CorMethodInfo(pMethodInfo);
			if (methodInfo.ModuleHandle != _targetModuleHandle || JitHookUtils.GetRealMethodHandle(methodInfo.MethodHandle) != _targetMethodHandle || _depth > 20)
				return _compileMethodOriginal(pThis, pICorJitInfo, pMethodInfo, flags, ppEntryAddress, pNativeSizeOfCode);
			compilationInfo = new JitCompilationInfo {
				PointerOfICorJitInfo = pICorJitInfo,
				MethodInfo = methodInfo,
				PointerOfNativeCodeAddress = ppEntryAddress,
				PointerOfNativeCodeSize = pNativeSizeOfCode
			};
			try {
				_depth++;
				// 深度，表示CompileMethodStub被嵌套调用了多少次，防止调用次数过多导致堆栈溢出
				if (_callback(compilationInfo))
					// 返回true表示不将方法交给JIT编译器进行继续编译
					return 0;
				pICorJitInfo = compilationInfo.PointerOfICorJitInfo;
				pMethodInfo = compilationInfo.MethodInfo.PointerOfData;
				ppEntryAddress = compilationInfo.PointerOfNativeCodeAddress;
				pNativeSizeOfCode = compilationInfo.PointerOfNativeCodeSize;
				// 使用回调方法修改后的数据进行编译
				return _compileMethodOriginal(pThis, pICorJitInfo, pMethodInfo, flags, ppEntryAddress, pNativeSizeOfCode);
			}
			finally {
				_depth = 0;
			}
		}
	}
}
