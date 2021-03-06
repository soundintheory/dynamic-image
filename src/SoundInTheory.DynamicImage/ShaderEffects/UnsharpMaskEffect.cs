using System.Windows;
using System.Windows.Media;

namespace SoundInTheory.DynamicImage.ShaderEffects
{
	internal class UnsharpMaskEffect : ShaderEffectBase<UnsharpMaskEffect>
	{
		public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(UnsharpMaskEffect), 0);

		public Brush Input
		{
			get { return (Brush)GetValue(InputProperty); }
			set { SetValue(InputProperty, value); }
		}

		public static readonly DependencyProperty BlurMaskProperty = RegisterPixelShaderSamplerProperty("BlurMask", typeof(UnsharpMaskEffect), 1);

		public Brush BlurMask
		{
			get { return (Brush)GetValue(BlurMaskProperty); }
			set { SetValue(BlurMaskProperty, value); }
		}

		public static readonly DependencyProperty AmountProperty = DependencyProperty.Register("Amount", typeof(double), typeof(UnsharpMaskEffect), new UIPropertyMetadata(0.5, PixelShaderConstantCallback(0)));

		public double Amount
		{
			get { return (double)GetValue(AmountProperty); }
			set { SetValue(AmountProperty, value); }
		}

		public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register("Threshold", typeof(double), typeof(UnsharpMaskEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(1)));

		public double Threshold
		{
			get { return (double)GetValue(ThresholdProperty); }
			set { SetValue(ThresholdProperty, value); }
		}

		public UnsharpMaskEffect()
		{
			UpdateShaderValue(InputProperty);
			UpdateShaderValue(BlurMaskProperty);
			UpdateShaderValue(AmountProperty);
			UpdateShaderValue(ThresholdProperty);
		}
	}
}