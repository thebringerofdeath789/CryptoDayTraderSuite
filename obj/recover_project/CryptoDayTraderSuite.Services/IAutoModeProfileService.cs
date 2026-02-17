using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
	public interface IAutoModeProfileService
	{
		List<AutoModeProfile> GetAll();

		AutoModeProfile Get(string profileId);

		void Upsert(AutoModeProfile profile);

		void Delete(string profileId);

		void ReplaceAll(List<AutoModeProfile> profiles);
	}
}
