using System.Runtime.InteropServices;
using System.Text;

namespace JitTools {
	internal static unsafe class NativeMethods {
		public const uint MAX_PATH = 260;
		public const uint PAGE_EXECUTE_READWRITE = 0x40;

		public enum CorInfoOptions {
			CORINFO_OPT_INIT_LOCALS = 0x00000010,                // zero initialize all variables
			CORINFO_GENERICS_CTXT_FROM_THIS = 0x00000020,        // is this shared generic code that access the generic context from the this pointer?  If so, then if the method has SEH then the 'this' pointer must always be reported and kept alive.
			CORINFO_GENERICS_CTXT_FROM_METHODDESC = 0x00000040,  // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodDesc)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
			CORINFO_GENERICS_CTXT_FROM_METHODTABLE = 0x00000080, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodTable)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
			CORINFO_GENERICS_CTXT_MASK = CORINFO_GENERICS_CTXT_FROM_THIS | CORINFO_GENERICS_CTXT_FROM_METHODDESC | CORINFO_GENERICS_CTXT_FROM_METHODTABLE,
			CORINFO_GENERICS_CTXT_KEEP_ALIVE = 0x00000100,       //  Keep the generics context alive throughout the method even if there is no explicit use, and report its location to the CLR
		};

		public enum CORINFO_EH_CLAUSE_FLAGS {
			CORINFO_EH_CLAUSE_NONE = 0,
			CORINFO_EH_CLAUSE_FILTER = 0x0001,
			CORINFO_EH_CLAUSE_FINALLY = 0x0002,
			CORINFO_EH_CLAUSE_FAULT = 0x0004,
			CORINFO_EH_CLAUSE_DUPLICATE = 0x0008,
			CORINFO_EH_CLAUSE_SAMETRY = 0x0010,
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_SECTION_HEADER {
			public fixed sbyte Name[8];
			public uint VirtualSize;
			public uint VirtualAddress;
			public uint SizeOfRawData;
			public uint PointerToRawData;
			public uint PointerToRelocations;
			public uint PointerToLinenumbers;
			public ushort NumberOfRelocations;
			public ushort NumberOfLinenumbers;
			public uint Characteristics;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CORINFO_METHOD_INFO_20 {
			public void* ftn;               // CORINFO_METHOD_HANDLE
			public void* scope;             // CORINFO_MODULE_HANDLE
			public byte* ILCode;
			public uint ILCodeSize;
			public ushort maxStack;
			public ushort EHcount;
			public CorInfoOptions options;
			public uint regionKind;         // CorInfoRegionKind
			public CORINFO_SIG_INFO_20 args;
			public CORINFO_SIG_INFO_20 locals;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct CORINFO_METHOD_INFO_40 {
			public void* ftn;               // CORINFO_METHOD_HANDLE
			public void* scope;             // CORINFO_MODULE_HANDLE
			public byte* ILCode;
			public uint ILCodeSize;
			public uint maxStack;
			public uint EHcount;
			public CorInfoOptions options;
			public uint regionKind;         // CorInfoRegionKind
			public CORINFO_SIG_INFO_40 args;
			public CORINFO_SIG_INFO_40 locals;
		};

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CORINFO_SIG_INFO_20 {
			public uint callConv; // CorInfoCallConv
			public void* retTypeClass; // CORINFO_CLASS_HANDLE
			public void* retTypeSigClass; // CORINFO_CLASS_HANDLE
			public byte retType; // CorInfoType
			public byte flags;
			public ushort numArgs;
			public CORINFO_SIG_INST sigInst;
			public void* args; // CORINFO_ARG_LIST_HANDLE
			public void* pSig; // PCCOR_SIGNATURE
			public void* scope; // CORINFO_MODULE_HANDLE
			public uint token;
		};

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CORINFO_SIG_INFO_40 {
			public uint callConv; // CorInfoCallConv
			public void* retTypeClass; // CORINFO_CLASS_HANDLE
			public void* retTypeSigClass; // CORINFO_CLASS_HANDLE
			public byte retType; // CorInfoType
			public byte flags;
			public ushort numArgs;
			public CORINFO_SIG_INST sigInst;
			public void* args; // CORINFO_ARG_LIST_HANDLE
			public void* pSig; // PCCOR_SIGNATURE
			public uint cbSig;
			public void* scope; // CORINFO_MODULE_HANDLE
			public uint token;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct CORINFO_SIG_INST {
			public uint classInstCount;
			public void** classInst; // CORINFO_CLASS_HANDLE*
			public uint methInstCount;
			public void** methInst; // CORINFO_CLASS_HANDLE*
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CORINFO_EH_CLAUSE {
			public CORINFO_EH_CLAUSE_FLAGS Flags;
			public uint TryOffset;
			public uint TryLength;
			public uint HandlerOffset;
			public uint HandlerLength;
			public uint ClassTokenOrFilterOffset;
		}

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void ResetDelegate(void* pMethodDesc);

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void* DoPrestubDelegate(void* pMethodDesc, void* pDispatchingMT);

		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate void* GetWrappedMethodDescDelegate(void* pMethodDesc);

		public delegate int CompCompileDelegate(void* pThis, void* methodHnd, void* classPtr, void* compHnd, void* methodInfo, byte** methodCodePtr, uint* methodCodeSize, void* compileFlags);

		public delegate int CompileMethodDelegate(void* pThis, void* compHnd, void* methodInfo, uint flags, byte** entryAddress, uint* nativeSizeOfCode);

		public delegate void GetEHInfoDelegate(void* pThis, void* ftn, uint EHnumber, out CORINFO_EH_CLAUSE clause);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetModuleFileName(void* hModule, StringBuilder lpFilename, uint nSize);

		[DllImport("mscorjit.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "getJit", SetLastError = true)]
		public static extern void* GetJit2();

		[DllImport("clrjit.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "getJit", SetLastError = true)]
		public static extern void* GetJit4();

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern void* GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool VirtualProtect(void* lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);
	}
}
