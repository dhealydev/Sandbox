using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Data.SqlClient;
using System.Configuration;

namespace DAL
{
	public class Logging
	{
		public int SaveETListInfo(string listName, int id, int numSubscribers)
		{
			using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DynamicsExtension"].ConnectionString))
			{
				return
					conn.Execute(
						@"INSERT INTO [dbo].[ETAutomation_ETListInfo]  ([ETListName] ,[ETListID] ,[NumSubscribers] )
		   VALUES 
		   ( @ListName
			 ,@ID
			 ,@NumSubscribers
		   )", new { ListName = listName, ID = id, NumSubscribers = numSubscribers }
						);
			}
		}

		public int SaveETSubscriberInfo(string listName, int id, string status, string email)
		{
			using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DynamicsExtension"].ConnectionString))
			{
				return
					conn.Execute(
					   @"INSERT INTO [dbo].[ETAutomation_ETListsWithSubscribers]
		   ([ETListID]
		   ,[ETListName]
		   ,[ETSubscriberStatus]
		   ,[SubscriberKey])
	 VALUES
		   (@ETListID
		   ,@ETListName
		   ,@ETSubscriberStatus
		   ,@SubscriberKey)", new { ETListID = id, ETListName = listName, ETSubscriberStatus = status, SubscriberKey = email }
						);
			}
		}

		public int LogETSubscriberChange(string listName, int id, string status, string email, string action, Guid committeeID, string committeeName)
		{
			using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DynamicsExtension"].ConnectionString))
			{
				return
					conn.Execute(
					   @"INSERT INTO [dbo].[ETAutomation_ChangeLog]
		   ([ETListID]
		   ,[ETListName]
		   ,[ETSubscriberKey]
		   ,[ETSubscriberStatus]
		   ,[Action]
		   ,[CommitteeID]
		   ,[CommitteeName]
		   ,[CreatedDateTime])
	 VALUES
		   (@ETListID
		   ,@ETListName
		   ,@ETSubscriberKey
		   ,@ETSubscriberStatus
		   ,@Action
		   ,@CommitteeID
		   ,@CommitteeName
		   ,@CreatedDateTime)", new { ETListID = id, ETListName = listName, ETSubscriberStatus = status, ETSubscriberKey = email, Action = action, CommitteeID = committeeID, CommitteeName = committeeName, CreatedDateTime = DateTime.Now }
						);
			}
		}

		public int LogMessage(string message, string stackTrace, string availableInfo)
		{
			using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DynamicsExtension"].ConnectionString))
			{
				return
					conn.Execute(
					   @"INSERT INTO [dbo].[ETAutomation_AppLog]
		   (
			[Message]
		   ,[StackTrace]
		   ,[AvailableInfo]
		   ,[CreatedDateTime])
	 VALUES
		   (@Message
		   ,@StackTrace
		   ,@AvailableInfo
		   ,@CreatedDateTime)", new { Message = message, StackTrace = stackTrace, AvailableInfo = availableInfo, CreatedDateTime = DateTime.Now }
						);
			}
		}
	}
}
