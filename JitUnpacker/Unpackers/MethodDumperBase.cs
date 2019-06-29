using System;
using dnlib.DotNet;
using JitTools.Runtime;
using static JitTools.NativeMethods;
using CallingConvention = System.Runtime.InteropServices.CallingConvention;
using RuntimeEnvironment = JitTools.Runtime.RuntimeEnvironment;

namespace JitTools.Unpackers {
	internal abstract unsafe class MethodDumperBase : IMethodDumper {
		protected readonly UnpackerContext _context;
		protected readonly IJitHook _jitHook;
		protected GetEHInfoDelegate _getEHInfo;
		protected int _targetIndex;
		protected DumpingState _state;
		protected uint _dumpCount;

		public DumpingState State => _state;

		public uint DumpCount => _dumpCount;

		protected MethodDumperBase(UnpackerContext context) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			_context = context;
			_jitHook = JitHookFactory.Create(_context.Settings.HookType);
			_jitHook.TargetModuleHandle = _context.ModuleHandle;
			_jitHook.Callback = OnJitCompilation;
			SetIdle();
		}

		protected abstract bool OnJitCompilation(JitCompilationInfo compilationInfo);

		public virtual void Hook() {
			SetIdle();
			_jitHook.Hook();
		}

		public virtual void Unhook() {
			_jitHook.Unhook();
			SetIdle();
		}

		public virtual void SetTargetMethod(int index) {
			_targetIndex = index;
			_jitHook.TargetMethodHandle = _context.MethodHandles[index];
			_state = DumpingState.Waiting;
		}

		public virtual void SetIdle() {
			_targetIndex = -1;
			_jitHook.TargetMethodHandle = null;
			_state = DumpingState.Waiting;
		}

		protected void EnsueGetEHInfo(void* pICorJitInfo) {
			if (_getEHInfo == null)
				InitializeGetEHInfo(pICorJitInfo);
		}

		protected virtual void InitializeGetEHInfo(void* pICorJitInfo) {
			void* pGetEHInfo;

			pGetEHInfo = VTableHelpers.GetGetEHInfo(pICorJitInfo);
			if (RuntimeEnvironment.IsClr45x)
				pGetEHInfo = MarshalEx.ConvertCallingConvention(pGetEHInfo, CallingConvention.ThisCall, CallingConvention.StdCall);
			_getEHInfo = MarshalEx.CreateDelegate<GetEHInfoDelegate>(pGetEHInfo);
		}

		protected static byte[] ReadILs(CorMethodInfo methodInfo) {
			byte[] byteILs;

			byteILs = new byte[methodInfo.ILCodeSize];
			MarshalEx.Read(methodInfo.ILCode, byteILs);
			return byteILs;
		}

		protected static byte[] GetVariables(void* pArg, uint count) {
			byte* pNextArg;
			byte elementType;
			byte[] variables;

			pNextArg = (byte*)pArg;
			for (uint i = 0; i < count; i++) {
				elementType = *pNextArg++;
				WalkType(ref pNextArg, elementType);
			}
			variables = new byte[pNextArg - (byte*)pArg];
			MarshalEx.Read(pArg, variables);
			return variables;
		}

		protected static byte[] BuildLocalSig(byte[] variables, uint count) {
			int compressedCountLength;
			byte[] localSig;

			compressedCountLength = GetCompressedUInt32Length(count);
			localSig = new byte[1 + compressedCountLength + variables.Length];
			// CORINFO_CALLCONV_LOCAL_SIG + CompressedNumberOfVariables
			localSig[0] = 0x07;
			WriteCompressedUInt32(localSig, 1, count);
			Buffer.BlockCopy(variables, 0, localSig, 1 + compressedCountLength, variables.Length);
			return localSig;
		}

		private static int GetCompressedUInt32Length(uint value) {
			if (value <= 0x7F)
				return 1;
			if (value <= 0x3FFF)
				return 2;
			if (value <= 0x1FFFFFFF)
				return 4;
			else
				throw new ArgumentOutOfRangeException(nameof(value));
		}

