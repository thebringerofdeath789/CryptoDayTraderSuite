namespace CryptoDayTraderSuite.Services
{
    internal static class AiJsonSchemas
    {
        public const string GovernorBiasSchema = "{\"bias\":\"Bullish\"|\"Bearish\"|\"Neutral\",\"reason\":\"one short sentence\",\"confidence\":0.0-1.0}";
        public const string GovernorBiasKeysCsv = "bias, reason, confidence";
        public static readonly string[] GovernorBiasKeys = { "bias", "reason", "confidence" };

        public const string PlannerReviewSchema = "{\"bias\":\"Bullish\"|\"Bearish\"|\"Neutral\",\"approve\":true|false,\"reason\":\"one short sentence\",\"confidence\":0.0-1.0,\"SuggestedLimit\":0.0}";
        public const string PlannerReviewKeysCsv = "bias, approve, reason, confidence, SuggestedLimit";
        public static readonly string[] PlannerReviewKeys = { "bias", "approve", "reason", "confidence", "SuggestedLimit" };

        public const string PlannerProposerSchema = "{\"approve\":true|false,\"symbol\":\"...\",\"side\":\"Buy\"|\"Sell\",\"entry\":0.0,\"stop\":0.0,\"target\":0.0,\"strategyHint\":\"...\",\"reason\":\"one short sentence\",\"confidence\":0.0-1.0}";
        public const string PlannerProposerKeysCsv = "approve, symbol, side, entry, stop, target, strategyHint, reason, confidence";
        public static readonly string[] PlannerProposerKeys = { "approve", "symbol", "side", "entry", "stop", "target", "strategyHint", "reason", "confidence" };
    }
}