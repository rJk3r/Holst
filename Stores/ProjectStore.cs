using Holst.Models;
using System;
using System.Collections.ObjectModel;

namespace Holst.Stores
{
    public class ProjectStore
    {
        public ObservableCollection<BaseProject> Projects { get; } = new ObservableCollection<BaseProject>();

        private BaseProject _currentProject;
        public BaseProject CurrentProject
        {
            get => _currentProject;
            set
            {
                _currentProject = value;
                CurrentProjectChanged?.Invoke();
            }
        }

        public event Action CurrentProjectChanged;
        public event Action ProjectsChanged;

        public void AddProject(BaseProject project)
        {
            if (!Projects.Contains(project))
            {
                Projects.Add(project);
                ProjectsChanged?.Invoke();
            }
        }

        public void RemoveProject(BaseProject project)
        {
            if (Projects.Remove(project))
            {
                ProjectsChanged?.Invoke();
            }
        }
    }
}
