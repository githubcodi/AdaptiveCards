﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using XamlCardVisualizer.Helpers;

namespace XamlCardVisualizer.ViewModel
{
    public abstract class GenericDocumentViewModel : BindableBase
    {
        public MainPageViewModel MainPageViewModel { get; private set; }
        public string Token { get; private set; }
        public IStorageFile File { get; private set; }

        private string _name = "Untitled";
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public GenericDocumentViewModel(MainPageViewModel model)
        {
            MainPageViewModel = model;
        }

        private string _payload = "";
        public string Payload
        {
            get { return _payload; }
            set { SetPayload(value); }
        }

        private void SetPayload(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (object.Equals(_payload, value))
            {
                return;
            }

            SetProperty(ref _payload, value);

            Reload();
        }

        public async void Reload()
        {
            if (!IsReloading)
            {
                IsReloading = true;

                await Task.Delay(1000);

                IsReloading = false;
                NotifyPropertyChanged(DelayedUpdatePayload);
                Load();
            }
        }

        private bool _isReloading;
        /// <summary>
        /// Represents whether the system is delaying reloading. Views should indicate this.
        /// </summary>
        public bool IsReloading
        {
            get { return _isReloading; }
            private set { SetProperty(ref _isReloading, value); }
        }

        /// <summary>
        /// This property will be notified of changes on a delayed schedule, so that it's not
        /// changing every single time a character is typed. Views presenting 
        /// </summary>
        public string DelayedUpdatePayload
        {
            get { return Payload; }
        }

        public ObservableCollection<ErrorViewModel> Errors { get; private set; } = new ObservableCollection<ErrorViewModel>();

        protected async Task LoadFromFileAsync(IStorageFile file, string token)
        {
            this.Name = file.Name;
            this.File = file;
            this.Token = token;
            Payload = await FileIO.ReadTextAsync(file);
        }

        public async Task SaveAsync()
        {
            try
            {
                await FileIO.WriteTextAsync(File, Payload);
            }
            catch (Exception ex)
            {
                var dontWait = new MessageDialog(ex.ToString()).ShowAsync();
            }
        }

        private void Load()
        {
            if (string.IsNullOrWhiteSpace(Payload))
            {
                SetSingleError(new ErrorViewModel()
                {
                    Message = "Invalid payload",
                    Type = ErrorViewModelType.Error
                });
                return;
            }

            string payload = Payload;

            try
            {
                LoadPayload(payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                MakeErrorsLike(new List<ErrorViewModel>()
                {
                    new ErrorViewModel()
                    {
                        Message = "Loading failed",
                        Type = ErrorViewModelType.Error
                    }
                });
            }
        }

        protected abstract void LoadPayload(string payload);

        protected void SetSingleError(ErrorViewModel error)
        {
            MakeErrorsLike(new List<ErrorViewModel>() { error });
        }

        protected void MakeErrorsLike(List<ErrorViewModel> errors)
        {
            errors.Sort();
            Errors.MakeListLike(errors);
        }
    }
}
