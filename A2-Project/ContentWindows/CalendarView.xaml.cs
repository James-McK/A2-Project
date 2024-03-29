﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using A2_Project.UserControls;

namespace A2_Project.ContentWindows
{
	/// <summary>
	/// Interaction logic for Calander.xaml
	/// </summary>
	public partial class CalandarView : Window
	{
		// The hour of the day work starts at (e.g. 9 being 9:00)
		private const double dayStartTime = 8;
		private const double dayEndTime = 19;

		// How much extra space there should be between days (e.g. 1.3 being 30% the width of 1 day)
		private const double spaceBetweenDays = 1.3;
		// The number of rooms available (Assuming room IDs start at 0 and count up by 1)
		private int appRoomCount = 3;
		// The width 1 day should have
		private const int dayWidth = 120;
		// The height 1 hour should have
		private const int hourHeight = 80;

		private const int baseYOffset = 30;

		// The colours used for colouring appointments based on the applied filter
		private readonly Color[] colours;

		// The difference between the position of the mouse and the currently selected element
		private Point diffMouseAndElem;

		// Is the mouse currently held down?
		private bool mouseDown = false;

		// The currently selected element to be moved with the mouse
		private FrameworkElement currentlySelected;

		private bool toExit = false;

		// The date picker used for selecting a week to display
		private readonly DatePicker datePicker;
		// The name of the appointment table
		private readonly string tableName = "Appointment";
		// The columns of the appointment table
		private readonly DBObjects.Column[] columns;

		// The sidebar used for editing the selected appointment
		private readonly DataEditingSidebar editingSidebar;

		// The text of the appointment key labels
		private string[] keyLabels;
		// The index of the key to next be added
		private int keyLoadedCount = 0;
		// The ComboBox used for selecting which colour filter will be used
		private ComboBox cmbKey;

		// Used to store if the user has just moved to the previous/next week
		private bool hasMoved = false;

		private readonly List<FrameworkElement> labelElements = new List<FrameworkElement>();

		private ItemSelectionWindow selSpecificAppWindow;

		private string[] dataToBeSelected = null;

		public List<BookingCreator> BookingParts { get; set; }

		private bool updateOnDateChanged = true;

		public CalandarView()
		{
			BookingParts = new List<BookingCreator>();
			// Get the number of rooms
			appRoomCount = Convert.ToInt32(DBMethods.MiscRequests.GetMinKeyNotUsed("Grooming Room", "Grooming Room ID"));
			// For now, filtering by staff is default

			keyLabels = DBMethods.MetaRequests.GetAllFromTable("Staff").Select(x => x[1]).ToArray();
			// Get the column data for the appointment table
			columns = DBMethods.MetaRequests.GetColumnDataFromTable(tableName);

			// Get the colours for filters
			colours = new Color[]
			{
				Color.FromRgb(183, 28, 28), // Red
				Color.FromRgb(2, 119, 189), // Blue
				Color.FromRgb(190, 96, 0), // Dark orange? Not quite brown
				Color.FromRgb(46, 125, 50), // Green
				Color.FromRgb(49, 27, 146) // Deep Purple
			};

			InitializeComponent();

			stpBookDogID.Children.Add(UIMethods.GenAppropriateElement(columns[1], out _, false, true));
			ComboBox cmbBkStaff = (ComboBox)UIMethods.GenAppropriateElement(columns[3], out _, false, true);
			cmbBkStaff.LayoutTransform = new ScaleTransform(2, 2);
			cmbBkStaff.SelectionChanged += CmbBkStaff_SelectionChanged;
			stpBookStaffID.Children.Add(cmbBkStaff);

			// Start the thread for moving the selected element to the mouse when needed
			Thread loopThread = new Thread(Loop)
			{ IsBackground = true };
			loopThread.Start();

			// Create a DatePicker used for selecting a date to display
			datePicker = new DatePicker()
			{
				Margin = new Thickness(544, 10, 0, 0),
				Width = 200 / 1.5,
				FontSize = 16,
				RenderTransform = new ScaleTransform(1.5, 1.5),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			grd.Children.Add(datePicker);
			datePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
			datePicker.SelectedDate = DateTime.Today;

			Label lblPrevWeek = new Label()
			{
				Content = "<",
				FontSize = 30,
				FontWeight = FontWeights.Bold,
				Margin = new Thickness(500, 0, 0, 0)
			};
			lblPrevWeek.MouseDown += LblPrevWeek_MouseDown;
			grd.Children.Add(lblPrevWeek);
			Label lblNextWeek = new Label()
			{
				Content = ">",
				FontSize = 30,
				FontWeight = FontWeights.Bold,
				Margin = new Thickness(760, 0, 0, 0)
			};
			lblNextWeek.MouseDown += LblNextWeek_MouseDown;
			grd.Children.Add(lblNextWeek);

			// Instantiate the editing sidebar
			editingSidebar = new DataEditingSidebar(columns, tableName, this, false);
			lblEditingSidebar.Content = editingSidebar.Content;

			grdFindAppt.MouseEnter += GrdFindAppt_MouseEnter;
			grdFindAppt.MouseLeave += GrdFindAppt_MouseLeave;
			grdFindAppt.MouseDown += GrdFindAppt_MouseDown;

			string bookingID = DBMethods.MiscRequests.GetMinKeyNotUsed("Booking", "Booking ID");
			lblNewBookingID.Content = $"Booking ID: {bookingID}";
			BookingParts.Add(new BookingCreator(this, bookingID, GetBookingDogID(), GetBookingStaffID()));
			stpBookingManager.Children.Add(BookingParts[0]);

			// Start the process of adding the key for the appointment view
			AddKey();
		}

		private void GrdFindAppt_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (selSpecificAppWindow is null) selSpecificAppWindow = new ItemSelectionWindow(this, "Appointment");
			try
			{
				selSpecificAppWindow.Show();
			}
			catch
			{
				selSpecificAppWindow = new ItemSelectionWindow(this, "Appointment");
				selSpecificAppWindow.Show();
			}
		}

