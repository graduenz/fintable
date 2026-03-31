namespace Fintable.Features.Sync;

public static class SyncWarningCodes
{
    public const string NoProvidersToSync = "no_providers_to_sync";
    public const string ProviderTypeNotSupportedSkipped = "provider_type_not_supported_skipped";
    public const string TransactionUnknownAccountSkipped = "transaction_unknown_account_skipped";
    public const string CategoryMappingMissing = "category_mapping_missing";
    public const string InvoiceMappingMissing = "invoice_mapping_missing";
    public const string SyncDataConsistencyRisk = "sync_data_consistency_risk";
}
