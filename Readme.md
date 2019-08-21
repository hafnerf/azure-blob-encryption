# Blob Encryption on Upload

This function is based on https://docs.microsoft.com/en-us/azure/storage/common/storage-client-side-encryption

This Functions encrypts uploaded Files with a Unique Key and uploads the File as an encrypted Blob to Azure Blob Storage.

The Key used for Encryption will be wrapped with an Azure KeyVault Key and attached as Metadata to the Blob.

To decrypt the Blob one needs access to the KeyVault Key used to wrap the Encryption Key.

Access to the different Services is managed through RBAC via a Managed Function Identity.

Config Variabels:

keyvault: KeyVault Endpoint e.g. https://contosovault.vault.azure.net
key: Name of the Key used for Key Wrapping e.g. sample-key
storage_account: StorageAccount Endpoint e.g. https://contosostorageaccount.blob.core.windows.net
blob_container: Name of the Blob Container e.g. sampleblobcontainer