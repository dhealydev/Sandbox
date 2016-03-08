using System;
using System.Linq;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Client;
using System.Configuration;
using DAL;

namespace ExactTargetIntegration
{
    class Program
    {
        private static readonly CrmConnection _connection = CrmConnection.Parse(ConfigurationManager.AppSettings["CrmConnectionString"]);
        internal static readonly OrganizationService _service = new OrganizationService(_connection);
        private static readonly  Logging Log = new Logging();
        static void Main(string[] args)
        {
            try
            {
                foreach (var committee in Committee.AllCommittees())
                {
                    var committeeIntegration = new CommitteeIntegration(committee.Name);

                    try
                    {
                        committeeIntegration.Run();
                    }
                    catch (Exception exception)
                    {
                        //Log a general application exception
                        Log.LogMessage(exception.Message, exception.StackTrace,
                                        String.Format("Error Running Committee:{0}", committee.Name));
                    }
                }
            }
            catch (Exception exception)
            {
               Log.LogMessage(exception.Message, exception.StackTrace, "Application Error");
            }
        }
    }

    static class exts
    {
        public static bool IsOneOf<T>(this T src, params T[] args)
        {
            return args.Contains(src);
        }
    }
}