		private void GrdFindAppt_MouseLeave(object sender, MouseEventArgs e)
		{
			Grid g = (Grid)sender;
			Rectangle r = g.Children.OfType<Rectangle>().First();
			r.Fill = new SolidColorBrush(Color.FromRgb(213, 213, 213));
		}

		private void GrdFindAppt_MouseEnter(object sender, MouseEventArgs e)
		{
			Grid g = (Grid)sender;
			Rectangle r = g.Children.OfType<Rectangle>().First();
			r.Fill = new SolidColorBrush(Color.FromRgb(241, 241, 241));
		}

		/// <summary>
		/// Move to the date of and select, if it exists, the appointment with the given data.
		/// </summary>
		public void SelectSpecificAppointment(string[] appData)
		{
			if (appData is null) return;
			SelectNewRect(grdResults.Children.OfType<Rectangle>().Where(r => r.Tag is not null && GetDataTag(r)[0] == appData[0]).FirstOrDefault());
			dataToBeSelected = appData;
			DateTime d = DateTime.Parse(appData[9]);
			datePicker.SelectedDate = d;
			dataToBeSelected = null;
		}


		/// <summary>
		/// Display the next week
		/// </summary>
		private void LblNextWeek_MouseDown(object sender, MouseButtonEventArgs e)
		{
			datePicker.SelectedDate = ((DateTime)datePicker.SelectedDate).AddDays(7);
		}

		/// <summary>
		/// Display the previous week
		/// </summary>
		private void LblPrevWeek_MouseDown(object sender, MouseButtonEventArgs e)
		{
			datePicker.SelectedDate = ((DateTime)datePicker.SelectedDate).AddDays(-7);
		}

		private void AddKey(FrameworkElement frPrev = null)
		{
			// This method was written before I was aware of stack panels.

			// The base X offset
			int baseX = 0;
			// The base Y offset
			int baseY = 5;
			int gapBetweenKeys = 25;

			// The first element to be added is the checkbox that allows other filters to be selected
			if (keyLoadedCount == 0)
			{
				// Get the selected index of the previous ComboBox, if it exists
				int selIndex;
				if (cmbKey != null) selIndex = cmbKey.SelectedIndex;
				else selIndex = 0;

				Label lblColourBy = new Label()
				{
					Margin = new Thickness(baseX, baseY - 12, 0, 0),
					Content = "Colour By:"
				};
				grdKey.Children.Add(lblColourBy);

				// Creates the new ComboBox
				cmbKey = new ComboBox()
				{
					Margin = new Thickness(baseX + 130, baseY - 5, 0, 0),
					Width = 240,
					FontSize = 20,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					ItemsSource = new string[] { "Staff Member", "Appointment Type", "Includes Nails And Teeth", "Paid" },
					SelectedIndex = selIndex
				};
				cmbKey.SelectionChanged += CmbKeyOptions_SelectionChanged;
				// Allow the next key to be added
				cmbKey.Loaded += LblLoaded_LoadNextKey;

				grdKey.Children.Add(cmbKey);
				keyLoadedCount++;
				return;
			}

			Rectangle r = new Rectangle
			{
				Width = 20,
				Height = 20,
				Margin = new Thickness(baseX + (keyLoadedCount - 1) * 120, baseY, 0, 0),
				Fill = new SolidColorBrush(colours[keyLoadedCount - 1]),
				Stroke = Brushes.Black,
				StrokeThickness = 1,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			grdKey.Children.Add(r);

			Label l = new Label()
			{
				Content = keyLabels[keyLoadedCount - 1],
				Margin = new Thickness(baseX + r.Width + (keyLoadedCount - 1) * 120, baseY - 12, 0, 0)
			};

			// Allow the current key to be placed relative to the previous one
			if (!(frPrev is null))
			{
				r.Margin = new Thickness(frPrev.Margin.Left + frPrev.ActualWidth + gapBetweenKeys, r.Margin.Top, 0, 0);
				l.Margin = new Thickness(frPrev.Margin.Left + frPrev.ActualWidth + gapBetweenKeys + r.Width, l.Margin.Top, 0, 0);
			}

			if (keyLoadedCount == keyLabels.Length)
			{
				// If this is the final element to be created, reset the keyCount so it can be used in the future, and exit.
				keyLoadedCount = 0;
				grdKey.Children.Add(l);
			}
			else
			{
				// Otherwise, when the next label is loaded, this method must be called again
				l.Loaded += LblLoaded_LoadNextKey;
				grdKey.Children.Add(l);
				keyLoadedCount++;
			}
		}

		/// <summary>
		/// Once this label has been loaded, try to add the next key.
		/// </summary>
		private void LblLoaded_LoadNextKey(object sender, RoutedEventArgs e)
		{
			FrameworkElement fr = (FrameworkElement)sender;
			fr.Loaded -= LblLoaded_LoadNextKey;
			AddKey(fr);
		}

		/// <summary>
		/// When the selected filter is changed, update the displayed keys and the colours of the appointment rectangles
		/// </summary>
		private void CmbKeyOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cmb = (ComboBox)sender;
			switch (cmb.SelectedItem.ToString())
			{
				case "Staff Member":
					keyLabels = DBMethods.MetaRequests.GetAllFromTable("Staff").Select(x => x[1]).ToArray();
					break;
				case "Paid":
					keyLabels = new string[] { "Not Paid", "Paid" };
					break;
				case "Appointment Type":
					keyLabels = DBMethods.MetaRequests.GetAllFromTable("Appointment Type").Select(x => x[3]).ToArray();
					break;
				case "Includes Nails And Teeth":
					keyLabels = new string[] { "Does Not Include Nails And Teeth", "Includes Nails And Teeth" };
					break;
			}
			grdKey.Children.Clear();
			AddKey();

			// Recolour every rectangle according to the new key
			IEnumerable<Rectangle> rects = grdResults.Children.OfType<Rectangle>();
			foreach (Rectangle r in rects)
			{
				string[] data = GetDataTag(r);
				if (data is null) continue;
				r.Fill = GetColourForRect(data);
			}
		}

