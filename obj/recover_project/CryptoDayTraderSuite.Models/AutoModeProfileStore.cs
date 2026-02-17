using System;
using System.Collections.Generic;

namespace CryptoDayTraderSuite.Models
{
	[Serializable]
	public class AutoModeProfileStore
	{
		public int Version = 1;

		public List<AutoModeProfile> Profiles = new List<AutoModeProfile>();
	}
}
