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
using Vault;

namespace Secrecy
{
    internal class PutInHashicorpAction : ISuggestedAction
    {
        private readonly ITrackingSpan _span;
        private readonly string value;
        private readonly string language;
        private readonly ITextSnapshot _snapshot;
        private readonly string _display;
        internal static IReadOnlyDictionary<string, string> replacementTexts = new Dictionary<string, string>
        {
            ["CSharp"] = "(await vaultClient.Secret.Read<Dictionary<String, String>>(\"{0}/{1}\")).Data[\"{2}\"]",
            ["JScript"] = "(await vaultClient.read('{0}/{1}'))['data']['{2}']",
            ["TypeScript"] = "(await vaultClient.read('{0}/{1}'))['data']['{2}']",
            ["Python"] = "vaultClient.secrets.kv.v1.read_secret(mount_point='{0}', path='{1}')['data']['{2}']"
        };

        public PutInHashicorpAction(ITrackingSpan span, string value, string language)
        {
            _span = span;
            this.value = value;
            this.language = language;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            _display = string.Format("Secrecy: Put '{0}' in Hashicorp Vault", span.GetText(_snapshot));
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
            textBlock.Inlines.Add(new Run() { Text = String.Format(replacementTexts[language], "$engine$", "$path$", "$key$") });
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
                string engine, path, key, secret;
                var prompt = new Prompt();
                prompt.promptMessage.Text = "Supply the engine for the secret!";
                prompt.ShowDialog();
                engine = prompt.ResponseText;
                if (engine is null || engine == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                var promptp = new Prompt();
                promptp.promptMessage.Text = "Supply a path for the secret!";
                promptp.ShowDialog();
                path = promptp.ResponseText;
                if (path is null || path == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                var promptk = new Prompt();
                promptk.promptMessage.Text = "Supply a key name for the secret!";
                promptk.ShowDialog();
                key = promptk.ResponseText;
                if (key is null || key == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                var promptv = new Prompt();
                promptv.promptMessage.Text = "Validate the secret (remove '', etc.)!";
                promptv.ResponseText = value;
                promptv.ShowDialog();
                secret = promptv.ResponseText;
                if (secret is null || secret == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                var vaultClient = new VaultClient(new Uri(Options.Instance.HashicorpVaultAddress), Options.Instance.HashicorpVaultToken == "" ? System.Environment.GetEnvironmentVariable("VAULT_TOKEN") : Options.Instance.HashicorpVaultToken);
                _span.TextBuffer.Replace(_span.GetSpan(_snapshot), String.Format(replacementTexts[language], engine, path, key));
                var data = new Dictionary<string, string> { { key, secret } };
                vaultClient.Secret.Write(engine + '/' + path, data, cancellationToken).Wait();
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
