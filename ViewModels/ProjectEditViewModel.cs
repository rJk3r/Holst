using Holst.Commands;
using Holst.Models;
using Holst.Services;
using Holst.Stores;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Linq;

namespace Holst.ViewModels
{
    public class ProjectEditViewModel : ViewModelBase
    {
        private readonly NavigationStore _navigationStore;
        private readonly ProjectStore _projectStore;
        private readonly AccountStore _accountStore;
        private readonly IDatabaseService _databaseService;
        private readonly ProjectFileService _fileService;

        public string ProjectTitle => _projectStore.CurrentProject?.Name ?? "Новый проект";

        public TextProject CurrentTextProject => _projectStore.CurrentProject as TextProject;

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
        public ICommand SaveProjectCommand { get; }
        public ICommand ImportProjectCommand { get; }
        public ICommand ExportProjectCommand { get; }

        public ProjectEditViewModel(NavigationStore navigationStore, ProjectStore projectStore, AccountStore accountStore, IDatabaseService databaseService)
        {
            _navigationStore = navigationStore;
            _projectStore = projectStore;
            _accountStore = accountStore;
            _databaseService = databaseService;
            _fileService = new ProjectFileService();
            _statusMessage = string.Empty;

            NavigateHomeCommand = new NavigateCommand<HomeViewModel>(navigationStore, () => new HomeViewModel(navigationStore, projectStore, accountStore, databaseService));
            NavigateAccountCommand = new NavigateCommand<AccountViewModel>(navigationStore, () => new AccountViewModel(navigationStore, projectStore, accountStore, databaseService));
            SaveProjectCommand = new RelayCommand<FlowDocument>(OnSave);
            ImportProjectCommand = new RelayCommand(OnImport);
            ExportProjectCommand = new RelayCommand(OnExport);
        }

        public void UpdateDocumentBlocks(List<DocumentBlock> blocks)
        {
            if (_projectStore.CurrentProject is TextProject tp)
            {
                tp.Blocks = blocks;
                tp.UpdatedAt = DateTime.UtcNow;
            }
        }

        private async void OnSave(FlowDocument document)
        {
            var project = _projectStore.CurrentProject;
            if (project == null)
            {
                StatusMessage = "Нет активного проекта для сохранения.";
                return;
            }

            // parse blocks from provided FlowDocument and update project's content
            try
            {
                var blocks = ParseDocumentBlocks(document);
                UpdateDocumentBlocks(blocks);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при подготовке документа: {ex.Message}";
                return;
            }

            if (string.IsNullOrEmpty(project.FilePath))
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");
                Directory.CreateDirectory(dir);
                project.FilePath = Path.Combine(dir, $"{project.Name}_{project.Id:N}.holst");
            }

            var path = project.FilePath;
            if (!path.EndsWith(".holst", StringComparison.OrdinalIgnoreCase))
                path = Path.ChangeExtension(path, ".holst");

            project.UpdatedAt = DateTime.UtcNow;

            bool ok = await _fileService.SaveProjectAsync(project, path);
            if (ok)
            {
                StatusMessage = $"Сохранено: {Path.GetFileName(path)}";
            }
            else
            {
                StatusMessage = "Ошибка при сохранении файла.";
            }
        }

        private List<DocumentBlock> ParseDocumentBlocks(FlowDocument document)
        {
            var blocks = new List<DocumentBlock>();

            foreach (var block in document.Blocks.OfType<Paragraph>())
            {
                var textRange = new TextRange(block.ContentStart, block.ContentEnd);
                string text = textRange.Text;
                string trimmed = text.TrimStart();
                string trimmedFull = text.Trim();

                if (string.IsNullOrWhiteSpace(text))
                {
                    blocks.Add(new ParagraphBlock { Text = text });
                    continue;
                }

                if (trimmed.StartsWith("```"))
                {
                    blocks.Add(new CodeBlock { Text = trimmedFull });
                    continue;
                }

                if (trimmed.StartsWith("---") || trimmed.StartsWith("***") || trimmed.StartsWith("___"))
                {
                    blocks.Add(new ParagraphBlock { Text = trimmedFull });
                    continue;
                }

                if (trimmed.StartsWith("> "))
                {
                    blocks.Add(new ParagraphBlock { Text = trimmedFull });
                    continue;
                }

                if (trimmed.StartsWith("### "))
                {
                    blocks.Add(new HeaderBlock { Level = 3, Text = trimmedFull.Substring(4).TrimEnd('\r', '\n') });
                    continue;
                }

                if (trimmed.StartsWith("## "))
                {
                    blocks.Add(new HeaderBlock { Level = 2, Text = trimmedFull.Substring(3).TrimEnd('\r', '\n') });
                    continue;
                }

                if (trimmed.StartsWith("# "))
                {
                    blocks.Add(new HeaderBlock { Level = 1, Text = trimmedFull.Substring(2).TrimEnd('\r', '\n') });
                    continue;
                }

                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ ") || Regex.IsMatch(trimmed, @"^\d+\.\s"))
                {
                    blocks.Add(new ParagraphBlock { Text = trimmedFull });
                    continue;
                }

                blocks.Add(new ParagraphBlock { Text = text });
            }

            return blocks;
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
                    project.FilePath = dlg.FileName;
                    _projectStore.AddProject(project);
                    _projectStore.CurrentProject = project;
                    OnPropertyChanged(nameof(ProjectTitle));
                    OnPropertyChanged(nameof(CurrentTextProject));
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
            var project = _projectStore.CurrentProject;
            if (project == null)
            {
                StatusMessage = "Нет активного проекта для экспорта.";
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Holst Project (*.holst)|*.holst",
                Title = "Экспорт проекта",
                FileName = $"{project.Name ?? "project"}.holst"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    project.UpdatedAt = DateTime.UtcNow;
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
