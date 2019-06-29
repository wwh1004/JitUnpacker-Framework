using System;
using System.Text;

namespace JitTools {
	internal sealed class Logger {
		private static readonly Logger _instance = new Logger();
		private static readonly object _syncRoot = new object();

		public static Logger Instance => _instance;

		private Logger() {
		}

		public void LogNewLine() {
			LogInfo(string.Empty, ConsoleColor.Gray);
		}

		public void LogInfo(string value) {
			LogInfo(value, ConsoleColor.Gray);
		}

		public void LogWarning(string value) {
			LogInfo(value, ConsoleColor.Yellow);
		}

		public void LogError(string value) {
			LogInfo(value, ConsoleColor.Red);
		}

		public void LogInfo(string value, ConsoleColor color) {
			lock (_syncRoot) {
				ConsoleColor oldColor;

				oldColor = Console.ForegroundColor;
				Console.ForegroundColor = color;
				Console.WriteLine(value);
				Console.ForegroundColor = oldColor;
			}
		}

		public void LogException(Exception value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			LogError(ExceptionToString(value));
		}

		private static string ExceptionToString(Exception exception) {
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			StringBuilder sb;

			sb = new StringBuilder();
			DumpException(exception, sb);
			return sb.ToString();
		}

		private static void DumpException(Exception exception, StringBuilder sb) {
			sb.AppendLine("Type: " + Environment.NewLine + exception.GetType().FullName);
			sb.AppendLine("Message: " + Environment.NewLine + exception.Message);
			sb.AppendLine("Source: " + Environment.NewLine + exception.Source);
			sb.AppendLine("StackTrace: " + Environment.NewLine + exception.StackTrace);
			sb.AppendLine("TargetSite: " + Environment.NewLine + exception.TargetSite.ToString());
			sb.AppendLine("----------------------------------------");
			if (exception.InnerException != null)
				DumpException(exception.InnerException, sb);
		}
	}
}
