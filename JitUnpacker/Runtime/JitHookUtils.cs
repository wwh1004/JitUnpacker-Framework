using System;
using System.Reflection;

namespace JitTools.Runtime {
	internal static unsafe class JitHookUtils {
		private static readonly ConstructorInfo ConstructorInfo_RuntimeMethodHandleInternal;
		private static readonly MethodBase MethodBase_GetDeclaringType;
		private static readonly MethodBase MethodBase_GetStubIfNeeded;
		private static readonly FieldInfo FieldInfo_m_handle;
		private static readonly MethodBase MethodBase_GetUnboxingStub;

		static JitHookUtils() {
			if (RuntimeEnvironment.IsClr4x) {
				Type type_RuntimeMethodHandleInternal;

				type_RuntimeMethodHandleInternal = Type.GetType("System.RuntimeMethodHandleInternal");
				ConstructorInfo_RuntimeMethodHandleInternal = type_RuntimeMethodHandleInternal.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(IntPtr) }, null);
				MethodBase_GetDeclaringType = typeof(RuntimeMethodHandle).GetMethod("GetDeclaringType", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { Type.GetType("System.RuntimeMethodHandleInternal") }, null);
				MethodBase_GetStubIfNeeded = typeof(RuntimeMethodHandle).GetMethod("GetStubIfNeeded", BindingFlags.NonPublic | BindingFlags.Static);
				FieldInfo_m_handle = type_RuntimeMethodHandleInternal.GetField("m_handle", BindingFlags.NonPublic | BindingFlags.Instance);
			}
			else {
				MethodBase_GetDeclaringType = typeof(RuntimeMethodHandle).GetMethod("GetDeclaringType", BindingFlags.NonPublic | BindingFlags.Instance);
				MethodBase_GetUnboxingStub = typeof(RuntimeMethodHandle).GetMethod("GetUnboxingStub", BindingFlags.NonPublic | BindingFlags.Instance);
			}
		}

		public static void* GetRealMethodHandle(void* methodHandle) {
			if (RuntimeEnvironment.IsClr4x) {
				object runtimeMethodHandleInternal;
				Type declaringType;

				runtimeMethodHandleInternal = ConstructorInfo_RuntimeMethodHandleInternal.Invoke(new object[] { (IntPtr)methodHandle });
				declaringType = (Type)MethodBase_GetDeclaringType.Invoke(null, new object[] { runtimeMethodHandleInternal });
				return declaringType.IsValueType
					? (void*)(IntPtr)FieldInfo_m_handle.GetValue(MethodBase_GetStubIfNeeded.Invoke(null, new object[] { runtimeMethodHandleInternal, declaringType, null }))
					: methodHandle;
			}
			else {
				RuntimeMethodHandle runtimeMethodHandle;

				runtimeMethodHandle = *(RuntimeMethodHandle*)&methodHandle;
				return Type.GetTypeFromHandle((RuntimeTypeHandle)MethodBase_GetDeclaringType.Invoke(runtimeMethodHandle, null)).IsValueType
					? (void*)((RuntimeMethodHandle)MethodBase_GetUnboxingStub.Invoke(runtimeMethodHandle, null)).Value
					: methodHandle;
			}
		}
	}
}
