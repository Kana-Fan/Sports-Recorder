using CommunityToolkit.Mvvm.Input;
using SportsRecorder.Model;
using SportsRecorder.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class WorkoutTemplateViewModel : TrainingRecordsViewModel
    {
        private WorkoutTemplate _selectedTemplate;
        public WorkoutTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (CheckChange())
                {
                    return;
                }
                else
                {
                    _selectedTemplate = value;
                    OnPropertyChanged(nameof(SelectedTemplate));
                    OnPropertyChanged(nameof(IsCardio));
                    OnPropertyChanged(nameof(IsStrength));
                    OnPropertyChanged(nameof(IsDataEmpty));

                    AddPropertyChanged();
                }
            }
        }

        protected override ExerciseType? CurrentType => SelectedTemplate?.Type;
        protected override ObservableCollection<Cardio>? CurrentCardios => SelectedTemplate?.Cardios;
        protected override ObservableCollection<Strength>? CurrentStrengths => SelectedTemplate?.Strengths;

        public ICommand NewTemplateCommand { get; }
        public ICommand RenameTemplateCommand { get; }
        public ICommand DeleteTemplateCommand { get; }
        public ICommand AddEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand SaveCommand { get; }

        public WorkoutTemplateViewModel()
        {
            SelectedTemplate = Templates.Any() ? Templates[0] : new();

            SaveCommand = new RelayCommand(execute: ExecuteSave, canExecute: () => HasChange);

            NewTemplateCommand = new RelayCommand<ExerciseType>(type =>
            {
                if (CheckChange())
                {
                    return;
                }

                var name = _dialogService.Prompt("請輸入範本名稱：");

                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                name = name.Trim();

                // 檢查是否有重複名字
                if (IsNameDuplicated(name, type))
                {
                    return;
                }

                var template = new WorkoutTemplate { Name = name, Type = type };
                Templates.Add(template);
                SelectedTemplate = template;
                UpdateDirtyState();
            });

            RenameTemplateCommand = new RelayCommand(() =>
            {
                if (SelectedTemplate == null)
                {
                    return;
                }

                var name = _dialogService.Prompt("請輸入新名稱：", SelectedTemplate.Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                name = name.Trim();

                // 檢查是否有重複名字
                if (IsNameDuplicated(name, SelectedTemplate.Type, excluding: SelectedTemplate))
                {
                    return;
                }

                SelectedTemplate.Name = name;

                // 強制 ComboBox 重新整理選取框顯示的文字
                var temp = SelectedTemplate;
                SelectedTemplate = null;
                SelectedTemplate = temp;

                UpdateDirtyState();
            });

            DeleteTemplateCommand = new RelayCommand(() =>
            {
                if (SelectedTemplate == null)
                {
                    return;
                }

                if (!_dialogService.Confirm($"確定要刪除範本「{SelectedTemplate.Name}」嗎？"))
                {
                    return;
                }

                HasChange = false;
                Templates.Remove(SelectedTemplate);
                SelectedTemplate = Templates.Any() ? Templates[0] : new();
                HasChange = true;
            });

            AddEntryCommand = new RelayCommand(() =>
            {
                if (SelectedTemplate == null)
                {
                    return;
                }

                if (SelectedTemplate.Type == ExerciseType.Cardio)
                {
                    SelectedTemplate.Cardios.Add(new Cardio());
                }

                else
                {
                    SelectedTemplate.Strengths.Add(new Strength());
                }

                OnPropertyChanged(nameof(IsDataEmpty));
                AddPropertyChanged();
                UpdateDirtyState();
            });

            DeleteEntryCommand = new RelayCommand<ExerciseEntryBase>(entry =>
            {
                if (entry == null || SelectedTemplate == null)
                {
                    return;
                }

                if (!_dialogService.Confirm("確定要刪除這筆項目嗎？"))
                {
                    return;
                }

                if (entry is Cardio c)
                {
                    SelectedTemplate.Cardios.Remove(c);
                }
                else if (entry is Strength s)
                {
                    SelectedTemplate.Strengths.Remove(s);
                }

                OnPropertyChanged(nameof(IsDataEmpty));
                UpdateDirtyState();
            });
        }

        private void ExecuteSave()
        {
            // 只檢查範本內的空項目
            if (!RemoveEmptyEntries(SelectedTemplate.Type, SelectedTemplate.Cardios, SelectedTemplate.Strengths))
            {
                return;
            }

            if (SaveAndNotify(Templates, TemplatesFileName))
            {
                OnPropertyChanged(nameof(IsDataEmpty));
            }
        }

        private void AddPropertyChanged()
        {
            if (SelectedTemplate == null)
            {
                return;
            }

            SubscribeItemsChanged(SelectedTemplate.Type == ExerciseType.Cardio ? SelectedTemplate.Cardios : SelectedTemplate.Strengths);
        }

        protected override void UpdateDirtyState()
        {
            CompareAndSetDirty(Templates, TemplatesFileName);
        }

        private bool CheckChange()
        {
            if (HasChange)
            {
                _dialogService.ShowMessage("當前有未儲存的項目，請先儲存");
                return true;
            }

            return false;
        }
    }
}
