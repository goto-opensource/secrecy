using Microsoft.VisualStudio.Shell;

namespace Secrecy
{
    /// <summary>
    /// A provider for custom <see cref="DialogPage" /> implementations.
    /// </summary>
    internal class DialogPageProvider
    {
        public class General : BaseOptionPage<Options> { }
    }

}
