using System.Windows.Controls;
using System.Windows.Media;

namespace SuperDentist.App.Services
{
    public sealed class PrintService : IPrintService
    {
        public void PrintVisual(Visual visual, string description)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                dialog.PrintVisual(visual, description);
            }
        }
    }
}