		private static void WriteCompressedUInt32(byte[] buffer, int startIndex, uint value) {
			if (value <= 0x7F)
				buffer[startIndex + 0] = (byte)value;
			else if (value <= 0x3FFF) {
				buffer[startIndex + 0] = (byte)((value >> 8) | 0x80);
				buffer[startIndex + 1] = (byte)value;
			}
			else if (value <= 0x1FFFFFFF) {
				buffer[startIndex + 0] = (byte)((value >> 24) | 0xC0);
				buffer[startIndex + 1] = (byte)(value >> 16);
				buffer[startIndex + 2] = (byte)(value >> 8);
				buffer[startIndex + 3] = (byte)value;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(value));
		}

		protected static byte[] GetNextVariable(ref void* pArg) {
			byte* pNextArg;
			byte elementType;
			byte[] variable;

			pNextArg = (byte*)pArg;
			elementType = *pNextArg++;
			WalkType(ref pNextArg, elementType);
			variable = new byte[pNextArg - (byte*)pArg];
			MarshalEx.Read(pArg, variable);
			pArg = pNextArg;
			return variable;
		}

		private static void WalkType(ref byte* p, byte elementType) {
			byte t;

			switch ((ElementType)elementType) {
			case ElementType.ValueType:
				ReadCompressedUInt32(ref p);
				break;
			case ElementType.Class:
				ReadCompressedUInt32(ref p);
				break;
			case ElementType.Ptr:
				t = *p; p++;
				WalkType(ref p, t);
				break;
			case ElementType.FnPtr: {
					byte conv = *p; p++;
					if ((conv & 0x10) != 0)
						ReadCompressedUInt32(ref p);
					uint paramCount = ReadCompressedUInt32(ref p);
					t = *p; p++;
					WalkType(ref p, t);
					for (uint i = 0; i < paramCount; i++) {
						t = *p; p++;
						WalkType(ref p, t);
					}
				}
				break;
			case ElementType.ByRef:
				t = *p; p++;
				WalkType(ref p, t);
				break;
			case ElementType.Pinned:
				t = *p; p++;
				WalkType(ref p, t);
				break;
			case ElementType.SZArray:
				t = *p; p++;
				WalkType(ref p, t);
				break;
			case ElementType.Array: {
					t = *p; p++;
					WalkType(ref p, t);
					_ = ReadCompressedUInt32(ref p);
					// rank
					uint sizes = ReadCompressedUInt32(ref p);
					for (uint i = 0; i < sizes; i++)
						ReadCompressedUInt32(ref p);
					uint low_bounds = ReadCompressedUInt32(ref p);
					for (uint i = 0; i < low_bounds; i++)
						ReadCompressedUInt32(ref p);
				}
				break;
			case ElementType.CModOpt:
				ReadCompressedUInt32(ref p);
				t = *p; p++;
				WalkType(ref p, t);
				break;
			case ElementType.CModReqd:
				ReadCompressedUInt32(ref p);
				t = *p; p++;
				WalkType(ref p, t);
				break;
			case ElementType.Sentinel:
				t = *p; p++;
				WalkType(ref p, t);
				break;
			case ElementType.Var:
				ReadCompressedUInt32(ref p);
				break;
			case ElementType.MVar:
				ReadCompressedUInt32(ref p);
				break;
			case ElementType.GenericInst: {
					p++;
					ReadCompressedUInt32(ref p);
					uint arity = ReadCompressedUInt32(ref p);
					for (uint i = 0; i < arity; i++) {
						t = *p; p++;
						WalkType(ref p, t);
					}
				}
				break;
			}
		}

		private static uint ReadCompressedUInt32(ref byte* p) {
			uint result;
			byte first;

			first = *p;
			p++;
			if ((first & 0x80) == 0)
				result = first;
			else if ((first & 0x40) == 0) {
				result = ((uint)(first & ~0x80) << 8) | *p;
				p++;
			}
			else {
				byte a = *p; p++;
				byte b = *p; p++;
				byte c = *p; p++;
				result = ((uint)(first & ~0xc0) << 24) | (uint)a << 16 | (uint)b << 8 | c;
			}
			return result;
		}

		protected CORINFO_EH_CLAUSE[] GetAllExceptionHandlers(void* pICorJitInfo, CorMethodInfo methodInfo) {
			CORINFO_EH_CLAUSE[] clauses;

			if (methodInfo.ExceptionHandlerCount == 0)
				return null;
			clauses = new CORINFO_EH_CLAUSE[methodInfo.ExceptionHandlerCount];
			for (uint i = 0; i < clauses.Length; i++)
				GetEHInfo(pICorJitInfo, methodInfo.MethodHandle, i, out clauses[i]);
			return clauses;
		}

