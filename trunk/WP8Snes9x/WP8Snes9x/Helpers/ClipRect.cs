using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace CropControl
{
	 public class ClipRect : INotifyPropertyChanged
	 {
		  private double top = Double.NaN;
		  private double left = Double.NaN;
		  private double bottom = Double.NaN;
		  private double right = Double.NaN;

		  public double ShiftX { get; set; }
		  public double ShiftY { get; set; }

		  public double TopShifted { get { return ShiftY + Top; } }
		  public double LeftShifted { get { return ShiftX + Left; } }
		  public double BottomShifted { get { return ShiftY + Bottom; } }
		  public double RightShifted { get { return ShiftX + Right; } }

		  public bool IsInitialized 
		  {
				get { return !Double.IsNaN(left) && !Double.IsNaN(right) && !Double.IsNaN(top) && !Double.IsNaN(bottom); }
		  }

		  public void Reset()
		  {
				top = Double.NaN;
				left = Double.NaN;
				bottom = Double.NaN;
				right = Double.NaN;
		  }

		  public double Top
		  {
				get 
				{
					 if (Double.IsNaN(top))
					 {
						  return default(double); 
					 }
					 return top; 
				}
				set
				{
					 top = value;
					 // Call OnPropertyChanged whenever the property is updated
					 OnPropertyChanged("Top");
					 OnDependentPropertyCahnged("Rect");
					 OnDependentPropertyCahnged("Center");
					 OnPropertyChanged("TopShifted");
					 OnDependentPropertyCahnged("CenterShifted");
				}
		  }

		  public double Left
		  {
				get 
				{
					 if (Double.IsNaN(left))
					 {
						  return default(double); 
					 }
					 return left; 
				}
				set
				{
					 left = value;
					 // Call OnPropertyChanged whenever the property is updated
					 OnPropertyChanged("Left");
					 OnDependentPropertyCahnged("Rect");
					 OnDependentPropertyCahnged("Center");
					 OnPropertyChanged("LeftShifted");
					 OnDependentPropertyCahnged("CenterShifted");
				}
		  }

		  public double Bottom
		  {
				get 
				{
					 if (Double.IsNaN(bottom))
					 {
						  return default(double); 
					 }
					 return bottom; 
				}
				set
				{
					 bottom = value;
					 // Call OnPropertyChanged whenever the property is updated
					 OnPropertyChanged("Bottom");
					 OnDependentPropertyCahnged("Rect");
					 OnDependentPropertyCahnged("Center");
					 OnPropertyChanged("BottomShifted");
					 OnDependentPropertyCahnged("CenterShifted");
				}
		  }

		  public double Right
		  {
				get
				{
					 if (Double.IsNaN(right))
					 {
						  return default(double);
					 }
					 return right;
				}
				set
				{
					 right = value;
					 // Call OnPropertyChanged whenever the property is updated
					 OnPropertyChanged("Right");
					 OnDependentPropertyCahnged("Rect");
					 OnDependentPropertyCahnged("Center");
					 OnPropertyChanged("RightShifted");
					 OnDependentPropertyCahnged("CenterShifted");
				}
		  }

		  public double Width { get { return Right - Left; } }
		  public double Height { get { return Bottom - Top; } }

		  public Rect Rect { get { return new Rect(Left, Top, Width, Height); } }

		  public Point Center { get { return new Point(Left + Width / 2.0, Top + Height / 2.0); } }

		  public Point CenterShifted { get { return new Point(LeftShifted + Width / 2.0, TopShifted + Height / 2.0); } }

		  #region INotifyPropertyChanged Members

		  public event PropertyChangedEventHandler PropertyChanged;

		  #endregion

		  private void OnDependentPropertyCahnged(string name)
		  {
				if (IsInitialized)
				{
					 OnPropertyChanged(name);
				}
		  }

		  protected void OnPropertyChanged(string name)
		  {
				PropertyChangedEventHandler handler = PropertyChanged;
				if (handler != null)
				{
					 handler(this, new PropertyChangedEventArgs(name));
				}
		  }
	 }
}

