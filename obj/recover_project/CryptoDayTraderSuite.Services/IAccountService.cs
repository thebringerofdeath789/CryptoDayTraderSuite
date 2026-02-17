using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
	public interface IAccountService
	{
		List<AccountInfo> GetAll();

		AccountInfo Get(string id);

		void Upsert(AccountInfo info);

		void Delete(string id);

		void ReplaceAll(List<AccountInfo> items);
	}
}
