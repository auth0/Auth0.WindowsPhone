using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;

namespace SampleApp.Phone81
{
    internal static class MessageHelpers
    {
        public static IAsyncOperation<IUICommand> ShowDialogAsync(string text)
        {
            return new MessageDialog(text).ShowAsync();
        }

        public static IAsyncOperation<IUICommand> ShowOKCancelAsync(string text, string title = "")
        {
            var dialog = new MessageDialog(text, title);
            dialog.Commands.Add(new UICommand("OK"));
            dialog.Commands.Add(new UICommand("Cancel"));
            return dialog.ShowAsync();
        }
    }
}
