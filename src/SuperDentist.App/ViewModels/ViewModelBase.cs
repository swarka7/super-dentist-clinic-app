using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace SuperDentist.App.ViewModels
{
    public abstract class ViewModelBase : ObservableValidator
    {
        private bool _isBusy;
        private int _suspendValidationCount;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        protected void RunWithoutValidation(Action action)
        {
            _suspendValidationCount++;
            try
            {
                action();
            }
            finally
            {
                _suspendValidationCount--;
            }
        }

        protected void ResetValidation()
        {
            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length == 0)
                {
                    ClearErrors(property.Name);
                }
            }
        }

        protected bool TryParseInt(string? value, int min, int max, out int result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return false;
            }

            if (parsed < min || parsed > max)
            {
                return false;
            }

            result = parsed;
            return true;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (_suspendValidationCount > 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(e.PropertyName))
            {
                return;
            }

            var property = GetType().GetProperty(e.PropertyName);
            if (property == null || property.GetIndexParameters().Length > 0)
            {
                return;
            }

            ValidateProperty(property.GetValue(this), e.PropertyName);
        }
    }
}
