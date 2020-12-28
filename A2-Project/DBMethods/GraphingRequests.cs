﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A2_Project.DBMethods
{
	public static class GraphingRequests
	{
		private static readonly string[] months = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

		public static void GetCountOfAppointmentTypes(ref int[][] data, ref string[] headers, DateTime minDate)
		{
			data[0] = DBAccess.GetStringsWithQuery($"SELECT Count([Appointment Type ID]) FROM [Appointment] WHERE [Appointment Date] BETWEEN '{minDate:yyyy-MM-dd}' AND '{DateTime.Now:yyyy-MM-dd}' GROUP BY [Appointment Type ID];").Select(int.Parse).ToArray();
			headers = DBAccess.GetStringsWithQuery("SELECT [Description] FROM [Appointment Type] ORDER BY [Appointment Type ID];").ToArray();
		}

		public static void GetBusinessOfStaff(ref int[][] data, ref string[] headers, DateTime minDate)
		{
			data[0] = DBAccess.GetStringsWithQuery($"SELECT Count([Staff ID]) FROM [Appointment] WHERE [Appointment Date] BETWEEN '{minDate:yyyy-MM-dd}' AND '{DateTime.Now:yyyy-MM-dd}' GROUP BY [Staff ID] ORDER BY [Staff ID];").Select(int.Parse).ToArray();
			headers = DBAccess.GetStringsWithQuery("SELECT [Staff Name] FROM [Staff] ORDER BY [Staff ID];").ToArray();
		}

		/// <summary>
		/// Gets the number of clients over time
		/// </summary>
		public static void GetGrowthOverTime(ref int[][] data, ref string[] headers, DateTime minDate)
		{
			DateTime startDate = MaxDate(Convert.ToDateTime(DBAccess.GetStringsWithQuery("SELECT MIN([Client Join Date]) FROM [Client]")[0]), minDate);
			DateTime endDate = Convert.ToDateTime(DBAccess.GetStringsWithQuery("SELECT MAX([Client Join Date]) FROM [Client]")[0]);
			int diff = (int)(endDate - startDate).TotalDays;
			List<int> growth = new List<int>();
			for (double i = 0; i < diff; i += diff / 75.0)
			{
				growth.Add(Convert.ToInt32(DBAccess.GetStringsWithQuery("SELECT COUNT([Client ID]) FROM [Client] WHERE [Client Join Date] <= '" + startDate.AddDays(i).ToString("yyyy-MM-dd") + "';")[0]));
			}
			data[0] = growth.ToArray();
			headers = InterpolateDates(startDate, diff);
		}

		/// <summary>
		/// Gets the number of appointments on each day of the week
		/// </summary>
		public static void GetAppsByDayOfWeek(ref int[][] data, ref string[] headers, DateTime minDate)
		{
			data[0] = new int[7];
			for (int i = 0; i < data[0].Length; i++)
				data[0][i] = Convert.ToInt32(DBAccess.GetStringsWithQuery("SET DATEFIRST 1; SELECT COUNT([Appointment ID]) FROM [Appointment] WHERE DATEPART(WEEKDAY, [Appointment Date]) = " + (i + 1) + $" AND [Appointment Date] BETWEEN '{minDate:yyyy-MM-dd}' AND '{DateTime.Now:yyyy-MM-dd}';")[0]);
			headers = new string[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
		}

		/// <summary>
		/// Gets the number of appointments in each month of the year
		/// </summary>
		public static void GetBookingsInMonths(ref int[][] data, ref string[] headers, DateTime minDate)
		{
			data[0] = new int[12];
			for (int i = 0; i < data[0].Length; i++)
			{
				data[0][i] = Convert.ToInt32(DBAccess.GetStringsWithQuery("SELECT COUNT([Appointment ID]) FROM [Appointment] WHERE DATEPART(MONTH, [Appointment Date]) = " + (i + 1) + $" AND [Appointment Date] BETWEEN '{minDate:yyyy-MM-dd}' AND '{DateTime.Now:yyyy-MM-dd}';")[0]);
			}
			headers = months;
		}

		/// <summary>
		/// Gets a rolling average of what % of appointments have been cancelled over time
		/// </summary>
		public static void GetAppCancelRate(ref int[][] data, ref string[] headers, DateTime minDate)
		{
			DateTime startDate = MaxDate(Convert.ToDateTime(DBAccess.GetStringsWithQuery("SELECT MIN([Appointment Date]) FROM Appointment")[0]), minDate);
			DateTime endDate = DateTime.Now;
			int diff = (int)(endDate - startDate).TotalDays;
			List<int> growth = new List<int>();
			for (double i = 0; i < diff; i += diff / 75.0)
			{
				int totalInTime = Convert.ToInt32(DBAccess.GetStringsWithQuery("SELECT COUNT([Appointment ID]) FROM [Appointment] WHERE [Appointment Date] <= '" + startDate.AddDays(i).ToString("yyyy-MM-dd") + "' AND [Appointment Date] > '" + startDate.AddDays(i - diff / 10.0).ToString("yyyy-MM-dd") + "';")[0]);
				int cancelledInTime = Convert.ToInt32(DBAccess.GetStringsWithQuery("SELECT COUNT([Appointment ID]) FROM [Appointment] WHERE [Is Cancelled] = 1 AND [Appointment Date] <= '" + startDate.AddDays(i).ToString("yyyy-MM-dd") + "' AND [Appointment Date] > '" + startDate.AddDays(i - diff / 10.0).ToString("yyyy-MM-dd") + "';")[0]);
				if (cancelledInTime == 0 || totalInTime == 0)
					growth.Add(0);
				else growth.Add((int)((float)cancelledInTime * 100 / totalInTime));
			}
			data[0] = growth.ToArray();
			headers = InterpolateDates(startDate, diff);
		}

		// TODO: Currently classifies clients that have > 1 dogs and booked them all in one go as repeat customers, even if they never book after that.
		public static void GetCustReturns(ref int[][] data, ref string[] headers, DateTime minDate)
		{
			DateTime startDate = MaxDate(Convert.ToDateTime(DBAccess.GetStringsWithQuery("SELECT MIN([Client Join Date]) FROM [Client]")[0]), minDate);
			DateTime endDate = DateTime.Now;
			int diff = (int)(endDate - startDate).TotalDays;
			List<int> growth = new List<int>();
			for (double i = 0; i < diff; i += diff / 36.0)
			{
				string query = "SELECT COUNT([Appointment].[Appointment ID]) FROM [Client] " +
				"INNER JOIN [Dog] ON [Dog].[Client ID] = [Client].[Client ID] INNER JOIN [Appointment] ON [Appointment].[Dog ID] = [Dog].[Dog ID] " +
				"WHERE [Client].[Client Join Date] < '" + startDate.AddDays(i).ToString("yyyy-MM-dd") + "' AND [Appointment].[Is Initial Appointment] = 1 " +
				"AND [Client].[Client Join Date] > '" + startDate.AddDays(i - diff / 10.0).ToString("yyyy-MM-dd") + "';";
				List<string> result = DBAccess.GetStringsWithQuery(query);
				growth.Add(Convert.ToInt32(result[0]));
			}
			data[0] = growth.ToArray();
			headers = InterpolateDates(startDate, diff);
		}

		// TODO: BookedInAdvancedDiscount should be its own table?
		public static void GetIncomeLastYear(ref int[][] data, ref string[] headers, DateTime minDate)
		{
			headers = new string[12];
			DateTime endDate = DateTime.Now.Date;
			DateTime startDate = endDate.AddMonths(-12);
			List<List<List<string>>> growth = new List<List<List<string>>>();
			for (int i = 0; i < 12; i++)
			{
				headers[i] = months[startDate.AddMonths(i).Month - 1];
				string query = "SELECT [Appointment Type ID], [Includes Nail And Teeth], [Booked In Advance Discount] FROM [Appointment] WHERE [Is Paid] = 1 AND [Appointment Date] BETWEEN '" + startDate.AddMonths(i).ToString("yyyy-MM-dd") + "' AND '" + startDate.AddMonths(i + 1).ToString("yyyy-MM-dd") + "';";
				growth.Add(DBAccess.GetListStringsWithQuery(query));
			}
			List<List<string>> appTypeData = DBAccess.GetListStringsWithQuery("SELECT * FROM [Appointment Type]");
			List<double> income = new List<double>();
			for (int i = 0; i < growth.Count; i++)
			{
				income.Add(0);
				List<List<string>> lls = growth[i];
				foreach (List<string> ls in lls)
				{
					int incomeFromApp = Convert.ToInt32(ls[0]);
					double t = Convert.ToDouble(appTypeData[incomeFromApp][2]);
					if (ls[1] == "1") t += 5;
					// TODO: Use actual costs of appointments
					income[i] += t - 38;
				}
			}
			data[0] = income.Select(x => (int)Math.Round(x)).ToArray();
		}

		/// <summary>
		/// Returns 7 dates linearly between the startDate and end date.
		/// diff represents the difference between the start date and the end date in days
		/// </summary>
		private static string[] InterpolateDates(DateTime startDate, int diff)
		{
			List<string> dates = new List<string>();
			for (double i = 0; i <= diff; i += (double)diff / 6)
			{
				dates.Add(startDate.AddDays(i).ToString("dd/MM/yyyy"));
			}
			return dates.ToArray();
		}

		private static DateTime MaxDate(DateTime d1, DateTime d2)
		{
			if (d1 > d2) return d1;
			else return d2;
		}
	}
}
