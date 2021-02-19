﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace A2_Project.ContentWindows
{
	/// <summary>
	/// Interaction logic for ItemSelectionWindow.xaml
	/// </summary>
	public partial class ItemSelectionWindow : Window
	{
		private FilterableDataGrid dtg;
		private object parent;

		public ItemSelectionWindow(UserControls.ValidatedTextbox _parent)
		{
			Initialise(_parent, _parent.Column.Constraints.ForeignKey.ReferencedTable);
		}

		public ItemSelectionWindow(object _parent, string _tableName)
		{
			Initialise(_parent, _tableName);
		}

		private void Initialise(object _parent, string _tableName)
		{
			InitializeComponent();
			parent = _parent;
			dtg = new FilterableDataGrid(_tableName, this);
			dtg.SetMaxHeight(600);
			lblDtg.Content = dtg.Content;

			Height = double.NaN;
			Width = double.NaN;

			Button btnConfirm = new Button()
			{
				Content = "Confirm Selection",
				FontSize = 20,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			btnConfirm.Click += BtnConfirm_Click;
			stp.Children.Add(btnConfirm);
		}

		private void BtnConfirm_Click(object sender, RoutedEventArgs e)
		{
			string[] data = dtg.GetSelectedData();

			if (data is null) Close();

			if (parent is UserControls.ValidatedTextbox tbx)
				tbx.Text = data[0];
			else if (parent is CalandarView calView)
				calView.SelectSpecificAppointment(data);
			else
				throw new NotImplementedException();

			Close();
		}
	}
}