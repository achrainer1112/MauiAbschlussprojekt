using MauiAbschlussprojekt.ViewModels;
using Microsoft.Maui.Graphics;

namespace MauiAbschlussprojekt.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();

            if (Shell.Current is AppShell appShell)
                appShell.UpdateFlyoutHeader();

            UpdateCircle();

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.ProgressPercentage))
                    UpdateCircle();
            };
        }

        private void UpdateCircle()
        {
            WaterCircle.Drawable = new WaterArcDrawable(_viewModel.ProgressPercentage);
            WaterCircle.Invalidate();
        }
    }

    public class WaterArcDrawable : IDrawable
    {
        private readonly double _progress;

        public WaterArcDrawable(double progress)
        {
            _progress = Math.Max(progress, 0.0);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float cx = dirtyRect.Width / 2f;
            float cy = dirtyRect.Height / 2f;
            float strokeWidth = 14f;
            float radius = Math.Min(cx, cy) - strokeWidth / 2f;

            float left = cx - radius;
            float top = cy - radius;
            float size = radius * 2f;

            canvas.StrokeColor = Color.FromArgb("#E8EDF2");
            canvas.StrokeSize = strokeWidth;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawCircle(cx, cy, radius);

            if (_progress >= 1.0)
            {
                canvas.StrokeColor = Color.FromArgb("#1A73E8");
                canvas.StrokeSize = strokeWidth;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawCircle(cx, cy, radius);
                return;
            }

            float filledAngle = (float)(_progress * 360.0);
            float emptyAngle = 360f - filledAngle;

            canvas.StrokeColor = Color.FromArgb("#1A73E8");
            canvas.StrokeSize = strokeWidth;
            canvas.StrokeLineCap = LineCap.Round;

            canvas.DrawArc(left, top, size, size,
                           90f - filledAngle,
                           90f - filledAngle - emptyAngle,
                           false,
                           false);
        }
    }
}
