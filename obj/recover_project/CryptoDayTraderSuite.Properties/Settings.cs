using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CryptoDayTraderSuite.Properties
{
	[CompilerGenerated]
	[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.0.3.0")]
	internal sealed class Settings : ApplicationSettingsBase
	{
		private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

		public static Settings Default => defaultInstance;

		[UserScopedSetting]
		[DebuggerNonUserCode]
		[DefaultSettingValue("")]
		public string ApiKey
		{
			get
			{
				return (string)this["ApiKey"];
			}
			set
			{
				this["ApiKey"] = value;
			}
		}

		[UserScopedSetting]
		[DebuggerNonUserCode]
		[DefaultSettingValue("")]
		public string ApiSecret
		{
			get
			{
				return (string)this["ApiSecret"];
			}
			set
			{
				this["ApiSecret"] = value;
			}
		}

		[UserScopedSetting]
		[DebuggerNonUserCode]
		[DefaultSettingValue("")]
		public string ApiExtra
		{
			get
			{
				return (string)this["ApiExtra"];
			}
			set
			{
				this["ApiExtra"] = value;
			}
		}

		[UserScopedSetting]
		[DebuggerNonUserCode]
		[DefaultSettingValue("False")]
		public bool AutoModeAutoRunEnabled
		{
			get
			{
				return (bool)this["AutoModeAutoRunEnabled"];
			}
			set
			{
				this["AutoModeAutoRunEnabled"] = value;
			}
		}
	}
}
