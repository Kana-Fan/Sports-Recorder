using System.Windows;
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
            TextAnimation();
        }

        private void TextAnimation()
        {
            Storyboard perChar = new Storyboard();
            perChar.RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(10));

            _text.TextEffects = new TextEffectCollection();

            for (int i = 0; i < _text.Text.Length; i++)
            {
                TextEffect effect = new TextEffect();
                effect.Transform = new TranslateTransform();
                effect.PositionStart = i;
                effect.PositionCount = 1;
                _text.TextEffects.Add(effect);

                DoubleAnimation anim = new DoubleAnimation();
                anim.From = 0;
                anim.To = -15;
                anim.AutoReverse = true;
                anim.AccelerationRatio = .2;
                anim.DecelerationRatio = .2;
                anim.RepeatBehavior = new RepeatBehavior(1);
                anim.Duration = TimeSpan.FromSeconds(0.5);
                anim.BeginTime = TimeSpan.FromMilliseconds(100 * i); // 打字節奏,字與字間隔小

                Storyboard.SetTargetProperty(anim, new PropertyPath("TextEffects[" + i + "].Transform.Y"));
                Storyboard.SetTargetName(anim, _text.Name);

                perChar.Children.Add(anim);
            }

            perChar.Begin(this);
        }
    }
}
