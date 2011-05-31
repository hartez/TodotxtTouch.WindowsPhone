using System;
using System.Diagnostics;
using AgiliTrain.PhoneyTools;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	static class Trace
	{
		internal static void Write(PhoneLogger.LogLevel logLevel, string message, params object[] args)
		{
			PhoneLogger.MinimumLogLevel = PhoneLogger.LogLevel.Info;
			Debug.WriteLine(message, args);
			
			switch(logLevel)
			{
				case PhoneLogger.LogLevel.Info:
					PhoneLogger.LogInfo(message, args);
					break;
				case PhoneLogger.LogLevel.Debug:
					PhoneLogger.LogDebug(message, args);
					break;
				case PhoneLogger.LogLevel.Error:
					PhoneLogger.LogError(message, args);
					break;
				case PhoneLogger.LogLevel.Critical:
					PhoneLogger.LogCritical(message, args);
					break;
				default:
					throw new ArgumentOutOfRangeException("logLevel");
			}
		}
	}
}