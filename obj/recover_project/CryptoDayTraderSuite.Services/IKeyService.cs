using System.Collections.Generic;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.Services
{
	public interface IKeyService
	{
		List<KeyInfo> GetAll();

		KeyInfo Get(string id);

		KeyInfo Get(string broker, string label);

		void Upsert(KeyInfo info);

		void Delete(string id);

		void Remove(string broker, string label);

		void ReplaceAll(List<KeyInfo> items);

		string GetActiveId();

		string GetActiveId(string broker);

		void SetActive(string id);

		void SetActive(string broker, string label);

		string Protect(string plain);

		string Unprotect(string cipher);
	}
}
