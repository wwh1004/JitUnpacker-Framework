using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.IO;
using static JitTools.NativeMethods;

namespace JitTools.Unpackers {
	internal delegate uint TokenResolver(Code code, uint token);

	internal sealed class JitMethodBodyReader : MethodBodyReaderBase {
		private readonly ModuleDefMD _moduleDef;
		private TokenResolver _tokenResolver;

		public TokenResolver TokenResolver {
			get => _tokenResolver;
			set => _tokenResolver = value;
		}

		public JitMethodBodyReader(ModuleDefMD moduleDef, IList<Parameter> parameters) {
			if (moduleDef == null)
				throw new ArgumentNullException(nameof(moduleDef));
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			_moduleDef = moduleDef;
			base.parameters = parameters;
		}

		public CilBody CreateCilBody(byte[] byteILs, ushort maxStack, byte[] byteLocalSig, CORINFO_EH_CLAUSE[] clauses) {
			if (byteILs == null)
				throw new ArgumentNullException(nameof(byteILs));

			if (byteLocalSig != null) {
				CallingConventionSig callingConventionSig;

				callingConventionSig = SignatureReader.ReadSig(_moduleDef, byteLocalSig);
				SetLocals(((LocalSig)callingConventionSig).Locals);
			}
			reader = ByteArrayDataReaderFactory.CreateReader(byteILs);
			ReadInstructionsNumBytes((uint)byteILs.Length);
			if (clauses != null)
				foreach (CORINFO_EH_CLAUSE clause in clauses) {
					ExceptionHandler exceptionHandler;

					exceptionHandler = new ExceptionHandler((ExceptionHandlerType)clause.Flags) {
						TryStart = GetInstruction(clause.TryOffset),
						TryEnd = GetInstruction(clause.TryOffset + clause.TryLength),
						HandlerStart = GetInstruction(clause.HandlerOffset),
						HandlerEnd = GetInstruction(clause.HandlerOffset + clause.HandlerLength)
					};
					if (exceptionHandler.HandlerType == ExceptionHandlerType.Catch)
						exceptionHandler.CatchType = (ITypeDefOrRef)_moduleDef.ResolveToken(clause.ClassTokenOrFilterOffset);
					else if (exceptionHandler.HandlerType == ExceptionHandlerType.Filter)
						exceptionHandler.FilterStart = GetInstruction(clause.ClassTokenOrFilterOffset);
					Add(exceptionHandler);
				}
			return new CilBody(byteLocalSig != null, instructions, exceptionHandlers, locals) {
				MaxStack = maxStack
			};
		}

		protected override IField ReadInlineField(Instruction instr) {
			return (IField)_moduleDef.ResolveToken(ResolveToken(instr.OpCode.Code, reader.ReadUInt32()));
		}

		protected override IMethod ReadInlineMethod(Instruction instr) {
			return (IMethod)_moduleDef.ResolveToken(ResolveToken(instr.OpCode.Code, reader.ReadUInt32()));
		}

		protected override MethodSig ReadInlineSig(Instruction instr) {
			StandAloneSig standAloneSig;
			MethodSig methodSig;

			standAloneSig = (StandAloneSig)_moduleDef.ResolveToken(ResolveToken(instr.OpCode.Code, reader.ReadUInt32()));
			if (standAloneSig == null) {
				Logger.Instance.LogError("ReadInlineSig failed");
				return null;
			}
			methodSig = standAloneSig.MethodSig;
			if (methodSig != null)
				methodSig.OriginalToken = standAloneSig.MDToken.Raw;
			return methodSig;
		}

		protected override string ReadInlineString(Instruction instr) {
			return _moduleDef.ReadUserString(reader.ReadUInt32());
		}

		protected override ITokenOperand ReadInlineTok(Instruction instr) {
			return (ITokenOperand)_moduleDef.ResolveToken(ResolveToken(instr.OpCode.Code, reader.ReadUInt32()));
		}

		protected override ITypeDefOrRef ReadInlineType(Instruction instr) {
			return (ITypeDefOrRef)_moduleDef.ResolveToken(ResolveToken(instr.OpCode.Code, reader.ReadUInt32()));
		}

		private uint ResolveToken(Code code, uint token) {
			return _tokenResolver == null ? token : _tokenResolver(code, token);
		}
	}
}
