﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using A2_Project.ContentWindows;

namespace A2_Project.UserControls
{
	/// <summary>
	/// Interaction logic for BookingCreator.xaml
	/// </summary>
	public partial class BookingCreator : UserControl
	{
		private object container;

		// TODO: Should be List<string[]>?
		private List<string[]> data;

		public bool IsAdded { get; set; }

		public string BookingID { get; set; }
		public string DogID { get; set; }
		public string StaffID { get; set; }

		public BookingCreator(object _container, string _bookingID, string _dogID, string _staffID)
		{
			InitializeComponent();
			BookingID = Convert.ToInt32(_bookingID).ToString();
			DogID = _dogID;
			StaffID = _staffID;
			container = _container;

			cbxNewBookType.ItemsSource = new string[] { "Appointment", "Recurring Appointment" };
			cbxNewBookType.SelectedIndex = 0;
		}

		private void LblDelete_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (container is CalandarView cal) cal.DeleteBookingPart(this);
			else throw new NotImplementedException();
		}

		private void CbxNewBookType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			((CalandarView)container).RemoveRectsWithTag(this);
			IsAdded = false;
			stpContent.Children.Clear();
			if (cbxNewBookType.SelectedIndex == 0)
			{
				data = new List<string[]>();
				List<string> suggestedValues = new List<string>();
				foreach (DBObjects.Column c in DBObjects.DB.Tables.Where(t => t.Name == "Appointment").First().Columns)
				{
					suggestedValues.Add(UIMethods.GetSuggestedValue(c, ((CalandarView)container).BookingParts).ToString());
				}
				data.Add(suggestedValues.ToArray());
				data[0][1] = DogID;
				data[0][3] = StaffID;
				data[0][4] = BookingID;

				Rectangle r = new Rectangle()
				{
					Fill = new SolidColorBrush(Color.FromRgb(183, 28, 28)),
					Width = 120 / 3, // Day width / count in day
					Height = 80, // Hour height
					Stroke = Brushes.Black,
					StrokeThickness = 1,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Tag = this,
					Name = "r0"
				};
				r.MouseDown += Rct_MouseDown;
				stpContent.Children.Add(r);
			}
			else if (cbxNewBookType.SelectedIndex == 1)
			{
				// TODO: Remove rectangle from cal view

				StackPanel stpTime = new StackPanel()
				{
					Name = "stpTime",
					Orientation = Orientation.Horizontal
				};

				Label lblRepeat = new Label()
				{
					Content = "Repeating every"
				};
				stpTime.Children.Add(lblRepeat);

				TextBox tbxTimePeriod = new TextBox()
				{
					Name = "tbxTimePeriod",
					Margin = new Thickness(10, 0, 10, 0),
					MinWidth = 30,
					MaxWidth = 100,
					FontSize = 24,
					Background = null,
					TextWrapping = TextWrapping.Wrap,
					Foreground = new SolidColorBrush(Color.FromRgb(241, 241, 241)),
					CaretBrush = new SolidColorBrush(Color.FromRgb(241, 241, 241)),
					HorizontalContentAlignment = HorizontalAlignment.Center,
					VerticalContentAlignment = VerticalAlignment.Center,
					Text = "3"
				};
				tbxTimePeriod.PreviewTextInput += Tbx_OnlyAllowNumbers;
				stpTime.Children.Add(tbxTimePeriod);

				ComboBox cbxTimeType = new ComboBox()
				{
					Name = "cbxTimeType",
					ItemsSource = new string[] { "days", "weeks", "months" },
					Width = 100,
					VerticalAlignment = VerticalAlignment.Top,
					LayoutTransform = new ScaleTransform(2, 2)
				};
				stpTime.Children.Add(cbxTimeType);

				TextBox tbxBookCount = new TextBox()
				{
					Name = "tbxBookCount",
					Margin = new Thickness(10, 0, 5, 0),
					MinWidth = 30,
					MaxWidth = 100,
					FontSize = 24,
					Background = null,
					TextWrapping = TextWrapping.Wrap,
					Foreground = new SolidColorBrush(Color.FromRgb(241, 241, 241)),
					CaretBrush = new SolidColorBrush(Color.FromRgb(241, 241, 241)),
					HorizontalContentAlignment = HorizontalAlignment.Center,
					VerticalContentAlignment = VerticalAlignment.Center,
					Text = "4"
				};
				tbxBookCount.PreviewTextInput += Tbx_OnlyAllowNumbers;
				stpTime.Children.Add(tbxBookCount);

				Label lblTimes = new Label()
				{
					Content = "times."
				};
				stpTime.Children.Add(lblTimes);

				stpContent.Children.Add(stpTime);

				StackPanel stpStart = new StackPanel()
				{
					Name = "stpStart",
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 10, 0, 0)
				};