		/// <summary>
		/// Get the colour a rectangle should have based on its data and the current filter
		/// </summary>
		private SolidColorBrush GetColourForRect(string[] rectData)
		{
			string cmbCase;
			if (cmbKey is null) cmbCase = "Staff Member";
			else cmbCase = cmbKey.SelectedItem.ToString();
			return cmbCase switch
			{
				"Staff Member" => new SolidColorBrush(colours[Convert.ToInt32(rectData[3])]),
				"Paid" => new SolidColorBrush(colours[Convert.ToInt32(Convert.ToBoolean(rectData[8]))]),
				"Appointment Type" => new SolidColorBrush(colours[Convert.ToInt32(rectData[2])]),
				"Includes Nails And Teeth" => new SolidColorBrush(colours[Convert.ToInt32(Convert.ToBoolean(rectData[6]))]),
				_ => throw new NotImplementedException(),
			};
		}

		/// <summary>
		/// A looping method to move the currently selected rectangle to the mouse when needed.
		/// </summary>
		private void Loop()
		{
			while (!toExit)
			{
				Dispatcher.Invoke(() => {
					// Prevent a MouseUp event from somehow being missed
					if (mouseDown && Mouse.LeftButton == MouseButtonState.Released)
					{
						UpdateAfterMouseUp((Rectangle)currentlySelected);
					}

					if (mouseDown && currentlySelected is FrameworkElement elem && elem.Parent is FrameworkElement parent)
					{
						Point mousePos = Mouse.GetPosition(parent);


						bool shouldSnapToGrid = false;
						object tag = elem.Tag;
						if (tag is BookingCreator booking)
						{
							// If the appointment has not yet been dragged onto grdResults, it should not snap to a grid
							shouldSnapToGrid = parent == grdResults;
						}
						else shouldSnapToGrid = true;


						if (shouldSnapToGrid)
						{
							// Get the position of the rectangle snapped to a grid
							SnapMarginToGrid(elem, mousePos, out Thickness newMargin, out int roomID, out int dDiff);

							// Allow the selected week to be changed if the currently selected element is moved to either side of the grid
							if (mousePos.X > dayWidth * spaceBetweenDays * 6 + dayWidth && !hasMoved)
							{
								datePicker.SelectedDate = datePicker.SelectedDate.Value.AddDays(7);
								hasMoved = true;
							}
							else if (mousePos.X < 0 && !hasMoved)
							{
								datePicker.SelectedDate = datePicker.SelectedDate.Value.AddDays(-7);
								hasMoved = true;
							}
							else if (mousePos.X > 0 && mousePos.X < dayWidth * spaceBetweenDays * 6 + dayWidth) hasMoved = false;

							// If the element should be moved
							// (No need to move it if the new position is the same as the old one)
							if (newMargin != elem.Margin)
							{
								// See if the new position would clash with other appointments

								DateTime day = GetStartOfWeek().AddDays(dDiff);
								TimeSpan appStart = TimeSpan.FromHours(dayStartTime - 1 + newMargin.Top / hourHeight);

								string[] data = GetDataTag(elem);

								bool doesClash = DBMethods.MiscRequests.DoesAppointmentClash(data, roomID, day, appStart, BookingParts, out string errMessage);
								editingSidebar.DisplayError(errMessage);

								int appLength = DBMethods.MiscRequests.GetAppLength(data, BookingParts);

								// If it does clash, spend a bit of time to see if the appointment should be moved up/down past the item it clashes with.

								if (doesClash)
								{
									TimeSpan oldStart = TimeSpan.FromHours(dayStartTime - 1 + elem.Margin.Top / hourHeight);

									if (!DBMethods.MiscRequests.DoesAppointmentClash(data, roomID, day, oldStart, BookingParts, out _))
									{
										elem.Margin = new Thickness(dDiff * dayWidth * spaceBetweenDays + roomID * dayWidth / appRoomCount, elem.Margin.Top, 0, 0);
									}

									int direction = (int)((newMargin.Top - elem.Margin.Top) / Math.Abs(newMargin.Top - elem.Margin.Top));
									int count = 0;

									while (true)
									{
										count += direction;
										TimeSpan toCheck = oldStart.Add(TimeSpan.FromMinutes(count * 15));
										if (toCheck.TotalHours > 18 || toCheck.TotalHours < 9) break;
										bool doesNewClash = DBMethods.MiscRequests.DoesAppointmentClash(data, roomID, day, toCheck, BookingParts, out _);
										if (!doesNewClash)
										{
											TimeSpan halfway = oldStart.Add(TimeSpan.FromMinutes((count * 15 + appLength) / 2));

											TimeSpan mouseTime = TimeSpan.FromMinutes((dayStartTime - 1 + mousePos.Y / hourHeight) * 60);
											bool shouldUpdate = false;
											if (direction == -1 && mouseTime < halfway) shouldUpdate = true;
											else if (direction == 1 && mouseTime > halfway) shouldUpdate = true;

											if (shouldUpdate)
											{
												newMargin.Top = (toCheck.TotalHours - dayStartTime + 1) * hourHeight;
												elem.Margin = newMargin;
											}
											break;
										}
									}

								}
								else elem.Margin = newMargin;
							}
						}
						else
						{
							// If it is not being snapped to a grid, just move it to the centre of the mouse.
							elem.Margin = new Thickness(mousePos.X - elem.Width / 2, mousePos.Y - elem.Height / 2, 0, 0);
						}

					}
				});
				Thread.Sleep(10);
			}
		}