		protected virtual void GetEHInfo(void* pICorJitInfo, void* methodHandle, uint ehIndex, out CORINFO_EH_CLAUSE clause) {
			_getEHInfo(VTableHelpers.AdjustThis_GetEHInfo(pICorJitInfo), methodHandle, ehIndex, out clause);
		}

		protected void RestoreMethod(int index, byte[] byteILs, ushort maxStack, byte[] byteLocalSig, CORINFO_EH_CLAUSE[] clauses, TokenResolver tokenResolver) {
			MethodDef methodDef;
			JitMethodBodyReader methodBodyReader;

			methodDef = _context.DumpedModuleDef.ResolveMethod((uint)index + 1);
			methodBodyReader = new JitMethodBodyReader(_context.DumpedModuleDef, methodDef.Parameters) {
				TokenResolver = tokenResolver
			};
			methodDef.FreeMethodBody();
			methodDef.Body = methodBodyReader.CreateCilBody(byteILs, maxStack, byteLocalSig, clauses);
		}

		protected static class VTableHelpers {
			public static void* GetGetEHInfo(void* pICorJitInfo) {
				// CLR47:
				// 8BBB 48190000 | mov     edi, [ebx+0x1948]            |
				// 8D5424 3C     | lea     edx, [esp+0x3C]              |
				// 52            | push    edx                          |
				// 51            | push    ecx                          |
				// FFB3 54190000 | push    [ebx+0x1954]                 |
				// 8B07          | mov     eax, [edi]                   |
				// 8B70 20       | mov     esi, [eax+0x20]              |
				// 8BCE          | mov     ecx, esi                     |
				// FF15 8C112D6F | call    [<__guard_check_icall_fptr>] |
				// 8BCF          | mov     ecx, edi                     |
				// FFD6          | call    esi                          |

				// CLR40:
				// 8B86 D81B0000 | mov     eax, [esi+0x1BD8]  | eax是pICorJitInfo
				// 8B48 04       | mov     ecx, [eax+0x4]     |
				// 8B49 04       | mov     ecx, [ecx+0x4]     |
				// 8D55 E4       | lea     edx, [ebp-0x1C]    |
				// 52            | push    edx                |
				// FF75 E0       | push    [ebp-0x20]         |
				// 8D4401 04     | lea     eax, [ecx+eax+0x4] |
				// FFB6 E41B0000 | push    [esi+0x1BE4]       |
				// 8B08          | mov     ecx, [eax]         |
				// 50            | push    eax                |
				// FF51 28       | call    [ecx+0x28]         | [ecx+0x28]是getEHinfo vtordisp的函数指针

				void* pGetEHInfo;

				if (RuntimeEnvironment.IsClr45x) {
					uint edi;
					uint eax;

					edi = (uint)pICorJitInfo;
					eax = *(uint*)edi;
					pGetEHInfo = *(void**)(eax + 0x20);
				}
				else if (RuntimeEnvironment.IsClr40x) {
					uint eax;
					uint ecx;

					eax = (uint)pICorJitInfo;
					ecx = *(uint*)(eax + 0x4);
					ecx = *(uint*)(ecx + 0x4);
					eax = ecx + eax + 0x4;
					ecx = *(uint*)eax;
					pGetEHInfo = *(void**)(ecx + 0x28);
				}
				else {
					uint eax;
					uint ecx;

					eax = (uint)pICorJitInfo;
					ecx = *(uint*)(eax + 0x4);
					ecx = *(uint*)(ecx + 0x4);
					eax = ecx + eax + 0x4;
					ecx = *(uint*)eax;
					pGetEHInfo = *(void**)(ecx + 0x20);
				}
				return pGetEHInfo;
			}

			public static void* AdjustThis_GetEHInfo(void* pICorJitInfo) {
				void* newThis;

				if (RuntimeEnvironment.IsClr45x)
					newThis = pICorJitInfo;
				else {
					void* temp;

					temp = *(byte**)((byte*)pICorJitInfo + 0x4);
					temp = *(byte**)((byte*)temp + 0x4);
					newThis = (byte*)pICorJitInfo + (int)temp + 4;
				}
				return newThis;
			}
		}
	}
}
