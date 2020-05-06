using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace Secrecy
{
    class Options : BaseOptionModel<Options>
    {
        private static bool cached = false;
        private static Options cache;

        [Category("Secrecy")]
        [DisplayName("Hashicorp Vault token")]
        [Description("Leave empty when want to use ENV variable VAULT_TOKEN")]
        [DefaultValue("token")]
        public string HashicorpVaultToken { get; set; } = "token";

        [Category("Secrecy")]
        [DisplayName("Hashicorp Vault adress and port")]
        [Description("Use full url, like: https://127.0.0.1:8200")]
        [DefaultValue("https://127.0.0.1:8200")]
        public string HashicorpVaultAddress { get; set; } = "https://127.0.0.1:8200";

        [Category("Secrecy")]
        [DisplayName("Credstash AWS region")]
        [Description("Use format: us-east-2")]
        [DefaultValue("us-east-2")]
        public string CredstashAWSRegion { get; set; } = "us-east-2";

        [Category("Secrecy")]
        [DisplayName("AWS KMS region")]
        [Description("Use format: us-east-2")]
        [DefaultValue("us-east-2")]
        public string AWSKMSRegion { get; set; } = "us-east-2";

        [Category("Secrecy")]
        [DisplayName("AWS SSM region")]
        [Description("Use format: us-east-2")]
        [DefaultValue("us-east-2")]
        public string AWSSSMRegion { get; set; } = "us-east-2";

        [Category("Secrecy")]
        [DisplayName("Azure Key Vault url")]
        [Description("Use format: https://vault.azure.net")]
        [DefaultValue("https://replaceme.azure.net")]
        public string AKVUrl { get; set; } = "https://replaceme.azure.net";
        
        [Category("Secrecy")]
        [DisplayName("Azure Key Vault short name (for powershell")]
        [Description("Use format: vaultname")]
        [DefaultValue("replaceme")]
        public string AKVShortName { get; set; } = "replaceme";

        [Category("Secrecy")]
        [DisplayName("Offer Credstash")]
        [DefaultValue(true)]
        public bool OfferCredstash { get; set; } = true;

        [Category("Secrecy")]
        [DisplayName("Offer AWS KMS")]
        [DefaultValue(true)]
        public bool OfferAWSKMS { get; set; } = true;

        [Category("Secrecy")]
        [DisplayName("Offer AWS SSM")]
        [DefaultValue(true)]
        public bool OfferAWSSSM { get; set; } = true;

        [Category("Secrecy")]
        [DisplayName("Offer Hashicorp Vault")]
        [DefaultValue(true)]
        public bool OfferHashicorpVault { get; set; } = true;

        [Category("Secrecy")]
        [DisplayName("Offer Azure Key Vault")]
        [DefaultValue(true)]
        public bool OfferAKV { get; set; } = true;

    }

}