		/// <summary>
		/// Cancel all appointments that are part of the same booking as the given appointment
		/// </summary>
		internal void CancelBooking(string[] appData)
		{
			if (currentlySelected is null) return;

			if (currentlySelected.Tag is BookingCreator)
			{
				// If the appointment has not yet been confirmed, cancel all appointments that have not yet been confirmed, as they are all part of the same booking
				BookingCreator[] bookings = BookingParts.ToArray();
				foreach (BookingCreator b in bookings)
				{
					DeleteBookingPart(b);
				}
				BookingParts = new List<BookingCreator>();
			}
			else
			{
				string bookingID = appData[4];

				// Get the data for all other appointments in that booking
				string[][] appts = DBMethods.MiscRequests.GetByColumnData("Appointment", "Booking ID", bookingID).Select(x => x.ToArray()).ToArray();
				foreach (string[] app in appts)
				{
					// Hide all cancelled appointments
					Rectangle rect = grdResults.Children.OfType<Rectangle>().Where(r => r.Tag is string[] appData && appData[0] == app[0]).FirstOrDefault();
					if (rect is not null)
					{
						grdResults.Children.Remove(rect);
					}
					// Mark all appointments that are part of the same booking as cancelled
					DBMethods.MiscRequests.UpdateColumn("Appointment", "True", "Cancelled", "Appointment ID", app[0]);
				}
			}
		}

		private void SnapMarginToGrid(FrameworkElement elem, Point mousePos, out Thickness newMargin, out int roomID, out int dDiff)
		{
			Point diff = (Point)(mousePos - diffMouseAndElem);

			int ySnap = hourHeight / 4;

			// Get the middle of the selected element
			double midLeft = diff.X + elem.Width / 2;
			double midTop = diff.Y + ySnap / 2;

			// Gets the difference in days between the start of the week and the day the selected appointment should be on
			dDiff = (int)(midLeft / (dayWidth * spaceBetweenDays));
			// Gets the x offset that can be used to calculate which room/day the appointment should be placed into.

			double roomIDOffset = (midLeft % (dayWidth * spaceBetweenDays)) / (dayWidth / appRoomCount);

			// Place the appointment in the correct room/day whenever the user tries to place it into the gap between the days
			if (roomIDOffset > appRoomCount && roomIDOffset < appRoomCount + 0.5)
			{
				roomID = appRoomCount - 1;
			}
			else if (roomIDOffset > appRoomCount + 0.5)
			{
				roomID = 0;
				dDiff++;
			}
			else
			{
				roomID = (int)roomIDOffset;
			}

			if (roomID < 0) roomID = 0;
			if (dDiff > 6) dDiff = 6;

			// 'Snap' the appointment to a grid to represent where it should be
			newMargin = new Thickness(dDiff * dayWidth * spaceBetweenDays + roomID * dayWidth / appRoomCount, midTop - midTop % ySnap, 0, 0);

			newMargin.Left = Math.Max(0, newMargin.Left);

			newMargin.Top = Math.Max(hourHeight, newMargin.Top);
			newMargin.Top = Math.Min((dayEndTime + 1 - dayStartTime) * hourHeight - elem.Height, newMargin.Top);
		}

		/// <summary>
		/// Get the appointment data related to the given item as a string[]
		/// </summary>
		private static string[] GetDataTag(FrameworkElement r)
		{
			if (r is null) return null;
			else if (r.Tag is string[] strArr) return strArr;
			else if (r.Tag is BookingCreator booking)
			{
				if (r.Name.Length < 2)
					throw new NotImplementedException();
				return booking.GetData()[Convert.ToInt32(r.Name[1..])];
			}
			else if (r.Tag is null) return null;
			else throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a duplicate of the rectangle clicked on to be moved around.
		/// </summary>
		private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
		{
			mouseDown = true;
			SelectNewRect((Rectangle)sender);
		}

		/// <summary>
		/// Select the given rectangle and deselect the old one.
		/// </summary>
		private void SelectNewRect(Rectangle newSel)
		{
			if (newSel is null) return;

			// Reset the stroke around the previously selected element
			if (currentlySelected is Rectangle rct)
			{
				rct.Stroke = Brushes.Black;
				rct.StrokeThickness = 1;

				bool doesClash = DBMethods.MiscRequests.DoesAppointmentClash(GetDataTag(rct), BookingParts, out _);

				string[] prevSel = editingSidebar.GetData();
				editingSidebar.ChangeSelectedData(GetDataTag(rct));
				bool isDataValid = editingSidebar.IsValid(out _);
				editingSidebar.ChangeSelectedData(prevSel);

				if (doesClash || !isDataValid)
				{
					rct.StrokeThickness = 4;
					rct.Stroke = Brushes.White;
				}
			}

			// Update which element is currently selected
			currentlySelected = newSel;

			if (currentlySelected is FrameworkElement element && element.Parent is FrameworkElement parent)
			{
				diffMouseAndElem = (Point)(Mouse.GetPosition(parent) - new Point(element.Margin.Left, element.Margin.Top));
				if (element is Rectangle rect)
				{
					// Create a new rectangle with the same properties as the old one
					Rectangle newRect = new Rectangle
					{
						Width = rect.Width,
						Height = rect.Height,
						Margin = rect.Margin,
						Fill = rect.Fill,
						Stroke = Brushes.AliceBlue, // A different stroke help marks out which appointment is currently selected
						StrokeThickness = 2,
						Tag = rect.Tag,
						VerticalAlignment = rect.VerticalAlignment,
						HorizontalAlignment = rect.HorizontalAlignment,
						Name = rect.Name
					};

					// The new rectangle should be displayed over the hour line markings for now
					Panel.SetZIndex(newRect, 1);
					editingSidebar.ChangeSelectedData(GetDataTag(rect));

					newRect.MouseDown += Rectangle_MouseDown;
					newRect.MouseUp += RctRect_MouseUp;
					currentlySelected = newRect;
					((Grid)parent).Children.Add(newRect);
				}

				if (element.Tag == null || element.Tag.ToString() != "Duplicate")
				{
					((Grid)parent).Children.Remove(element);
				}
			}
		}

		/// <summary>
		/// Tries to place the rectangle in an appropriate grid spot one the mouse is lifted.
		/// </summary>
		private void RctRect_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (sender != currentlySelected) return;

			if (sender is Rectangle r)
			{
				UpdateAfterMouseUp(r);
			}
		}

