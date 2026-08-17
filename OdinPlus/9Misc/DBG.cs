using System;

namespace OdinPlus
{
	public enum LogLevel
	{
		None = 0,
		Error = 1,
		Warn = 2,
		Info = 3,
		Debug = 4
	}

	public static class DBG
	{
		internal static LogLevel Level = LogLevel.Info;

		public static void cprt(string s) => global::Console.instance.Print(s);

		public static void InfoTL(string s) =>
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, s, 0, null);

		public static void InfoCT(string s) =>
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, s, 0, null);

		public static void blogDebug(object o)
		{
			if(Level >= LogLevel.Debug) Plugin.logger.LogInfo($"[DBG] {o}");
		}

		public static void blogInfo(object o)
		{
			if(Level >= LogLevel.Info) Plugin.logger.LogInfo(o);
		}

		public static void blogWarning(object o)
		{
			if(Level >= LogLevel.Warn) Plugin.logger.LogWarning(o);
		}

		public static void blogError(object o)
		{
			if(Level >= LogLevel.Error) Plugin.logger.LogError(o);
		}
	}
}
