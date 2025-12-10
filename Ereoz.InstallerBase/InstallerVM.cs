using Ereoz.InstallerBase.Core;
using Ereoz.InstallerBase.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ereoz.InstallerBase
{
    public partial class InstallerVM : NotifyPropertyChanged
    {
        #region ViewContent

        public string InstallerName => App.InstallerName;

        public string InstallerVersion { get; private set; }

        public string WindowTitle => $"{InstallParams.InstallAppFullName} - установщик";

        public string WelcomeHello => $"Добро пожаловать в программу установки {InstallParams.InstallAppFullName}";
        public string WelcomeInfo => "";
        public string WelcomeNext => "Нажмите 'Далее' чтобы продолжить.";

        public string UpdateHeader => $"Обновление {InstallParams.InstallAppName}";
        public string UpdateInfo => $"Приложение {InstallParams.InstallAppName} будет обновлено до версии {InstallParams.InstallAppVersion}.";

        public string RepairHeader => $"Восстановление {InstallParams.InstallAppName}";
        public string RepairInfo => $"Приложение {InstallParams.InstallAppFullName} уже установлено.\nЕсли хотите восстановить (переустановить) приложение - нажмите \"Восстановить\".";

        public string ConfigureHeader => "Детали установки";

        private double availableSpace;
        public double AvailableSpace { get => availableSpace; set { availableSpace = value; OnPropertyChanged(); } }

        public string ProgressHeader
        {
            get
            {
                string mode = string.Empty;

                if (InstallParams.InstallMode == InstallMode.Install)
                    mode = "Установка";
                else if (InstallParams.InstallMode == InstallMode.Update)
                    mode = "Обновление";
                else if (InstallParams.InstallMode == InstallMode.Repair)
                    mode = "Восстановление";

                return $"{mode} {InstallParams.InstallAppName}";
            }
        }

        private int progressPercent;
        public int ProgressPercent { get => progressPercent; set { progressPercent = value; OnPropertyChanged(); } }

        private string currentProgressFile;
        public string CurrentProgressFile { get => currentProgressFile; set { currentProgressFile = value; OnPropertyChanged(); } }

        public string InstallModeInfo
        {
            get
            {
                string result = string.Empty;

                if (InstallParams.InstallMode == InstallMode.Install)
                    result = "установки";
                else if (InstallParams.InstallMode == InstallMode.Update)
                    result = "обновления";
                else if (InstallParams.InstallMode == InstallMode.Repair)
                    result = "восстановления";

                return result;
            }
        }

        private int progress;
        public int Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged();

                ProgressPercent = progress * 100 / MaxProgress;
            }
        }
        public int MaxProgress
        {
            get
            {
                int result = InstallParams.FileNames.Count;

                if (InstallParams.IsShortcutDesktop)
                    result++;

                if (InstallParams.IsShortcutStartMenu)
                    result++;

                return result;
            }
        }

        #endregion

        #region Commands

        public ICommand DialogNavigateCommand { get; set; }
        public ICommand SelectInstallDirCommand { get; set; }
        public ICommand InstallCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand DoneCommand { get; set; }

        #endregion

        #region Visibility

        private Visibility lastVisibility;
        private Visibility nextVisibility;
        private Visibility installVisibility;
        private Visibility cancelVisibility;
        private Visibility doneVisibility;
        private Visibility updateVisibility;
        private Visibility repairVisibility;

        public Visibility LastVisibility { get => lastVisibility; set { lastVisibility = value; OnPropertyChanged(); } }
        public Visibility NextVisibility { get => nextVisibility; set { nextVisibility = value; OnPropertyChanged(); } }
        public Visibility InstallVisibility { get => installVisibility; set { installVisibility = value; OnPropertyChanged(); } }
        public Visibility CancelVisibility { get => cancelVisibility; set { cancelVisibility = value; OnPropertyChanged(); } }
        public Visibility DoneVisibility { get => doneVisibility; set { doneVisibility = value; OnPropertyChanged(); } }
        public Visibility UpdateVisibility { get => updateVisibility; set { updateVisibility = value; OnPropertyChanged(); } }
        public Visibility RepairVisibility { get => repairVisibility; set { repairVisibility = value; OnPropertyChanged(); } }

        public Visibility AdminIconVisibility => InstallParams.InstallerRunIsAdmin ? Visibility.Collapsed : Visibility.Visible;

        #endregion

        public InstallParams InstallParams { get; set; }

        public UserControl CurrentDialog
        {
            get => _currentDialog;
            set
            {
                _currentDialog = value;

                if (value is WelcomeDialog)
                {
                    LastVisibility = Visibility.Collapsed;
                    NextVisibility = Visibility.Visible;
                    InstallVisibility = Visibility.Collapsed;
                    UpdateVisibility = Visibility.Collapsed;
                    RepairVisibility = Visibility.Collapsed;
                    CancelVisibility = Visibility.Visible;
                    DoneVisibility = Visibility.Collapsed;
                }
                else if (value is UpdateDialog)
                {
                    LastVisibility = Visibility.Collapsed;
                    NextVisibility = Visibility.Collapsed;
                    InstallVisibility = Visibility.Collapsed;
                    UpdateVisibility = Visibility.Visible;
                    RepairVisibility = Visibility.Collapsed;
                    CancelVisibility = Visibility.Visible;
                    DoneVisibility = Visibility.Collapsed;
                }
                else if (value is RepairDialog)
                {
                    LastVisibility = Visibility.Collapsed;
                    NextVisibility = Visibility.Collapsed;
                    InstallVisibility = Visibility.Collapsed;
                    UpdateVisibility = Visibility.Collapsed;
                    RepairVisibility = Visibility.Visible;
                    CancelVisibility = Visibility.Visible;
                    DoneVisibility = Visibility.Collapsed;
                }
                else if (value is ConfigureDialog)
                {
                    LastVisibility = Visibility.Visible;
                    NextVisibility = Visibility.Collapsed;
                    InstallVisibility = Visibility.Visible;
                    UpdateVisibility = Visibility.Collapsed;
                    RepairVisibility = Visibility.Collapsed;
                    CancelVisibility = Visibility.Visible;
                    DoneVisibility = Visibility.Collapsed;
                }
                else if (value is ProgressDialog)
                {
                    LastVisibility = Visibility.Collapsed;
                    NextVisibility = Visibility.Collapsed;
                    InstallVisibility = Visibility.Collapsed;
                    UpdateVisibility = Visibility.Collapsed;
                    RepairVisibility = Visibility.Collapsed;
                    CancelVisibility = Visibility.Collapsed;
                    DoneVisibility = Visibility.Visible;
                }

                OnPropertyChanged();
            }
        }

        private IteratorCollection<UserControl> _dialogs = new IteratorCollection<UserControl>();
        private UserControl _currentDialog;
        private bool _isDone;
        private bool _isProgress;

        public InstallerVM(InstallParams installParams, Window mainWindow)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            InstallerVersion = $"{version.Major}.{version.Minor}.{version.Revision}";

            InstallParams = installParams;

            DialogNavigateCommand = new RelayCommand(DialogNavigateImpl);
            SelectInstallDirCommand = new RelayCommand(SelectInstallDirImpl, (o) => !App.DisableChangeBaseDirectory);
            InstallCommand = new RelayCommand(InstallImpl, CanInstall);
            CancelCommand = new RelayCommand(CancelImpl);
            DoneCommand = new RelayCommand(DoneImpl, (_) => _isDone);

            switch (InstallParams.InstallMode)
            {
                case InstallMode.Install:
                    {
                        _dialogs.Add(new WelcomeDialog());
                        _dialogs.Add(new ConfigureDialog());
                        _dialogs.Add(new ProgressDialog());
                        break;
                    }
                case InstallMode.Update:
                    {
                        _dialogs.Add(new UpdateDialog());
                        _dialogs.Add(new ProgressDialog());
                        break;
                    }
                case InstallMode.Repair:
                    {
                        _dialogs.Add(new RepairDialog());
                        _dialogs.Add(new ProgressDialog());
                        break;
                    }
            }

            CurrentDialog = _dialogs.Current;

            if (InstallParams.UIMode == UIMode.StartWithConfigure)
                CurrentDialog = _dialogs.Next();

            if (InstallParams.UIMode == UIMode.ProgressOnly)
            {
                CurrentDialog = _dialogs.Next();
                CurrentDialog = _dialogs.Next();

                mainWindow.ContentRendered += (s, e) =>
                {
                    InstallImpl(null);
                };
            }

            InstallParams.OnInstallDirChanged += () =>
            {
                if (string.IsNullOrWhiteSpace(InstallParams.InstallDir))
                    return;

                UpdateAvailableSpace(InstallParams.InstallDir);
            };
            
            if(!string.IsNullOrWhiteSpace(InstallParams.InstallDir))
                UpdateAvailableSpace(InstallParams.InstallDir);
        }

        private void UpdateAvailableSpace(string newDir)
        {
            try
            {
                DriveInfo di = new DriveInfo(Path.GetPathRoot(newDir));
                double free = Math.Round((double)di.AvailableFreeSpace / 1024 / 1024, 2, MidpointRounding.AwayFromZero);
                AvailableSpace = free;
            }
            catch
            {
                AvailableSpace = 0;
            }
        }

        private void DoneImpl(object _)
        {
            if (InstallParams.IsStartAppAfterFinishWork)
            {
                string appPath = string.Empty;

                try
                {
                    appPath = Path.Combine(InstallParams.InstallDir, "bin", $"{InstallParams.InstallAppName}.exe");
                    Process.Start(new ProcessStartInfo { FileName = appPath, WorkingDirectory = Path.Combine(InstallParams.InstallDir, "bin") });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}\n{appPath}");
                }
            }

            Application.Current.Shutdown();
        }

        private void DialogNavigateImpl(object isLastDialogParam)
        {
            if (isLastDialogParam == null)
                CurrentDialog = _dialogs.Next();
            else
                CurrentDialog = _dialogs.Last();
        }

        private void SelectInstallDirImpl(object _)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InstallParams.InstallDir = fbd.SelectedPath;
            }
        }

        private void CancelImpl(object _)
        {
            if (MessageBox.Show($"Вы действительно хотите отменить установку {InstallParams.InstallAppName}?", $"Отмена установки {InstallParams.InstallAppName}", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                App.IsDirectExit = true;
                Application.Current.Shutdown();
            }
        }

        public void CancelImpl(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isDone || App.IsDirectExit)
                return;

            if (_isProgress || MessageBox.Show($"Вы действительно хотите отменить установку {InstallParams.InstallAppName}?", $"Отмена установки {InstallParams.InstallAppName}", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                e.Cancel = true;
        }

        private void InstallImpl(object _)
        {
            _isProgress = true;
            CurrentDialog = _dialogs.Next();

            InstallerService.OnCopiedFileStart += (fileName) =>
            {
                CurrentProgressFile = fileName;
            };

            InstallerService.OnCopiedFileDone += () =>
            {
                Progress++;
            };

            InstallerService.OnAllDone += () =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    _isDone = true;
                    _isProgress = false;
                    CommandManager.InvalidateRequerySuggested();

                    if (InstallParams.UIMode == UIMode.ProgressOnly)
                        DoneImpl(null);
                }));
            };

            InstallerService.OnError += (message) =>
            {
                MessageBox.Show(message, "Ошибка установки", MessageBoxButton.OK, MessageBoxImage.Error);
                App.IsDirectExit = true;
                Environment.Exit(0);
            };

            InstallerService.InstallerWorker(InstallParams);
        }

        private bool CanInstall(object _) =>
            !string.IsNullOrWhiteSpace(InstallParams.InstallDir) &&
            AvailableSpace > InstallParams.RequiredSpace;
    }
}