		/// <summary>
		/// Save the user's changes afther a MouseUp event.
		/// </summary>
		private void UpdateAfterMouseUp(Rectangle r)
		{
			mouseDown = false;
			if (r.Parent is Panel p && p != grdResults && r.Tag is BookingCreator bk)
			{
				p.Children.Remove(r);
				r.MouseDown -= Rectangle_MouseDown;
				r.MouseUp -= RctRect_MouseUp;
				r.IsHitTestVisible = true;
				bk.AddRectBack(r);
				hasMoved = false;
				grdResults.MouseEnter -= GrdResults_MouseEnter;
				currentlySelected = null;
				return;
			}

			SwitchToEditing();
			editingSidebar.DisplayError("");
			Panel.SetZIndex(r, 0);

			if (r.Margin.Top % (hourHeight / 4) != 0)
			{
				SnapMarginToGrid(r, Mouse.GetPosition((UIElement)r.Parent), out Thickness newMargin, out _, out _);
				r.Margin = newMargin;
			}

			// Get the middle of the selected element
			double midLeft = r.Margin.Left + r.Width / 2;

			// Gets the difference in days between the start of the week and the day the selected appointment should be on
			int dDiff = (int)(midLeft / (dayWidth * spaceBetweenDays));
			// Gets the x offset that can be used to calculate which room/day the appointment should be placed into.
			int roomID = (int)(midLeft % (dayWidth * spaceBetweenDays) / (dayWidth / appRoomCount));

			string idColumnName = "Appointment ID";
			DateTime startOfWeek = GetStartOfWeek();
			DateTime appDate = startOfWeek.AddDays(dDiff);
			// Save the user's changes to the database
			string appID = GetDataTag(r)[0];

			TimeSpan t = TimeSpan.FromHours(dayStartTime - 1 + r.Margin.Top / hourHeight);

			if (r.Tag is string[])
			{
				DBMethods.MiscRequests.UpdateColumn(tableName, appDate.ToString("yyyy-MM-dd"), "Appointment Date", idColumnName, appID);
				DBMethods.MiscRequests.UpdateColumn(tableName, t.ToString("hh\\:mm"), "Appointment Time", idColumnName, appID);
				DBMethods.MiscRequests.UpdateColumn(tableName, roomID.ToString(), "Grooming Room ID", idColumnName, appID);

				string[] newData = DBMethods.MiscRequests.GetByColumnData(tableName, idColumnName, appID, columns.Select(x => x.Name).ToArray())[0].ToArray();
				r.Tag = newData;
				editingSidebar.ChangeSelectedData(newData);
			}
			else if (r.Tag is BookingCreator booking)
			{
				string[] data = GetDataTag(r);
				data[5] = roomID.ToString();
				data[9] = appDate.ToString("yyyy-MM-dd");
				data[10] = t.ToString("hh\\:mm");
				booking.SetData(data, r.Name[1..]);
				editingSidebar.ChangeSelectedData(data);
			}
			else throw new NotImplementedException();

			Rectangle[] toRemove = grdResults.Children.OfType<Rectangle>().Where(rct => rct.Tag is not null && GetDataTag(rct).SequenceEqual(GetDataTag(r))).ToArray();
			foreach (Rectangle rct in toRemove) grdResults.Children.Remove(rct);

			Rectangle newRect = GenRectFromData(GetDataTag(r), r.Name, true);
			if (r.Tag is BookingCreator bkPart) newRect.Tag = bkPart;
		}

		/// <summary>
		/// Ensures that the looping method exits whenever the application closes.
		/// </summary>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			toExit = true;
		}

