using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static JitTools.NativeMethods;
using RuntimeEnvironment = JitTools.Runtime.RuntimeEnvironment;

namespace JitTools {
	/// <summary>
	/// 对 <see cref="Marshal"/> 类的简化与扩展
	/// </summary>
	internal static unsafe class MarshalEx {
		private static readonly byte[] ThiscallToStdcallStubCode = {
			0x58,                         // pop     eax
			0x59,                         // pop     ecx
			0x50,                         // push    eax
			0x68, 0x00, 0x00, 0x00, 0x00, // push    pFunction
			0xC3                          // ret
		};
		private static readonly byte[] FastcallToStdcallStubCode = {
			0x58,                         // pop     eax
			0x59,                         // pop     ecx
			0x5A,                         // pop     edx
			0x50,                         // push    eax
			0x68, 0x00, 0x00, 0x00, 0x00, // push    pFunction
			0xC3                          // ret
		};
		private static readonly byte[] StdcallToThiscallStubCode = {
			0x58,                         // pop     eax
			0x51,                         // push    ecx
			0x50,                         // push    eax
			0x68, 0x00, 0x00, 0x00, 0x00, // push    pFunction
			0xC3                          // ret
		};
		private static readonly byte[] StdcallToFastcallStubCode = {
			0x58,                         // pop     eax
			0x52,                         // push    edx
			0x51,                         // push    ecx
			0x50,                         // push    eax
			0x68, 0x00, 0x00, 0x00, 0x00, // push    pFunction
			0xC3                          // ret
		};

		/// <summary>
		/// 分配内存
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public static void* Alloc(uint size) {
			return (void*)Marshal.AllocHGlobal((int)size);
		}

		/// <summary>
		/// 从指定地址读取数据
		/// </summary>
		/// <param name="address"></param>
		/// <param name="value"></param>
		public static void Read(void* address, byte[] value) {
			Marshal.Copy((IntPtr)address, value, 0, value.Length);
		}

		/// <summary>
		/// 向指定地址写入数据
		/// </summary>
		/// <param name="address"></param>
		/// <param name="value"></param>
		public static void Write(void* address, byte[] value) {
			uint oldProtection;

			VirtualProtect(address, (uint)value.Length, PAGE_EXECUTE_READWRITE, out oldProtection);
			Marshal.Copy(value, 0, (IntPtr)address, value.Length);
			VirtualProtect(address, (uint)value.Length, oldProtection, out _);
		}

		/// <summary>
		/// 创建调用约定为Stdcall的委托
		/// </summary>
		/// <typeparam name="TDelegate"></typeparam>
		/// <param name="pStdcallFunction">调用约定为Stdcall的函数地址</param>
		/// <returns></returns>
		public static TDelegate CreateDelegate<TDelegate>(void* pStdcallFunction) where TDelegate : class {
			return CreateDelegate<TDelegate>(pStdcallFunction, CallingConvention.StdCall);
		}

		/// <summary>
		/// 创建调用约定为Stdcall的委托
		/// </summary>
		/// <typeparam name="TDelegate"></typeparam>
		/// <param name="pAnycallFunction">任意调用约定的函数地址</param>
		/// <param name="fromCallingConvention">函数的调用约定</param>
		/// <returns></returns>
		public static TDelegate CreateDelegate<TDelegate>(void* pAnycallFunction, CallingConvention fromCallingConvention) where TDelegate : class {
			if (pAnycallFunction == null)
				throw new ArgumentNullException(nameof(pAnycallFunction));

			void* pStdcallFunction;
			Delegate stdcallDelegate;

			pStdcallFunction = ConvertCallingConvention(pAnycallFunction, fromCallingConvention, CallingConvention.StdCall);
			stdcallDelegate = Marshal.GetDelegateForFunctionPointer((IntPtr)pStdcallFunction, typeof(TDelegate));
			RuntimeHelpers.PrepareDelegate(stdcallDelegate);
			return (TDelegate)(object)stdcallDelegate;
		}

