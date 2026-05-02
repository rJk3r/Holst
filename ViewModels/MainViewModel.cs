using Holst.ViewModels;
using Holst.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace Holst.ViewModels
{
    // Наследуемся от базовой ViewModelBase (Она там и реализует интерфейс INotifyPropertyChanged)
    public class MainViewModel : ViewModelBase
    {
        private readonly NavigationStore _navigationStore;

        public ViewModelBase CurrentViewModel => _navigationStore.CurrentViewModel;

        public MainViewModel(NavigationStore navigationStore)
        {

            _navigationStore = navigationStore;

            _navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
        }

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
        }
    }
}