namespace JitTools.Unpackers {
	/// <summary>
	/// 相对于目标方法的Dump状态，因为DNGuardHVM会多次调用JIT，并且一般情况下最后一次调用JIT时才可以得到真正的IL
	/// </summary>
	internal enum DumpingState {
		Waiting,
		Dumping,
		Finished
	}

	internal interface IMethodDumper {
		DumpingState State { get; }

		uint DumpCount { get; }

		void Hook();

		void Unhook();

		void SetTargetMethod(int index);

		void SetIdle();
	}
}
