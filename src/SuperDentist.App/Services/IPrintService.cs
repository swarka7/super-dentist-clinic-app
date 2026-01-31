using System.Windows.Media;

namespace SuperDentist.App.Services
{
    public interface IPrintService
    {
        void PrintVisual(Visual visual, string description);
    }
}
