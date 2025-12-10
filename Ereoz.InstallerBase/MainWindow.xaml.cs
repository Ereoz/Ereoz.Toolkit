using System.Windows;

namespace Ereoz.InstallerBase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetVM(InstallerVM dataContext)
        {
            DataContext = dataContext;

            Closing += ((InstallerVM)DataContext).CancelImpl;
        }
    }
}