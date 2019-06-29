using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static JitTools.NativeMethods;
using static JitTools.Runtime.RuntimeConstants;

namespace JitTools.Runtime {
	internal sealed unsafe class JitHookInlineImpl : IJitHook {
		private readonly int* _pCallCompCompileOperation;
		private readonly void* _pCompCompileOriginal;
		private readonly CompCompileDelegate _compCompileOriginal;
		private readonly CompCompileDelegate _compCompileStub;
		// 我们不会使用到这个字段，为这个字段赋值的目的是防止GC回收委托
		private readonly void* _pCompCompileStub;
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

		public JitCompilationCallback Callback {
			get => _callback;
			set => _callback = value;
		}

		static JitHookInlineImpl() {
			// 防止JitHook进入死循环，先编译一些方法
			CORINFO_METHOD_INFO_40 nativeMethodInfo;
			CorMethodInfo methodInfo;

			nativeMethodInfo = new CORINFO_METHOD_INFO_40();
			methodInfo = new CorMethodInfo(&nativeMethodInfo);
			_ = methodInfo.MethodHandle;
			_ = methodInfo.ModuleHandle;
		}

		public JitHookInlineImpl() {
			_pCallCompCompileOperation = (int*)((byte*)RuntimeEnvironment.JitModuleHandle + CALL_COMPCOMPILE_RVA + 1);
			// 指向"call Compiler::compCompile"的操作数
			_pCompCompileOriginal = (byte*)_pCallCompCompileOperation + *_pCallCompCompileOperation + 4;
			_compCompileOriginal = MarshalEx.CreateDelegate<CompCompileDelegate>(_pCompCompileOriginal, CallingConvention.FastCall);
			// 获取Compiler::compCompile
			_compCompileStub = CompCompileStub;
			_pCompCompileStub = MarshalEx.GetDelegateAddress(_compCompileStub, CallingConvention.FastCall);
			// 调用回调方法的stub
			_compCompileStub(null, null, null, null, null, null, null, null);
		}

		public bool Hook() {
			if (_isHooked)
				throw new InvalidOperationException(nameof(_isHooked) + " = true");

			MarshalEx.Write(_pCallCompCompileOperation, BitConverter.GetBytes((int)_pCompCompileStub - (int)_pCallCompCompileOperation - 4));
			_isHooked = true;
			return true;
		}

		public bool Unhook() {
			if (!_isHooked)
				throw new InvalidOperationException(nameof(_isHooked) + " = false");

			MarshalEx.Write(_pCallCompCompileOperation, BitConverter.GetBytes((int)_pCompCompileOriginal - (int)_pCallCompCompileOperation - 4));
			_isHooked = false;
			return true;
		}

		[HandleProcessCorruptedStateExceptions]
		private int CompCompileStub(void* pThis, void* methodHandle, void* moduleHandle, void* pICorJitInfo, void* pMethodInfo, byte** ppEntryAddress, uint* pNativeSizeOfCode, void* compileFlags) {
			CorMethodInfo methodInfo;
			JitCompilationInfo compilationInfo;

			if (pThis == null)
				return 0;
			methodInfo = new CorMethodInfo(pMethodInfo);
			if (methodInfo.ModuleHandle != _targetModuleHandle || JitHookUtils.GetRealMethodHandle(methodInfo.MethodHandle) != _targetMethodHandle || _depth > 20)
				return _compCompileOriginal(pThis, methodHandle, moduleHandle, pICorJitInfo, pMethodInfo, ppEntryAddress, pNativeSizeOfCode, compileFlags);
			compilationInfo = new JitCompilationInfo {
				PointerOfICorJitInfo = pICorJitInfo,
				MethodInfo = methodInfo,
				PointerOfNativeCodeAddress = ppEntryAddress,
				PointerOfNativeCodeSize = pNativeSizeOfCode
			};
			try {
				_depth++;
				// 深度，表示CompCompileStub被嵌套调用了多少次，防止调用次数过多导致堆栈溢出
				if (_callback(compilationInfo))
					// 返回true表示不将方法交给JIT编译器进行继续编译
					return 0;
				pICorJitInfo = compilationInfo.PointerOfICorJitInfo;
				pMethodInfo = compilationInfo.MethodInfo.PointerOfData;
				ppEntryAddress = compilationInfo.PointerOfNativeCodeAddress;
				pNativeSizeOfCode = compilationInfo.PointerOfNativeCodeSize;
				// 使用回调方法修改后的数据进行编译
				return _compCompileOriginal(pThis, methodHandle, moduleHandle, pICorJitInfo, pMethodInfo, ppEntryAddress, pNativeSizeOfCode, compileFlags);
			}
			finally {
				_depth = 0;
			}
		}
	}
}