		/// <summary>
		/// 获取调用约定为Stdcall的委托地址
		/// </summary>
		/// <param name="stdcallDelegate">调用约定为Stdcall的委托</param>
		/// <returns></returns>
		public static void* GetDelegateAddress(Delegate stdcallDelegate) {
			return GetDelegateAddress(stdcallDelegate, CallingConvention.StdCall);
		}

		/// <summary>
		/// 获取调用约定为Stdcall的委托地址
		/// </summary>
		/// <param name="stdcallDelegate">任意调用约定的委托</param>
		/// <param name="toCallingConvention">委托的调用约定</param>
		/// <returns></returns>
		public static void* GetDelegateAddress(Delegate stdcallDelegate, CallingConvention toCallingConvention) {
			if (stdcallDelegate == null)
				throw new ArgumentNullException(nameof(stdcallDelegate));

			void* pStdcallFunction;

			RuntimeHelpers.PrepareDelegate(stdcallDelegate);
			pStdcallFunction = (void*)Marshal.GetFunctionPointerForDelegate(stdcallDelegate);
			return ConvertCallingConvention(pStdcallFunction, CallingConvention.StdCall, toCallingConvention);
		}

		/// <summary>
		/// 转换调用约定
		/// </summary>
		/// <param name="pFunction">函数地址</param>
		/// <param name="fromCallingConvention">原来的调用约定</param>
		/// <param name="toCallingConvention">将转换成的调用约定</param>
		/// <returns></returns>
		public static void* ConvertCallingConvention(void* pFunction, CallingConvention fromCallingConvention, CallingConvention toCallingConvention) {
			if (pFunction == null)
				throw new ArgumentNullException(nameof(pFunction));

			byte[] stubCode;
			void* pStub;

			if (!RuntimeEnvironment.Is32Bit)
				// 64位所有调用约定都是一样的
				return pFunction;
			if (fromCallingConvention == CallingConvention.StdCall)
				switch (toCallingConvention) {
				case CallingConvention.Winapi:
				case CallingConvention.StdCall:
					return pFunction;
				case CallingConvention.ThisCall:
					stubCode = CopyByteArray(StdcallToThiscallStubCode);
					fixed (byte* p = &stubCode[4])
						*(uint*)p = (uint)pFunction;
					break;
				case CallingConvention.FastCall:
					stubCode = CopyByteArray(StdcallToFastcallStubCode);
					fixed (byte* p = &stubCode[5])
						*(uint*)p = (uint)pFunction;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(toCallingConvention));
				}
			else if (toCallingConvention == CallingConvention.StdCall)
				switch (fromCallingConvention) {
				case CallingConvention.Winapi:
				case CallingConvention.StdCall:
					return pFunction;
				case CallingConvention.ThisCall:
					stubCode = CopyByteArray(ThiscallToStdcallStubCode);
					fixed (byte* p = &stubCode[4])
						*(uint*)p = (uint)pFunction;
					break;
				case CallingConvention.FastCall:
					stubCode = CopyByteArray(FastcallToStdcallStubCode);
					fixed (byte* p = &stubCode[5])
						*(uint*)p = (uint)pFunction;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(fromCallingConvention));
				}
			else
				throw new ArgumentOutOfRangeException();
			pStub = Alloc((uint)stubCode.Length);
			Write(pStub, stubCode);
			VirtualProtect(pStub, (uint)stubCode.Length, PAGE_EXECUTE_READWRITE, out uint _);
			return pStub;
		}

		private static byte[] CopyByteArray(byte[] array) {
			if (array == null)
				throw new ArgumentNullException(nameof(array));

			byte[] newArray;

			newArray = new byte[array.Length];
			Buffer.BlockCopy(array, 0, newArray, 0, array.Length);
			return newArray;
		}
	}
}
