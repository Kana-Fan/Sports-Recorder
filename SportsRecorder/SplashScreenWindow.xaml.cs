using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SportsRecorder
{
    /// <summary>
    /// SplashScreenWindow.xaml 的互動邏輯
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            PlayPerCharAnimation(_text);
        }

        private void PlayPerCharAnimation(
            TextBlock target,
            double amplitude = -15,
            double charDuration = 0.5,
            double staggerMs = 100,
            double totalRepeatSeconds = 10,
            double accelRatio = 0.2,
            double decelRatio = 0.2)
        {
            Storyboard perChar = new Storyboard();
            perChar.RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(totalRepeatSeconds));

            target.TextEffects = new TextEffectCollection();

            for (int i = 0; i < target.Text.Length; i++)
            {
                TextEffect effect = new TextEffect
                {
                    Transform = new TranslateTransform(),
                    PositionStart = i,
                    PositionCount = 1
                };
                target.TextEffects.Add(effect);

                DoubleAnimation anim = new DoubleAnimation
                {
                    From = 0,
                    To = amplitude,
                    AutoReverse = true,
                    AccelerationRatio = accelRatio,
                    DecelerationRatio = decelRatio,
                    RepeatBehavior = new RepeatBehavior(1),
                    Duration = TimeSpan.FromSeconds(charDuration),
                    BeginTime = TimeSpan.FromMilliseconds(staggerMs * i)
                };

                Storyboard.SetTargetProperty(anim, new PropertyPath($"TextEffects[{i}].Transform.Y"));
                Storyboard.SetTargetName(anim, target.Name);
                perChar.Children.Add(anim);
            }

            perChar.Begin(this);
        }
    }
}
