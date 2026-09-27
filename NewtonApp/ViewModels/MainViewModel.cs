using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using NewtonApp.Models;
using NewtonApp.Helpers;
using NewtonApp.Database;

namespace NewtonApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseHelper _db = new();

        private double _initialGuess = 1.0;
        private string _result = "";
        private string _iterations = "";
        private string _status = "";
        private bool _isCalculating;

        public ObservableCollection<SolutionRecord> History { get; set; } = new();

        public double InitialGuess
        {
            get => _initialGuess;
            set { _initialGuess = value; OnPropertyChanged(); }
        }

        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        public string Iterations
        {
            get => _iterations;
            set { _iterations = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsCalculating
        {
            get => _isCalculating;
            set
            {
                _isCalculating = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand SolveCommand { get; }
        public ICommand ClearCommand { get; }

        public MainViewModel()
        {
            SolveCommand = new RelayCommand(_ => Solve(), _ => !IsCalculating);
            ClearCommand = new RelayCommand(_ => Clear());

            if (!_db.TestConnection())
            {
                Status = "⚠ Нет подключения к БД";
                return;
            }

            LoadHistory();
            Status = "Готов. Введи начальное приближение.";
        }

        private double Function(double x) => x * x - 2;
        private double Derivative(double x) => 2 * x;

        private (double root, int iterations, string log) NewtonMethod(
            double x0, double tolerance = 1e-10, int maxIterations = 100)
        {
            double x = x0;
            string log = "";

            for (int i = 0; i < maxIterations; i++)
            {
                double fx = Function(x);
                double dfx = Derivative(x);

                if (Math.Abs(dfx) < 1e-12)
                {
                    log += $"Производная близка к нулю на итерации {i + 1}\n";
                    return (x, i + 1, log);
                }

                double xNew = x - fx / dfx;
                log += $"Итерация {i + 1}: x = {x:F8}, f(x) = {fx:F8}\n";

                if (Math.Abs(xNew - x) < tolerance)
                {
                    log += $"\nДостигнута точность на итерации {i + 1}";
                    return (xNew, i + 1, log);
                }

                x = xNew;
            }

            log += $"\nПревышено максимальное число итераций";
            return (x, maxIterations, log);
        }

        private void Solve()
        {
            try
            {
                IsCalculating = true;
                Status = "Вычисление...";

                var (root, iterations, log) = NewtonMethod(InitialGuess);

                Result = $"Корень уравнения x² - 2 = 0: x = {root:F10}";
                Iterations = log;

                var record = new SolutionRecord
                {
                    SolutionTime = DateTime.Now,
                    Equation = "x² - 2 = 0",
                    InitialGuess = InitialGuess,
                    Root = root,
                    IterationsCount = iterations,
                    Error = Math.Abs(root - Math.Sqrt(2))
                };

                if (_db.SaveSolution(record))
                {
                    LoadHistory();
                    Status = $"Решение найдено! Корень = {root:F8}";
                }
                else
                {
                    Status = "Решение найдено, но не сохранено в БД";
                }
            }
            catch (Exception ex)
            {
                Status = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsCalculating = false;
            }
        }

        private void LoadHistory()
        {
            History.Clear();
            foreach (var r in _db.GetAllSolutions())
                History.Add(r);
        }

        private void Clear()
        {
            var result = MessageBox.Show(
                "Удалить всю историю?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _db.ClearAll();
                LoadHistory();
                Status = "История очищена";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}