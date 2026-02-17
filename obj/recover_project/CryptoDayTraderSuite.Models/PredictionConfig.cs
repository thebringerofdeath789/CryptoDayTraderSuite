namespace CryptoDayTraderSuite.Models
{
	public class PredictionConfig
	{
		public decimal LearningRate { get; set; } = 0.05m;

		public decimal L2Regularization { get; set; } = 0.0001m;

		public int RollingEventsWindow { get; set; } = 500;
	}
}
