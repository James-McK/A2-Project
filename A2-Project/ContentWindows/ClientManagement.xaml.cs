﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace A2_Project.ContentWindows
{
	/// <summary>
	/// Interaction logic for ClientManagement.xaml
	/// </summary>
	public partial class ClientManagement : Window
	{
		// Used to prevent an infinite loop whenever the user changes their selection
		private bool shouldUpdate = true;

		// Variables for managing contacts
		private readonly FilterableDataGrid dtgContacts;
		private readonly DBObjects.Column[] contactsColumns;
		private DataEditingSidebar contactEditing;
		private DataEditingSidebar contactAdding;

		// Variables for managing dogs
		private readonly FilterableDataGrid dtgDogs;
		private readonly DBObjects.Column[] dogsColumns;
		private DataEditingSidebar dogsEditing;
		private DataEditingSidebar dogsAdding;

		// Variables for managing clients
		private readonly FilterableDataGrid dtgClients;
		private readonly DBObjects.Column[] clientsColumns;
		private DataEditingSidebar clientsEditing;
		private DataEditingSidebar clientsAdding;

		private const string contactString = "Contacts";
		private const string clientString = "Clients";
		private const string dogString = "Dogs";

		// Used to represent which table the user is currently editing
		// If I were to redo this now, I would use an enum instead.
		private string mode = "";

		public ClientManagement()
		{
			InitializeComponent();

			// Initialise each table and set them to all be equal sizes until the user picks one to edit

			contactsColumns = DBMethods.MetaRequests.GetColumnDataFromTable("Contact");
			dtgContacts = new FilterableDataGrid(contactsColumns, this);
			dtgContacts.SetMaxHeight(300);
			dtgContacts.HideCount();
			lblContacts.Content = dtgContacts.Content;

			dogsColumns = DBMethods.MetaRequests.GetColumnDataFromTable("Dog");
			dtgDogs = new FilterableDataGrid(dogsColumns, this);
			dtgDogs.SetMaxHeight(300);
			dtgDogs.HideCount();
			lblDogs.Content = dtgDogs.Content;

			clientsColumns = DBMethods.MetaRequests.GetColumnDataFromTable("Client");
			dtgClients = new FilterableDataGrid(clientsColumns, this);
			dtgClients.SetMaxHeight(300);
			dtgClients.HideCount();
			lblClients.Content = dtgClients.Content;
		}

		/// <summary>
		/// Called whenever the user saves their changes in an editing sidebar
		/// </summary>
		internal void UpdateFromSidebar(string[] data, bool isNew)
		{
			// Update the correct table with the new data
			switch (mode)
			{
				case contactString: dtgContacts.UpdateData(data, isNew); break;
				case dogString: dtgDogs.UpdateData(data, isNew); break;
				case clientString: dtgClients.UpdateData(data, isNew); break;
			}

			// Bring the user back to editing
			if (isNew)
			{
				string prevMode = mode;
				UpdateMode("");
				UpdateMode(prevMode);
			}
		}

		/// <summary>
		/// Called by an editing sidebar whenever the user wants to delete an item
		/// </summary>
		internal void DeleteItem(bool deleteRef = false)
		{
			try
			{
				switch (mode)
				{
					case contactString: dtgContacts.TryDeleteSelected(deleteRef); contactEditing.EmptySidebar(); break;
					case dogString: dtgDogs.TryDeleteSelected(deleteRef); dogsEditing.EmptySidebar(); break;
					case clientString: dtgClients.TryDeleteSelected(deleteRef); clientsEditing.EmptySidebar(); break;
				}
			}
			// An exception is thrown if there are other items which reference the item to be deleted
			catch (Exception ex)
			{
				// Allow the user to make the choice to delete the item and everything which references it (And everything that references the references, and so on)
				// Or cancel the deletion
				grdFKeyErrorOuter.Visibility = Visibility.Visible;
				tblFKeyRefError.Text = ex.Message;
			}
		}

		/// <summary>
		/// Called by one of the tables whenever an item it contains is clicked
		/// </summary>
		public void TableSelectionChanged(FilterableDataGrid sender, string[] newData)
		{
			if (!shouldUpdate) return;
			shouldUpdate = false;
			if (sender == dtgContacts)
			{
				UpdateMode(contactString);
			}
			else if (sender == dtgDogs)
			{
				UpdateMode(dogString);
			}
			else if (sender == dtgClients)
			{
				UpdateMode(clientString);
			}
			UpdateEditingSidebar(newData);
			shouldUpdate = true;
		}

		/// <summary>
		/// Move the user to editing whichever table they have selected
		/// </summary>
		private void UpdateMode(string newMode)
		{
			// Whenever a new item in the same table is selected:
			if (newMode == mode)
			{
				// Display the contacts/dogs/clients related to the selected item
				if (mode == contactString)
				{
					string[] selectedData = dtgContacts.GetSelectedData();
					if (selectedData is null) return;
					if (selectedData[0] != "No Results!")
					{
						dtgDogs.ChangeSearch(1, selectedData[1]);
						dtgClients.ChangeSearch(0, selectedData[1]);
					}
				}
				else if (mode == dogString)
				{
					string[] selectedData = dtgDogs.GetSelectedData();
					if (selectedData is null) return;
					if (selectedData[0] != "No Results!")
					{
						dtgContacts.ChangeSearch(1, selectedData[1]);
						dtgClients.ChangeSearch(0, selectedData[1]);
					}
				}
				else if (mode == clientString)
				{
					string[] selectedData = dtgClients.GetSelectedData();
					if (selectedData is null) return;
					if (selectedData[0] != "No Results!")
					{
						dtgContacts.ChangeSearch(1, selectedData[0]);
						dtgDogs.ChangeSearch(1, selectedData[0]);
					}
				}
			}
			else
			{
				mode = newMode;

				if (mode == "") return;

				double notSelMax = 200;
				double selMax = 650;

				lblEditBtn.Content = $"Editing {mode}";
				lblStartAddingBtn.Content = $"Add New {mode}";

				// Display the selected data
				// Show related items in the other tables
				// Resize the tables

				if (mode == contactString)
				{
					string[] selectedData = dtgContacts.GetSelectedData();
					if (selectedData[0] == "No Results!")
					{ return; }
					dtgContacts.ClearSearch();
					if (selectedData is null) return;
					dtgDogs.ChangeSearch(1, selectedData[1]);
					dtgClients.ChangeSearch(0, selectedData[1]);
					dtgContacts.SetMaxHeight(selMax);
				}
				else if (mode == dogString)
				{
					string[] selectedData = dtgDogs.GetSelectedData();
					if (selectedData[0] == "No Results!")
					{ return; }
					dtgDogs.ClearSearch();
					if (selectedData is null) return;
					dtgContacts.ChangeSearch(1, selectedData[1]);
					dtgClients.ChangeSearch(0, selectedData[1]);
					dtgDogs.SetMaxHeight(selMax);
				}
				else if (mode == clientString)
				{
					string[] selectedData = dtgClients.GetSelectedData();
					if (selectedData[0] == "No Results!")
					{ return; }
					dtgClients.ClearSearch();
					if (selectedData is null) return;
					dtgContacts.ChangeSearch(1, selectedData[0]);
					dtgDogs.ChangeSearch(1, selectedData[0]);
					dtgClients.SetMaxHeight(selMax);
				}

				if (mode != contactString) dtgContacts.SetMaxHeight(notSelMax);
				if (mode != dogString) dtgDogs.SetMaxHeight(notSelMax);
				if (mode != clientString) dtgClients.SetMaxHeight(double.NaN);
			}
		}

		private void UpdateEditingSidebar(string[] newData)
		{
			if (mode == contactString)
			{
				if (contactEditing is null) contactEditing = new DataEditingSidebar(contactsColumns, "Contact", this);
				if (contactAdding is null) contactAdding = new DataEditingSidebar(contactsColumns, "Contact", this);
				contactEditing.ChangeSelectedData(newData);
				contactAdding.StartAddNew(GetClientID());
				dtgDogs.UpdateSelectedIndex(-1);
				dtgClients.UpdateSelectedIndex(-1);
			}
			else if (mode == dogString)
			{
				if (dogsEditing is null) dogsEditing = new DataEditingSidebar(dogsColumns, "Dog", this);
				if (dogsAdding is null) dogsAdding = new DataEditingSidebar(dogsColumns, "Dog", this);
				dogsEditing.ChangeSelectedData(newData);
				dogsAdding.StartAddNew(GetClientID());
				dtgContacts.UpdateSelectedIndex(-1);
				dtgClients.UpdateSelectedIndex(-1);
			}
			else if (mode == clientString)
			{
				if (clientsEditing is null) clientsEditing = new DataEditingSidebar(clientsColumns, "Client", this);
				if (clientsAdding is null) clientsAdding = new DataEditingSidebar(clientsColumns, "Client", this);
				clientsEditing.ChangeSelectedData(newData);
				clientsAdding.StartAddNew(GetClientID());
				dtgDogs.UpdateSelectedIndex(-1);
				dtgContacts.UpdateSelectedIndex(-1);
			}

			UpdateClientEditor();
		}

		private void UpdateClientEditor()
		{
			if (mode == contactString)
			{
				lblEditing.Content = contactEditing.Content;
				lblAdding.Content = contactAdding.Content;
			}
			else if (mode == dogString)
			{
				lblEditing.Content = dogsEditing.Content;
				lblAdding.Content = dogsAdding.Content;
			}
			else if (mode == clientString)
			{
				lblEditing.Content = clientsEditing.Content;
				lblAdding.Content = clientsAdding.Content;
			}
		}

		/// <summary>
		/// The user wants to delete the item and anything else that references it
		/// </summary>
		private void BtnFkeyErrorAccept_Click(object sender, RoutedEventArgs e)
		{
			DeleteItem(true);
			grdFKeyErrorOuter.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// The user does not want to delete the selected item
		/// </summary>
		private void BtnFkeyErrorDecline_Click(object sender, RoutedEventArgs e)
		{
			grdFKeyErrorOuter.Visibility = Visibility.Collapsed;
		}

		private string GetClientID()
		{
			return dtgClients.GetClientID();
		}

		private void LblEditBtn_MouseDown(object sender, MouseButtonEventArgs e)
		{
			SelectLbl(lblEditBtn);
			DeselectLbl(lblStartAddingBtn);

			lblEditing.Visibility = Visibility.Visible;
			lblAdding.Visibility = Visibility.Collapsed;
		}

		private void LblStartAddBtn_MouseDown(object sender, MouseButtonEventArgs e)
		{
			SelectLbl(lblStartAddingBtn);
			DeselectLbl(lblEditBtn);

			lblEditing.Visibility = Visibility.Collapsed;
			lblAdding.Visibility = Visibility.Visible;
		}

		private static void SelectLbl(Label l)
		{
			l.Width = 420;
			l.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
			l.Foreground = new SolidColorBrush(Color.FromRgb(241, 241, 241));
		}

		private static void DeselectLbl(Label l)
		{
			l.Width = 280;
			l.Background = new SolidColorBrush(Color.FromRgb(37, 37, 37));
			l.Foreground = new SolidColorBrush(Color.FromRgb(213, 213, 213));
		}

		internal void AddToEdit()
		{
			LblEditBtn_MouseDown(null, null);
		}
	}
}