				Label lblStartAt = new Label()
				{
					Content = "Starting at "
				};
				stpStart.Children.Add(lblStartAt);

				ValidatedTextbox tbxStartTime = new ValidatedTextbox(DBObjects.DB.Tables.Where(t => t.Name == "Appointment").First().Columns.Where(c => c.Name == "Appointment Time").First())
				{
					Text = "9:00",
					Width = double.NaN
				};
				stpStart.Children.Add(tbxStartTime);
				tbxStartTime.SetWidth(110);

				CustomizableDatePicker dtpDate = new CustomizableDatePicker()
				{
					LayoutTransform = new ScaleTransform(1.5, 1.5),
					FontSize = 16,
					SelectedDate = ((CalandarView)container).GetSelDate(),
					Margin = new Thickness(10, 0, 0, 0)
				};
				stpStart.Children.Add(dtpDate);

				stpContent.Children.Add(stpStart);

				Button btnUpdate = new Button()
				{
					Content = "Save Changes",
					FontSize = 24,
					HorizontalAlignment = HorizontalAlignment.Left
				};
				btnUpdate.Click += BtnUpdate_Click ;
				stpContent.Children.Add(btnUpdate);

				cbxTimeType.SelectedIndex = 2;
			}
			else throw new NotImplementedException();
		}

		private void BtnUpdate_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				IsAdded = true;
				StackPanel stpTime = stpContent.Children.OfType<StackPanel>().Where(s => s.Name == "stpTime").First();
				TextBox tbxTimePeriod = stpTime.Children.OfType<TextBox>().Where(t => t.Name == "tbxTimePeriod").First();
				TextBox tbxBookCount = stpTime.Children.OfType<TextBox>().Where(t => t.Name == "tbxBookCount").First();
				ComboBox cbxTimeType = stpTime.Children.OfType<ComboBox>().Where(c => c.Name == "cbxTimeType").First();

				StackPanel stpStart = stpContent.Children.OfType<StackPanel>().Where(s => s.Name == "stpStart").First();
				ValidatedTextbox tbxStartTime = stpStart.Children.OfType<ValidatedTextbox>().First();
				CustomizableDatePicker dtpDate = stpStart.Children.OfType<CustomizableDatePicker>().First();

				int count = Convert.ToInt32(tbxBookCount.Text);
				int timeGap = Convert.ToInt32(tbxTimePeriod.Text);

				data = new List<string[]>();

				DateTime start = dtpDate.SelectedDate.Value;
				TimeSpan betweenPeriod;
				if (cbxTimeType.SelectedIndex == 0) betweenPeriod = new TimeSpan(timeGap, 0, 0, 0);
				else if (cbxTimeType.SelectedIndex == 1) betweenPeriod = new TimeSpan(timeGap * 7, 0, 0, 0);
				else if (cbxTimeType.SelectedIndex == 2) betweenPeriod = new TimeSpan(timeGap * 28, 0, 0, 0);
				else throw new NotImplementedException();

				DBObjects.Column[] cols = DBObjects.DB.Tables.Where(t => t.Name == "Appointment").First().Columns;
				for (int i = 0; i < count; i++)
				{
					List<string> suggested = new List<string>();
					for (int j = 0; j < cols.Length; j++)
					{
						suggested.Add(UIMethods.GetSuggestedValue(cols[j], ((CalandarView)container).BookingParts).ToString());
					}
					data.Add(suggested.ToArray());
					data[i][1] = DogID;
					data[i][3] = StaffID;
					data[i][4] = BookingID;

					data[i][9] = start.Add(betweenPeriod * i).ToString("yyyy-MM-dd");
					data[i][10] = TimeSpan.Parse(tbxStartTime.Text).ToString("hh\\:mm");
				}
				
				((CalandarView)container).RepBookingChanged(this);
			}
			catch
			{
			}
		}

		private void Rct_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Rectangle r = (Rectangle)sender;
			r.MouseDown -= Rct_MouseDown;
			Label lblFind = new Label()
			{
				Content = "Go to appointment",
				FontSize = 20
			};
			lblFind.MouseDown += LblFind_MouseDown;
			stpContent.Children.Add(lblFind);
			if (container is CalandarView cal)
			{
				stpContent.Children.Remove(r);
				cal.StartBookAppt(r);
			}
			else throw new NotImplementedException();
		}

		private void LblFind_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (container is CalandarView cal)
			{
				cal.SelectSpecificAppointment(GetData()[Convert.ToInt32(((FrameworkElement)sender).Name.Substring(1))]);
			}
		}

		internal List<string[]> GetData()
		{
			return data;
		}

		internal void SetData(string[] _data, string index)
		{
			data[Convert.ToInt32(index)] = _data;
		}

		private void Tbx_OnlyAllowNumbers(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			if (!int.TryParse(e.Text, out _)) e.Handled = true;
		}
	}
}