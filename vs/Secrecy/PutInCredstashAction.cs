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
using Narochno.Credstash;
using Microsoft.VisualStudio.Shell;

namespace Secrecy
{
    internal class PutInCredstashAction : ISuggestedAction
    {
        private readonly ITrackingSpan _span;
        private readonly string value;
        private readonly string language;
        private readonly ITextSnapshot _snapshot;
        private readonly string _display;
        internal static IReadOnlyDictionary<string, string> replacementTexts = new Dictionary<string, string>
        {
            ["CSharp"] = "CredstashBuilder.WithRegion(Amazon.RegionEndpoint.GetBySystemName(\"{2}\")).GetSecretAsync(name: \"{0}\", version: \"{1}\")",
            ["JScript"] = "new Credstash({{ awsOpts: {{ region: '{2}' }} }}).getSecret({{ name: '{0}', version: {1}}})",
            ["TypeScript"] = "new Credstash({{ awsOpts: {{ region: '{2}' }} }}).getSecret({{ name: '{0}', version: {1}}})",
            ["Python"] = "credstash.getSecret(name = '{0}', version = {1})"
        };

        public PutInCredstashAction(ITrackingSpan span, string value, string language)
        {
            _span = span;
            this.value = value;
            this.language = language;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            _display = string.Format("Secrecy: Put '{0}' in Credstash", span.GetText(_snapshot));
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
            textBlock.Inlines.Add(new Run() { Text = String.Format(replacementTexts[language], "$name$", "$version$", "$region$") });
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
                 prompt.promptMessage.Text = "Supply a name for the secret!";
                 prompt.ShowDialog();
                 name = prompt.ResponseText;
                 if (name is null || name == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                 var prompt2 = new Prompt();
                 prompt2.promptMessage.Text = "Validate the secret (remove '', etc.)!";
                 prompt2.ResponseText = value;
                 prompt2.ShowDialog();
                 secret = prompt2.ResponseText;
                 if (secret is null || secret == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
                 var credstashClient = CredstashBuilder.WithRegion(Amazon.RegionEndpoint.GetBySystemName(Options.Instance.CredstashAWSRegion));
                 credstashClient.PutAsync(name, secret).Wait();
                 _span.TextBuffer.Replace(_span.GetSpan(_snapshot), String.Format(replacementTexts[language], name, 1, Options.Instance.CredstashAWSRegion));
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
