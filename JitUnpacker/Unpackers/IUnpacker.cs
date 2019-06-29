namespace JitTools.Unpackers {
	internal interface IUnpacker {
		/// <summary>
		/// 脱壳机全名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// 壳ID（壳名称的缩写）
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Context
		/// </summary>
		UnpackerContext Context { get; }

		/// <summary>
		/// MethodDumper
		/// </summary>
		IMethodDumper MethodDumper { get; }

		/// <summary>
		/// 在cctor运行前初始化
		/// </summary>
		void PreInitialize();

		/// <summary>
		/// 在cctor运行后初始化
		/// </summary>
		void PostInitialize();

		/// <summary>
		/// 判断指定方法是否需要解密
		/// </summary>
		/// <param name="index">MethodHandle的索引</param>
		/// <returns></returns>
		bool NeedDecryptMethod(int index);

		/// <summary>
		/// 调用JIT
		/// </summary>
		/// <param name="index">MethodHandle的索引</param>
		void CallJit(int index);

		/// <summary>
		/// 解密完成后移除目标程序集中初始化运行时的代码
		/// </summary>
		void RemoveRuntime();
	}
}
