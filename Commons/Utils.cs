//------------------------------------------ START OF LICENSE -----------------------------------------
//Azure Usage and Billing Insights
//
//Copyright(c) Microsoft Corporation
//
//All rights reserved.
//
//MIT License
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//associated documentation files (the ""Software""), to deal in the Software without restriction, 
//including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial 
//portions of the Software.
//
//THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
//BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------- END OF LICENSE ------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration; // access to configuration files
using System.Data;
using System.Data.SqlClient;

namespace Commons
{
	public static class Utils
	{
		private static readonly string SqlConnectionString = ConfigurationManager.ConnectionStrings["ASQLConn"]?.ConnectionString;

		public static void UpdateSubscriptionStatus(Guid id, DataGenStatus dgs, DateTime dt)
		{
			SqlConnection connection = new SqlConnection(SqlConnectionString);
			SqlCommand sqlCommand = new SqlCommand("set nocount off; update dbo.Subscriptions set DataGenStatus = @DataGenStatus, DataGenDate = @DataGenDate where Id = @Id", connection);
			sqlCommand.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
			sqlCommand.Parameters.Add("@DataGenDate", SqlDbType.DateTime2).Value = dt;
			sqlCommand.Parameters.Add("@DataGenStatus", SqlDbType.Int).Value = (int)dgs;

			try {
				connection.Open();
				int count = sqlCommand.ExecuteNonQuery();
				if (count != 1) Console.WriteLine($"Subscriptions record {id} not found.");
			} catch (Exception e) {
				Console.WriteLine($"Exception ({nameof(UpdateSubscriptionStatus)}): {e.Message}");
				throw;
			} finally {
				sqlCommand.Dispose();
				connection.Dispose();
			}
		}

		public static List<Subscription> GetSubscriptions()
		{
			List<Subscription> subscriptionList = new List<Subscription>();

			// To prevent DB Connection count problem in other parallel threads with nested loops below, fetch all records (assumed to be small amount) locally.
			SqlConnection connection = new SqlConnection(SqlConnectionString);
			SqlCommand sqlCommand = new SqlCommand("SELECT * FROM dbo.Subscriptions", connection);

			try {
				connection.Open();
				SqlDataReader reader = sqlCommand.ExecuteReader();

				while (reader.Read()) {
					subscriptionList.Add(new Subscription {
						Id = (Guid)reader["Id"],
						DisplayName = (string)reader["DisplayName"],
						OrganizationId = (Guid)reader["OrganizationId"],
						IsConnected = (bool)reader["IsConnected"],
						ConnectedOn = (DateTime)reader["ConnectedOn"],
						ConnectedBy = (string)reader["ConnectedBy"],
						AzureAccessNeedsToBeRepaired = (bool)reader["AzureAccessNeedsToBeRepaired"],
						DisplayTag = (string)reader["DisplayTag"],
						DataGenStatus = (DataGenStatus)((int)reader["DataGenStatus"]),
						DataGenDate = (DateTime)reader["DataGenDate"]
					});
				}
			} catch (Exception e) {
				Console.WriteLine("Exception (GetSubscriptions): " + e.Message);
			} finally {
				sqlCommand.Dispose();
				connection.Dispose();
			}

			return subscriptionList;
		}
	}
}
