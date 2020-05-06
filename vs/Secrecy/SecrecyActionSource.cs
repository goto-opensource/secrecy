using EnvDTE;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Secrecy
{
    class SecrecyActionSource : ISuggestedActionsSource
    {
        private readonly SecrecySuggestedActionsSourceProvider _factory;
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;

        public SecrecyActionSource(SecrecySuggestedActionsSourceProvider testSuggestedActionsSourceProvider, ITextView textView, ITextBuffer textBuffer)
        {
            _factory = testSuggestedActionsSourceProvider;
            _textBuffer = textBuffer;
            _textView = textView;
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public void Dispose()
        {
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            string selectedText = range.Snapshot.GetText(_textView.Selection.SelectedSpans[0]);
            if (selectedText.Length > 0)
            {
                ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(_textView.Selection.SelectedSpans[0], SpanTrackingMode.EdgeInclusive);
                string language = _textBuffer.ContentType.TypeName;
                var actions = new List<ISuggestedAction>() { };
                if (Options.Instance.OfferCredstash && PutInCredstashAction.replacementTexts.TryGetValue(language, out var _)) actions.Add(new PutInCredstashAction(trackingSpan, selectedText, language));
                if (Options.Instance.OfferHashicorpVault && PutInHashicorpAction.replacementTexts.TryGetValue(language, out var _)) actions.Add(new PutInHashicorpAction(trackingSpan, selectedText, language));
                if (Options.Instance.OfferAKV && PutInAKV.replacementTexts.TryGetValue(language, out var _)) actions.Add(new PutInAKV(trackingSpan, selectedText, language));
                if (Options.Instance.OfferAWSSSM && EncryptWithAWSKMS.replacementTexts.TryGetValue(language, out var _)) actions.Add(new EncryptWithAWSKMS(trackingSpan, selectedText, language));
                if (Options.Instance.OfferAWSKMS && PutInAWSSSM.replacementTexts.TryGetValue(language, out var _)) actions.Add(new PutInAWSSSM(trackingSpan, selectedText, language));
                return new SuggestedActionSet[] { new SuggestedActionSet(null, actions, "", SuggestedActionSetPriority.None, null) };
            }
            return Enumerable.Empty<SuggestedActionSet>();
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                if (!_textView.Selection.IsEmpty)
                {
                    return true;
                }
                return false;
            });
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            ITextCaret caret = _textView.Caret;
            SnapshotPoint point;

            if (caret.Position.BufferPosition > 0)
            {
                point = caret.Position.BufferPosition - 1;
            }
            else
            {
                wordExtent = default(TextExtent);
                return false;
            }

            ITextStructureNavigator navigator = _factory.NavigatorService.GetTextStructureNavigator(_textBuffer);

            wordExtent = navigator.GetExtentOfWord(point);
            return true;
        }
    }
}
