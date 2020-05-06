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
using Amazon.KeyManagementService.Model;
using System.Text;
using System.IO;
using System.Linq;

namespace Secrecy
{
    internal class EncryptWithAWSKMS : ISuggestedAction
    {
        private readonly ITrackingSpan _span;
        private readonly string value;
        private readonly string language;
        private readonly ITextSnapshot _snapshot;
        private readonly string _display;
        internal static IReadOnlyDictionary<string, string> replacementTexts = new Dictionary<string, string>
        {
            ["CSharp"] = "Encoding.ASCII.GetString((await new AmazonKeyManagementServiceClient().DecryptAsync(new DecryptRequest(){{CiphertextBlob = new MemoryStream(System.Convert.FromBase64String(\"{0}\"))}})).Plaintext.ToArray())",
            ["JScript"] = "(await new aws.KMS().decrypt({{ CiphertextBlob: Buffer.from('{0}', 'base64') }}).promise()).Plaintext.toString()",
            ["TypeScript"] = "(await new aws.KMS().decrypt({{ CiphertextBlob: Buffer.from('{0}', 'base64'}) }).promise()).Plaintext.toString()",
            ["Python"] = "boto3.client('kms', region_name='{1}').decrypt(CiphertextBlob = base64.b64decode('{0}'))['Plaintext']"
        };

        public EncryptWithAWSKMS(ITrackingSpan span, string value, string language)
        {
            _span = span;
            this.value = value;
            this.language = language;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            _display = string.Format("Secrecy: Encrypt '{0}' with AWS KMS", span.GetText(_snapshot));
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
            textBlock.Inlines.Add(new Run() { Text = String.Format(replacementTexts[language], "$base64$", "$region$") });
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
                string secret, keyalias;
                var list = new Amazon.KeyManagementService.AmazonKeyManagementServiceClient().ListAliases(new ListAliasesRequest() { }).Aliases.Select(e => new { e.AliasName, e.AliasArn });
                var listchooser = new ListChooser(list.Select(e => e.AliasName).ToList());
                listchooser.ShowDialog();
                keyalias = listchooser.ListElement.SelectedItem as string;
                if (keyalias is null || keyalias == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                var promptv = new Prompt();
                promptv.promptMessage.Text = "Validate the secret (remove '', etc.)!";
                promptv.ResponseText = value;
                promptv.ShowDialog();
                secret = promptv.ResponseText;
                if (secret is null || secret == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                var vaultClient = new Amazon.KeyManagementService.AmazonKeyManagementServiceClient(Amazon.RegionEndpoint.GetBySystemName(Options.Instance.AWSKMSRegion));
                var response = vaultClient.Encrypt(new EncryptRequest()
                {
                    KeyId = keyalias,
                    Plaintext = new MemoryStream(Encoding.UTF8.GetBytes(secret))
                });
                _span.TextBuffer.Replace(_span.GetSpan(_snapshot), String.Format(replacementTexts[language], Convert.ToBase64String(response.CiphertextBlob.ToArray()), Options.Instance.AWSKMSRegion));
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
