using System;
using dnlib.DotNet;

namespace JitTools {
	internal sealed class DnlibLogger : ILogger {
		private static readonly DnlibLogger _instance = new DnlibLogger();

		private DnlibLogger() {
		}

		public static DnlibLogger Instance => _instance;

		public bool IgnoresEvent(LoggerEvent loggerEvent) {
			return false;
		}

		public void Log(object sender, LoggerEvent loggerEvent, string format, params object[] args) {
			ConsoleColor oldColor;

			oldColor = Console.ForegroundColor;
			switch (loggerEvent) {
			case LoggerEvent.Error:
				Console.ForegroundColor = ConsoleColor.Red;
				break;
			case LoggerEvent.Warning:
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			case LoggerEvent.Info:
				Console.ForegroundColor = ConsoleColor.Gray;
				break;
			case LoggerEvent.Verbose:
			case LoggerEvent.VeryVerbose:
				Console.ForegroundColor = ConsoleColor.DarkGray;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(loggerEvent));
			}
			Console.WriteLine("LoggerEvent: " + loggerEvent.ToString());
			Console.WriteLine("Info: " + Environment.NewLine + string.Format(format, args));
			Console.WriteLine();
			Console.ForegroundColor = oldColor;
		}
	}
}
