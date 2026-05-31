using Holst.Commands;
using Holst.Models;
using Holst.Services;
using Holst.Stores;
using Microsoft.Win32;
using System;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class ProjectEditViewModel : ViewModelBase
    {
        private readonly NavigationStore _navigationStore;
        private readonly ProjectStore _projectStore;
        private readonly ProjectFileService _fileService;

        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set
            {
                _projectName = value;
                OnPropertyChanged(nameof(ProjectName));
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateAccountCommand { get; }
        public ICommand ImportProjectCommand { get; }
        public ICommand ExportProjectCommand { get; }

        public ProjectEditViewModel(NavigationStore navigationStore, ProjectStore projectStore)
        {
            _navigationStore = navigationStore;
            _projectStore = projectStore;
            _fileService = new ProjectFileService();

            if (_projectStore.CurrentProject != null)
            {
                ProjectName = _projectStore.CurrentProject.Name;
            }

            NavigateHomeCommand = new NavigateCommand<HomeViewModel>(navigationStore, () => new HomeViewModel(navigationStore, projectStore));
            NavigateAccountCommand = new NavigateCommand<AccountViewModel>(navigationStore, () => new AccountViewModel(navigationStore, projectStore));
            ImportProjectCommand = new RelayCommand(OnImport);
            ExportProjectCommand = new RelayCommand(OnExport);
        }

        private async void OnImport()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Holst Project (*.holst)|*.holst",
                Title = "Импорт проекта"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var project = await _fileService.LoadProjectAsync(dlg.FileName);
                    _projectStore.AddProject(project);
                    StatusMessage = $"Импортирован: {project.Name}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка импорта: {ex.Message}";
                }
            }
        }

        private async void OnExport()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Holst Project (*.holst)|*.holst",
                Title = "Экспорт проекта",
                FileName = $"{ProjectName ?? "project"}.holst"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    BaseProject project;
                    if (_projectStore.CurrentProject != null)
                    {
                        project = _projectStore.CurrentProject;
                        project.Name = ProjectName;
                        project.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        project = ProjectFactory.CreateProject(ProjectType.Text, ProjectName, "User");
                    }

                    bool ok = await _fileService.SaveProjectAsync(project, dlg.FileName);
                    if (ok)
                    {
                        if (!_projectStore.Projects.Contains(project))
                            _projectStore.AddProject(project);
                        StatusMessage = $"Экспортирован: {project.Name}";
                    }
                    else
                    {
                        StatusMessage = "Ошибка при сохранении файла.";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка экспорта: {ex.Message}";
                }
            }
        }
    }
}
