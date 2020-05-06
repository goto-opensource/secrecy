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
using System.Linq;

namespace Secrecy
{
    internal class PutInAWSSSM : ISuggestedAction
    {
        private readonly ITrackingSpan _span;
        private readonly string value;
        private readonly string language;
        private readonly ITextSnapshot _snapshot;
        private readonly string _display;
        internal static IReadOnlyDictionary<string, string> replacementTexts = new Dictionary<string, string>
        {
            ["CSharp"] = "(await new AmazonSimpleSystemsManagementClient().GetParameterAsync(new GetParameterRequest() {{ Name = \"{0}\", WithDecryption = true }})).Parameter.Value",
            ["JScript"] = "(await new aws.SSM().getParameter({{ Name: '{0}', WithDecryption: true }}).promise()).Parameter.Value",
            ["TypeScript"] = "(await new aws.SSM().getParameter({{ Name: '{0}', WithDecryption: true }}).promise()).Parameter.Value",
            ["Python"] = "boto3.client('ssm', region_name='{1}').get_parameter(Name = '{0}', WithDecryption = True)['Parameter']['Value']"
        };

        public PutInAWSSSM(ITrackingSpan span, string value, string language)
        {
            _span = span;
            this.value = value;
            this.language = language;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            _display = string.Format("Secrecy: Put '{0}' in AWS SSM", span.GetText(_snapshot));
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
            textBlock.Inlines.Add(new Run() { Text = String.Format(replacementTexts[language], "$name$", "$region$") });
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
                var ssmClient = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementClient(Amazon.RegionEndpoint.GetBySystemName(Options.Instance.AWSKMSRegion));
                string name, secret, keyalias = "";
                var prompt = new Prompt();
                prompt.promptMessage.Text = "Supply a name for the parameter!";
                prompt.ShowDialog();
                name = prompt.ResponseText;
                if (name is null || name == String.Empty) { return System.Threading.Tasks.Task.CompletedTask; }
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
                ssmClient.PutParameterAsync(new Amazon.SimpleSystemsManagement.Model.PutParameterRequest()
                {
                    KeyId = keyalias,
                    Value = secret,
                    Type = "SecureString",
                    Name = name
                }).Wait();
                _span.TextBuffer.Replace(_span.GetSpan(_snapshot), String.Format(replacementTexts[language], name, Options.Instance.AWSSSMRegion));
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
