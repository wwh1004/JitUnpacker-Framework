namespace JitTools.Runtime {
	internal sealed unsafe class JitCompilationInfo {
		private void* _pICorJitInfo;
		private CorMethodInfo _methodInfo;
		private byte** _ppNativeCode;
		private uint* _pNativeCodeSize;

		public void* PointerOfICorJitInfo {
			get => _pICorJitInfo;
			set => _pICorJitInfo = value;
		}

		public CorMethodInfo MethodInfo {
			get => _methodInfo;
			set => _methodInfo = value;
		}

		public byte** PointerOfNativeCodeAddress {
			get => _ppNativeCode;
			set => _ppNativeCode = value;
		}

		public uint* PointerOfNativeCodeSize {
			get => _pNativeCodeSize;
			set => _pNativeCodeSize = value;
		}
	}

	/// <summary>
	/// JIT编译回调方法
	/// </summary>
	/// <param name="compilationInfo"></param>
	/// <returns>返回 <see langword="true"/> 表示不将方法交给JIT编译器进行继续编译，反之表示将方法交给JIT编译器进行继续编译</returns>
	internal unsafe delegate bool JitCompilationCallback(JitCompilationInfo compilationInfo);

	/// <summary>
	/// JitHook接口
	/// </summary>
	internal unsafe interface IJitHook {
		/// <summary>
		/// 要截获的模块的句柄
		/// </summary>
		void* TargetModuleHandle { get; set; }

		/// <summary>
		/// 要截获的方法的句柄
		/// </summary>
		void* TargetMethodHandle { get; set; }

		/// <summary>
		/// 编译前触发的回调方法
		/// </summary>
		JitCompilationCallback Callback { get; set; }

		/// <summary>
		/// 安装Hook
		/// </summary>
		/// <returns></returns>
		bool Hook();

		/// <summary>
		/// 卸载Hook
		/// </summary>
		/// <returns></returns>
		bool Unhook();
	}
}
