using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System.Threading;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Secrecy
{
    internal class PutInAKV : ISuggestedAction
    {
        private readonly ITrackingSpan _span;
        private readonly string value;
        private readonly string language;
        private readonly ITextSnapshot _snapshot;
        private readonly string _display;
        internal static IReadOnlyDictionary<string, string> replacementTexts = new Dictionary<string, string>
        {
            ["CSharp"] = "(await new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)).GetSecretAsync(\"{0}/secrets/{1}\")).Value",
            ["JScript"] = "(await new SecretClient('{0}', new ManagedIdentityCredential()).getSecret('{1}')).value",
            ["TypeScript"] = "(await new SecretClient('{0}', new ManagedIdentityCredential()).getSecret('{1}')).value",
            ["Python"] = "SecretClient(vault_url = '{0}', credential = DefaultAzureCredential()).get_secret('{1}').value",
            ["InBoxPowerShell"] = "(Get-AzureKeyVaultSecret -VaultName {0} -Name {1}).SecretValueText",
            ["PowerShell"] = "(Get-AzureKeyVaultSecret -VaultName {0} -Name {1}).SecretValueText"
        };

        public PutInAKV(ITrackingSpan span, string value, string language)
        {
            _span = span;
            this.value = value;
            this.language = language;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            _display = string.Format("Secrecy: Put '{0}' in Azure Key Vault", span.GetText(_snapshot));
        }

        public string DisplayText
        {
            get
            {
                return _display;
            }
        }

        public string IconAutomationText
        {
            get
            {
                return null;
            }
        }

        ImageMoniker ISuggestedAction.IconMoniker
        {
            get
            {
                return default(ImageMoniker);
            }
        }

        public string InputGestureText
        {
            get
            {
                return null;
            }
        }

        public bool HasActionSets
        {
            get
            {
                return false;
            }
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public bool HasPreview
        {
            get
            {
                return true;
            }
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = String.Format(replacementTexts[language], "$url$", "$name$") });
            return System.Threading.Tasks.Task.FromResult<object>(textBlock);
        }

        public void Dispose()
        {
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            ThreadHelper.JoinableTaskFactory.Run(() =>
            {
                string name, secret;
                var prompt = new Prompt();
                prompt.promptMessage.Text = "Supply the name for the secret!";
                prompt.ShowDialog();
                name = prompt.ResponseText;
                if (name is null || name == string.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                var promptv = new Prompt();
                promptv.promptMessage.Text = "Validate the secret (remove '', etc.)!";
                promptv.ResponseText = value;
                promptv.ShowDialog();
                secret = promptv.ResponseText;
                if (secret is null || secret == string.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                var vaultClient = new Microsoft.Azure.KeyVault.KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback));
                vaultClient.SetSecretAsync(Options.Instance.AKVUrl, name, secret).Wait();
                string aKVUrl = language == "PowerShell" || language == "InBoxPowerShell" ? Options.Instance.AKVShortName : Options.Instance.AKVUrl;
                _span.TextBuffer.Replace(_span.GetSpan(_snapshot), string.Format(replacementTexts[language], aKVUrl, name));
                return System.Threading.Tasks.Task.CompletedTask;

            });
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