		private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!updateOnDateChanged) return;

			updateOnDateChanged = false;
			datePicker.SelectedDate = GetStartOfWeek();
			updateOnDateChanged = true;

			// Clear the previous results
			grdResults.Children.Clear();

			foreach (FrameworkElement fr in labelElements)
			{
				grd.Children.Remove(fr);
			}
			labelElements.Clear();

			// If something is currently selected and the mouse is down, the selected element should be carried over to the next week
			if (currentlySelected is not null && mouseDown)
			{
				// It should probably check for a valid position before placing it
				grdResults.Children.Add(currentlySelected);
			}

			// Add labels to show hour of day
			for (double i = dayStartTime; i < dayStartTime + 12; i += 1)
			{
				Label lblTime = new Label()
				{
					Content = TimeSpan.FromHours(i).ToString(@"hh\:mm") + "   ",
					IsHitTestVisible = false,
					HorizontalAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(0, (i - dayStartTime + 1) * hourHeight - 24 + baseYOffset, grdResults.Width + grdResults.Margin.Right, 0)
				};

				// Horizontal translucent line to mark hour
				Rectangle hourLine = new Rectangle()
				{
					Height = 2,
					Width = dayWidth * 7 * spaceBetweenDays * 0.98,
					Margin = new Thickness(0, (i - dayStartTime + 1) * hourHeight + baseYOffset - 1, grdResults.Margin.Right + 4, 0),
					Fill = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
					Opacity = 0.15,
					IsHitTestVisible = false,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Right
				};

				Panel.SetZIndex(lblTime, 1);
				Panel.SetZIndex(hourLine, 1);
				grd.Children.Add(lblTime);
				grd.Children.Add(hourLine);
				labelElements.Add(lblTime);
				labelElements.Add(hourLine);
			}


			int days = 7;
			for (int i = 0; i < days; i++)
			{
				// Add a background to mark out each day
				Rectangle dayBackground = new Rectangle
				{
					Width = dayWidth + 4,
					Height = hourHeight * 11,
					Margin = new Thickness(i * dayWidth * spaceBetweenDays - 2, hourHeight, 0, 0),
					Fill = new SolidColorBrush(Color.FromRgb(11, 11, 11)),
					StrokeThickness = 0,
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left
				};
				Panel.SetZIndex(dayBackground, -1);
				grdResults.Children.Add(dayBackground);

				DateTime startOfWeek = GetStartOfWeek();
				DateTime currentDay = startOfWeek.AddDays(i);

				// Label each day of the week
				Label lblDayOfWeek = new Label()
				{
					Content = currentDay.DayOfWeek,
					Margin = new Thickness(i * dayWidth * spaceBetweenDays - 10, hourHeight - 40, 0, 0),
					Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
					Width = dayWidth + 20,
					HorizontalContentAlignment = HorizontalAlignment.Center
				};
				grdResults.Children.Add(lblDayOfWeek);

				// Generate all the rectangles to represent the appointments on that day
				List<List<string>> results = DBMethods.MiscRequests.GetAllAppointmentsOnDay(currentDay, columns.Select(x => x.Name).ToArray());
				foreach (List<string> ls in results)
				{
					GenRectFromData(ls.ToArray());
				}
			}

			AddRects();
		}

		/// <summary>
		/// Fill grdResults with the relevant appointmnets for the selected week
		/// </summary>
		private void AddRects()
		{
			DateTime startOfWeek = DateTime.Now.Date;
			// Note: The following Dispatcher.Invoke is probably no longer needed, however this comment was written after the final deadline.
			Dispatcher.Invoke(() => startOfWeek = GetStartOfWeek());
			DateTime endOfWeek = startOfWeek.AddDays(7);

			foreach (BookingCreator booking in BookingParts)
			{
				List<string[]> bkData = booking.GetData();
				for (int i = 0; i < bkData.Count; i++)
				{
					string[] bk = bkData[i];
					DateTime bkDate = DateTime.Parse(bk[9]).Date;
					if (bkDate >= startOfWeek && bkDate <= endOfWeek)
					{
						Rectangle r = GenRectFromData(bk, "r" + i.ToString(), true);
						if (r is not null) r.Tag = booking;
					}
				}
			}
		}

		/// <summary>
		/// Gets the date that is at the start of the selected week
		/// </summary>
		private DateTime GetStartOfWeek()
		{
			DateTime picked = (DateTime)datePicker.SelectedDate;
			return picked.AddDays(-DayOfWeekToInt(picked.DayOfWeek));
		}

		/// <summary>
		/// Update the selected data from the rectangle
		/// </summary>
		public bool UpdateFromSidebar(string[] data, bool isNew)
		{
			bool doesClash = DBMethods.MiscRequests.DoesAppointmentClash(data, BookingParts, out string errMessage);
			editingSidebar.DisplayError(errMessage);

			if (doesClash) return false;

			string[] prevData = GetDataTag(currentlySelected);

			if (prevData is null)
			{
				editingSidebar.DisplayError("Error: This appointment does not exist!");
				return false;
			}

			if ((prevData[2] == "3" && data[2] != "3") || (prevData[2] != "3" && data[2] == "3"))
			{
				editingSidebar.DisplayError("Error: You cannot change an appointment\nto/from allergy therapy!");
				return false;
			}

			if (data[2] == "3" && data[6] == "True")
			{
				editingSidebar.DisplayError("Error: An allergy appointment cannot\ninclude this option!");
				return false;
			}

			if (currentlySelected.Tag is string[])
			{
				if (!isNew)
				{
					Rectangle r = grdResults.Children.OfType<Rectangle>().Where(r => !(r.Tag is null) && GetDataTag(r)[0] == data[0]).FirstOrDefault();
					grdResults.Children.Remove(r);
				}
				GenRectFromData(data, null, true);
				DBMethods.DBAccess.UpdateTable(tableName, columns.Select(x => x.Name).ToArray(), data, isNew);
			}
			else if (currentlySelected.Tag is BookingCreator booking)
			{
				booking.SetData(data, currentlySelected.Name[1..]);
				Rectangle r = grdResults.Children.OfType<Rectangle>().Where(r => !(r.Tag is null) && r.Tag == booking && GetDataTag(r)[0] == data[0]).FirstOrDefault();
				grdResults.Children.Remove(r);
				Rectangle rNew = GenRectFromData(data, currentlySelected.Name, true);
				rNew.Tag = booking;
				currentlySelected = rNew;
			}
			else throw new NotImplementedException();

			return true;
		}

		/// <summary>
		/// Cancels the currently selected appointment. Returns true if the cancellation was successful
		/// </summary>
		internal bool CancelApp()
		{
			if (currentlySelected is null) return false;
			if (currentlySelected.Tag is BookingCreator booking)
			{
				DeleteBookingPart(booking);
				BookingParts.Remove(booking);
			}
			else
			{
				string[] data = GetDataTag(currentlySelected);

				if (data[2] == "3")
				{
					editingSidebar.DisplayError("Error: You cannot only cancel part of an\nallergy appointment.");
					return false;
				}
				else
				{
					DBMethods.MiscRequests.UpdateColumn("Appointment", "True", "Cancelled", "Appointment ID", data[0]);
					grdResults.Children.Remove(currentlySelected);
				}
			}
			currentlySelected = null;
			return true;
		}

		/// <summary>
		/// Generate a rectangle from the passed in data
		/// </summary>
		private Rectangle GenRectFromData(string[] data, string name = null, bool isExpensive = false)
		{
			if (data is null) return null;
			// Do not generate rectangles for cancelled appointments
			if (data[7] == "True") return null;
			if (data[9] == "" || data[10] == "") return null;


			int roomID = Convert.ToInt32(data[5]);
			int typeID = Convert.ToInt32(data[2]);
			DateTime d = DateTime.Parse(data[9]).Add(TimeSpan.Parse(data[10]));
			int dDiff = (d.Date - (DateTime)datePicker.SelectedDate).Days;
			dDiff += DayOfWeekToInt(((DateTime)datePicker.SelectedDate).DayOfWeek);

			// appLength is in minutes
			double appLength = DBMethods.MiscRequests.GetAppLength(data, BookingParts);

			Rectangle newRect = new Rectangle
			{
				Width = dayWidth / appRoomCount,
				Height = appLength / 60 * hourHeight,
				Margin = new Thickness(dDiff * dayWidth * spaceBetweenDays + roomID * dayWidth / appRoomCount, (d.TimeOfDay.TotalHours - dayStartTime + 1) * hourHeight, 0, 0),
				Fill = GetColourForRect(data),
				Stroke = Brushes.Black,
				StrokeThickness = 1,
				Tag = data,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Name = name
			};

			bool doesClash;
			bool isDataValid;

			if (isExpensive)
			{
				// If you have the time/computation to calculate if the appointment clashes more accurately, do so.
				string[] prevSel = editingSidebar.GetData();
				editingSidebar.ChangeSelectedData(data);
				isDataValid = editingSidebar.IsValid(out _);
				editingSidebar.ChangeSelectedData(prevSel);

				doesClash = DBMethods.MiscRequests.DoesAppointmentClash(data, BookingParts, out _);
			}
			else
			{
				isDataValid = true;

				// Otherwise, only check if it clashes with staff shifts.
				// This increases the performance of changing the selected date or similar, but at the cost of some accuracy.

				doesClash = !DBMethods.MiscRequests.IsAppInShift(dDiff, data[3], d.TimeOfDay, d.TimeOfDay.Add(TimeSpan.FromMinutes(appLength)), d.Date);
			}

			// If the appointment clashes, highlight it to the user.
			if (doesClash || !isDataValid)
			{
				newRect.StrokeThickness = 4;
				newRect.Stroke = Brushes.White;
			}

			if (currentlySelected is not null && currentlySelected.Tag == data)
			{
				newRect.Stroke = Brushes.AliceBlue;
				newRect.StrokeThickness = 2;
				currentlySelected = newRect;

			}

			if (dataToBeSelected is not null)
			{
				if (currentlySelected is null || (GetDataTag(currentlySelected)[0] != dataToBeSelected[0]))
				{
					if (dataToBeSelected[0] == data[0])
					{
						editingSidebar.ChangeSelectedData((string[])newRect.Tag);
						currentlySelected = newRect;
						newRect.Stroke = Brushes.AliceBlue;
						newRect.StrokeThickness = 2;
					}
				}
			}

			if (currentlySelected is not null && data[0] == GetDataTag(currentlySelected)[0])
			{
				if (mouseDown) return (Rectangle)currentlySelected;

				currentlySelected = newRect;
				newRect.Stroke = Brushes.AliceBlue;
				newRect.StrokeThickness = 2;
			}

			newRect.MouseDown += Rectangle_MouseDown;
			newRect.MouseUp += RctRect_MouseUp;
			grdResults.Children.Add(newRect);
			return newRect;
		}

		/// <summary>
		/// Turn a DayOfWeek into an integer
		/// </summary>
		private static int DayOfWeekToInt(DayOfWeek day)
		{
			return day switch
			{
				DayOfWeek.Monday => 0,
				DayOfWeek.Tuesday => 1,
				DayOfWeek.Wednesday => 2,
				DayOfWeek.Thursday => 3,
				DayOfWeek.Friday => 4,
				DayOfWeek.Saturday => 5,
				DayOfWeek.Sunday => 6,
				_ => throw new NotImplementedException(),
			};
		}

		private void LblEdit_MouseDown(object sender, MouseButtonEventArgs e)
		{
			SwitchToEditing();
		}

		private void SwitchToEditing()
		{
			SelectLbl(lblEditBtn);
			DeselectLbl(lblBookBtn);

			grdEditingSidebar.Visibility = Visibility.Visible;
			grdBookingSidebar.Visibility = Visibility.Collapsed;
		}

		private void LblBook_MouseDown(object sender, MouseButtonEventArgs e)
		{
			SelectLbl(lblBookBtn);
			DeselectLbl(lblEditBtn);

			grdEditingSidebar.Visibility = Visibility.Collapsed;
			grdBookingSidebar.Visibility = Visibility.Visible;
		}

		private static void SelectLbl(Label l)
		{
			l.Width = 420;
			l.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
			l.Foreground = new SolidColorBrush(Color.FromRgb(241, 241, 241));
		}

		private static void DeselectLbl(Label l)
		{
			l.Width = 220;
			l.Background = new SolidColorBrush(Color.FromRgb(37, 37, 37));
			l.Foreground = new SolidColorBrush(Color.FromRgb(213, 213, 213));
		}

		private void LblAddBookingPart_MouseDown(object sender, MouseButtonEventArgs e)
		{
			BookingCreator b = new BookingCreator(this, GetNewBookingID(), GetBookingDogID(), GetBookingStaffID());
			stpBookingManager.Children.Add(b);
			BookingParts.Add(b);
		}

		internal void DeleteBookingPart(BookingCreator bookingCreator)
		{
			if (currentlySelected is not null && currentlySelected.Tag == bookingCreator)
			{
				editingSidebar.EmptySidebar();
			}

			BookingParts.Remove(bookingCreator);
			stpBookingManager.Children.Remove(bookingCreator);
			List<Rectangle> rcts = grdResults.Children.OfType<Rectangle>().Where(r => r.Tag == bookingCreator).ToList();
			foreach (Rectangle r in rcts)
			{
				grdResults.Children.Remove(r);
			}
			bookingCreator = null;
			currentlySelected = null;
		}

		internal void StartBookAppt(Rectangle sender)
		{
			hasMoved = true;
			grd.Children.Add(sender);
			SelectNewRect(sender);
			diffMouseAndElem = new Point(currentlySelected.Width / 2, currentlySelected.Height / 2);

			//currentlySelected = sender;
			mouseDown = true;
			grdResults.MouseEnter += GrdResults_MouseEnter;
			currentlySelected.IsHitTestVisible = false;
		}

		private void GrdResults_MouseEnter(object sender, MouseEventArgs e)
		{
			Rectangle r = (Rectangle)currentlySelected;

			grdResults.MouseEnter -= GrdResults_MouseEnter;
			r.IsHitTestVisible = true;
			r.MouseDown += Rectangle_MouseDown;
			grd.Children.Remove(r);
			grdResults.Children.Add(r);
			((BookingCreator)r.Tag).IsAdded = true;
			SwitchToEditing();
		}

		private async void BtnConfirmBooking_Click(object sender, RoutedEventArgs e)
		{
			string[] bookingColumns = DBObjects.DB.Tables.Where(t => t.Name == "Booking").First().Columns.Select(x => x.Name).ToArray();
			string[] bookingData = new string[] { GetNewBookingID(), DateTime.Now.Date.ToString("yyyy-MM-dd") };
			DBMethods.DBAccess.UpdateTable("Booking", bookingColumns, bookingData, true);

			bool isAnyInvalid = false;
			string totalErrMessage = "";

			foreach (BookingCreator booking in BookingParts)
			{
				if (booking.IsAdded)
				{
					List<string[]> bkData = booking.GetData();
					foreach (string[] bk in bkData)
					{
						bool doesInstClash = DBMethods.MiscRequests.DoesAppointmentClash(bk, BookingParts, out string errMessage);
						isAnyInvalid = doesInstClash || isAnyInvalid;

						string[] prevSel = editingSidebar.GetData();
						editingSidebar.ChangeSelectedData(bk);
						bool isInstDataValid = editingSidebar.IsValid(out string err2Message);
						editingSidebar.ChangeSelectedData(prevSel);
						isAnyInvalid = !isInstDataValid || isAnyInvalid;

						if (doesInstClash || !isInstDataValid) totalErrMessage += $"Appointment {bk[0]}:\n";

						if (doesInstClash) totalErrMessage += errMessage + "\n";
						if (!isInstDataValid) totalErrMessage += err2Message[1..] + "\n";
					}
				}
			}

			if (isAnyInvalid)
			{
				lblBookingErr.Visibility = Visibility.Visible;
				lblBookingErr.Content = totalErrMessage;
				return;
			}
			else lblBookingErr.Visibility = Visibility.Collapsed;

			foreach (BookingCreator booking in BookingParts)
			{
				if (booking.IsAdded)
				{
					List<string[]> bkData = booking.GetData();
					foreach (string[] bk in bkData)
					{
						DBMethods.DBAccess.UpdateTable("Appointment", columns.Select(c => c.Name).ToArray(), bk, true);
						Rectangle rect = grdResults.Children.OfType<Rectangle>().Where(r => r.Name is not null && r.Name != "" && r.Tag == booking && bkData[Convert.ToInt32(r.Name[1..])] == bk).FirstOrDefault();

						if (rect is not null) rect.Tag = bk;
					}
				}
			}

			lblNewBookingID.Content = $"Booking ID: {DBMethods.MiscRequests.GetMinKeyNotUsed("Booking", "Booking ID")}";
			stpBookingManager.Children.Clear();
			BookingParts = new List<BookingCreator>();

			btnConfirmBooking.Content = "Booking Made!";
			await Task.Delay(2000);
			btnConfirmBooking.Content = "Confirm Booking";
		}

		public string GetNewBookingID()
		{
			return lblNewBookingID.Content.ToString()[11..];
		}

		public string GetBookingDogID()
		{
			return stpBookDogID.Children.OfType<ValidatedItem>().First().Text;
		}

		public string GetBookingStaffID()
		{
			int selIndex = stpBookStaffID.Children.OfType<ComboBox>().First().SelectedIndex;
			return selIndex.ToString();
		}

		public DateTime GetSelDate()
		{
			return datePicker.SelectedDate.Value;
		}

		public void RemoveRectsWithTag(object o)
		{
			Rectangle[] rects = grdResults.Children.OfType<Rectangle>().Where(r => r.Tag == o).ToArray();
			foreach (Rectangle r in rects) grdResults.Children.Remove(r);
		}

		public void RepBookingChanged(BookingCreator sender)
		{
			RemoveRectsWithTag(sender);
			
			List<string[]> appData = sender.GetData();
			for (int i = 0; i < appData.Count; i++)
			{
				Rectangle r = GenRectFromData(appData[i], "r" + i.ToString(), true);
				r.Tag = sender;
			}
		}

		public Color GetCurrentStaffColour()
		{
			return colours[stpBookStaffID.Children.OfType<ComboBox>().First().SelectedIndex];
		}

		private void CmbBkStaff_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (BookingCreator b in BookingParts)
			{
				if (!b.IsAdded)
				{
					b.ChangeRectColour(GetCurrentStaffColour());
				}
			}
		}
	}
}
