﻿using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace A2_Project
{
	/// <summary>
	/// Interaction logic for RegisterNewStaffWindow.xaml
	/// </summary>
	public partial class RegisterNewStaffWindow : Window
	{
		private readonly DBAccess dbAccess;
		// TODO: Should probably store staff phone no.
		public RegisterNewStaffWindow(DBAccess _dbAccess)
		{
			InitializeComponent();
			dbAccess = _dbAccess;
		}

		/// <summary>
		/// Checks if the passwords are valid while registering a new account, and returns all issues found.
		/// </summary>
		private static string IsValidPass(string pswOne, string pswTwo)
		{
			string issues = "";
			if (pswOne == "") issues += "You must enter a password. ";
			if (pswOne == "") issues += "You must re-enter your password. ";
			if (issues.Length > 0) return issues;
			if (pswOne != pswTwo) issues += "Your have incorrectly re-entered your password. ";
			int countCaps = 0, countNums = 0, countSymb = 0;
			foreach (char c in pswOne)
			{
				if (Char.IsUpper(c)) countCaps++;
				else if (Char.IsNumber(c)) countNums++;
				else if (Char.IsPunctuation(c) || Char.IsSymbol(c)) countSymb++;
			}
			if (countCaps < 1) issues += "Your password must contain at least 1 capital letter. ";
			if (countNums < 1) issues += "Your password must contain at least 1 number. ";
			if (countSymb < 1) issues += "Your password must contain at least 1 symbol. ";
			if (pswOne.Length < 7) issues += "Your password must be at least 8 characters long.";
			return issues;
		}

		private static bool IsValidEmail(string email)
		{
			string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
			Regex regex = new Regex(pattern);
			return regex.IsMatch(email);
		}

		private bool IsPhoneNoValid(string phoneNo)
		{
			string regexStr = @"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$";
			Regex regex = new Regex(regexStr);
			return regex.IsMatch(phoneNo);
		}

		private void BtnRegister_Click(object sender, RoutedEventArgs e)
		{
			tblOutput.Foreground = new SolidColorBrush(Color.FromRgb(182, 24, 39));
			if (txtName.Text != "")
			{
				try
				{
					string ispassValid = IsValidPass(pswPassword.Password, pswRePassword.Password);
					if (ispassValid == "")
					{
						if (txtEmail.Text == "" || IsValidEmail(txtEmail.Text))
						{
							if (txtPhoneNo.Text == "" || IsPhoneNoValid(txtPhoneNo.Text))
							{
								if (true/*TODO: Decide if name should be unique, and if so, check the name is not taken before creating the account*/)
								{
									// TODO: Create the account
									tblOutput.Text = "Account created!";
									tblOutput.Foreground = new SolidColorBrush(Color.FromRgb(241, 241, 241));
									//txtName.Text = "";
									//txtEmail.Text = "";
									//txtPhoneNo.Text = "";
									//pswPassword.Password = "";
									//pswRePassword.Password = "";
								}
								else tblOutput.Text = "Username already taken!";
							}
							else tblOutput.Text = "Invalid phone number!";
						}
						else tblOutput.Text = "Invalid email address!";
					}
					else 
					{
						tblOutput.Text = ispassValid;
					}
				}
				catch (Exception ex)
				{
					tblOutput.Text = ex.Message;
				}
			}
			else tblOutput.Text = "Please enter a name!";
		}

		private void TxtBox_KeyDown(object sender, KeyEventArgs e)
		{
			// Update issues after keydown
		}
	}
}
