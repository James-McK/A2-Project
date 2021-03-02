﻿using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace A2_Project.UserControls
{
	public class ValidatedTextbox : ValidatedItem
	{
		private TextBox tbx;
		public override string Text
		{
			get
			{
				return tbx.Text;
			}
			set
			{
				tbx.Text = value;
				IsValid = Validation.Validate(Text, Column, out string errorMessage);
				ErrorMessage = errorMessage;
			}
		}

		public ValidatedTextbox(DBObjects.Column column) : base (column)
		{
			tbx = new TextBox()
			{
				MinWidth = 200,
				MaxWidth = 250,
				FontSize = 24,
				Background = null,
				TextWrapping = System.Windows.TextWrapping.Wrap,
				Foreground = new SolidColorBrush(Color.FromRgb(241, 241, 241)),
				CaretBrush = new SolidColorBrush(Color.FromRgb(241, 241, 241))
			};
			AddTextChangedEvent(Tbx_TextChanged);
			if (Column.Constraints.Type == "int" || Column.Name.ToLower().Contains("phone")) tbx.PreviewTextInput += Tbx_OnlyAllowNumbers;
			stpContent.Children.Add(tbx);
			stpContent.Children.Add(img);

			if (Column.Constraints.ForeignKey is not null)
			{
				Button b = new Button()
				{
					Content = "?",
					Width = 20
				};
				b.Click += BtnSelectFKey_Click;
				stpContent.Children.Add(b);
			}
		}

		private void BtnSelectFKey_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ContentWindows.ItemSelectionWindow selWindow = new ContentWindows.ItemSelectionWindow(this);
			selWindow.Show();
			selWindow.SelectItem(tbx.Text);
		}

		public override void SetWidth(double newWidth)
		{
			base.SetWidth(newWidth);
			tbx.Width = newWidth - img.Width - img.Margin.Left;
		}

		public override void SetHeight(double newHeight)
		{
			base.SetHeight(newHeight);
			tbx.Height = newHeight;
		}

		private void Tbx_TextChanged(object sender, TextChangedEventArgs e)
		{
			IsValid = Validation.Validate(Text, Column, out string errorMessage);
			ErrorMessage = errorMessage;
		}

		private void Tbx_OnlyAllowNumbers(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			if (!int.TryParse(e.Text, out _)) e.Handled = true;
		}

		public override void AddTextChangedEvent(TextChangedEventHandler ev)
		{
			tbx.TextChanged += ev;
		}
	}
}
