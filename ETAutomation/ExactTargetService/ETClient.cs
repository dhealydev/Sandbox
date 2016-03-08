using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using ExactTargetService.ExactTargetClient;
using System.Xml;
using DAL;
using System.Configuration;


namespace ExactTargetService
{
    public class ETClient
    {
        private string USERNAME = ConfigurationManager.AppSettings["Username"]; //"GNYHAAdmin";
        private string PASSWORD = ConfigurationManager.AppSettings["Password"]; //"em@ilw0rkfl0w";
        private string KEY = ConfigurationManager.AppSettings["Key"]; //"c74ff5e0-65f7-48aa-9f9b-3187759ea4e3";
        
        private SoapClient _client;
        private Logging _logging = new Logging();

        public ETClient()
        { 
            _client = GetETClient();
        }

        public T Get<T>(FilterPart filter, string[] properties) where T : APIObject
        {
            RetrieveRequest request = new RetrieveRequest();
            request.ObjectType = typeof(T).Name;
            request.Properties = properties;
            request.Filter = filter;

            var obs = GetObjects(request);
            if (obs.Length > 0)
                return (T)obs[0];
            else
                return null;
        }

        public IEnumerable<T> GetMany<T>(FilterPart filter, string[] properties) where T : APIObject
        {
            RetrieveRequest request = new RetrieveRequest();
            request.ObjectType = typeof(T).Name;
            request.Properties = properties;
            request.Filter = filter;

            List<T> rv = new List<T>();
            foreach (var result in GetObjects(request))
            {
                rv.Add((T)result);
            }

            return rv;
        }

        //public IEnumerable<T> GetAll<T>(string[] properties) where T : APIObject
        //{
        //    RetrieveRequest request = new RetrieveRequest();
        //    request.ObjectType = typeof(T).Name;
        //    request.Properties = properties;

        //    foreach (var result in GetObjectsSoapBinding(request))
        //    {
        //        yield return (T)result;
        //    }
        //}

        public CreateResult[] Save<T>(T record, SaveAction action) where T : APIObject
        {
            CreateOptions co = new CreateOptions();
            co.SaveOptions = new SaveOption[1];
            co.SaveOptions[0] = new SaveOption();
            co.SaveOptions[0].SaveAction = action;
            co.SaveOptions[0].PropertyName = "*";
            string emailAddress = " ";

            Type t = record.GetType();
            if (t.Name == "Subscriber")
            {
                PropertyInfo propertyInfo = t.GetProperty("EmailAddress");
                 emailAddress = propertyInfo.GetValue(record, null).ToString();
            }
            string cRequestID;
            string overallStatus;
            var results = _client.Create(co, new APIObject[] { record }, out cRequestID, out overallStatus);

            if (!overallStatus.Equals("OK"))
            {
                if (results.Length > 0)
                {
                    if (results[0].StatusMessage == "InvalidEmailAddress")
                    {
                        //Debug.WriteLine("Invalid Email");
                        //Log.Message("Invalid Email {0}", emailAddress);
                        _logging.LogMessage(String.Format("Invalid Email {0}", emailAddress), "Inside Save<T> Method", "Email rejected by the ET system.");
                    }
                    else
                    if (results[0].StatusMessage == "EmailAddressAlreadyExists")
                    {
                        //Debug.WriteLine("Email Address Already Exists");
                        //Log.Message("Email Address Already Exists {0}", emailAddress);
                        _logging.LogMessage(String.Format("Email Address Already Exists {0}", emailAddress), "Inside Save<T> Method", "Email already exists in ET system");
                    }
                    else if (results[0].StatusMessage == "OnListAlready")
                    {
                        //Debug.WriteLine("On List Already");
                        //Log.Message("On List Already", emailAddress);
                        _logging.LogMessage(String.Format("Email Address Already on list {0}", emailAddress), "Inside Save<T> Method", "Email already on list in ET system");
                    }
                    else if (results[0].StatusMessage == "TriggeredSpamFilter")
                    {
                        //Debug.WriteLine("Triggered Spam Filter");
                        //Log.Message("Triggered Spam Filter {0}", emailAddress);
                        _logging.LogMessage(String.Format("Triggered Spam Filter {0}", emailAddress), "Inside Save<T> Method", "Email blocked by ET system");
                    }
                    else
                    {
                        if (results.Length > 0 && results[0].StatusMessage != "OnGlobalUnsubList")
                        {
                            _logging.LogMessage(String.Format("On Global Unsub List {0}", emailAddress),
                                                "Inside Save<T> Method", "Email is on ET system Global Usubscribe List");
                        }
                    }
                }

            }

            if (action.IsOneOf(SaveAction.AddOnly, SaveAction.UpdateAdd))
            {
                if (results.Length == 1)
                {
                    var result = results[0];
                    record.ID = result.NewID;
                }
            }

            return results;
        }

        /// <summary>
        /// Update record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="record"></param>
        /// <returns></returns>
        public UpdateResult[] Update<T>(T record) where T : APIObject
        {
            UpdateOptions options = new UpdateOptions();
            options.Action = "update";
            string cRequestID;
            string overallStatus;
            string emailAddress = " ";

            Type t = record.GetType();
            if (t.Name == "Subscriber")
            {
                PropertyInfo propertyInfo = t.GetProperty("EmailAddress");
                emailAddress = propertyInfo.GetValue(record, null).ToString();
            }

            var results = _client.Update(options, new APIObject[] { record }, out cRequestID, out overallStatus);

            if (!overallStatus.Equals("OnGlobalUnsubList") && !overallStatus.Equals("OK"))
            {
                _logging.LogMessage(String.Format("On Global Unsub List {0}", emailAddress),
                                                 "Inside Update<T> Method", "Email is on ET system Global Usubscribe List");
            }

            return results;
        }

        public DeleteResult[] Delete<T>(T record) where T : APIObject
        {
            DeleteOptions options = new DeleteOptions();
            string cRequestID;
            string overallStatus;
            
            var results = _client.Delete(options, new APIObject[] { record }, out cRequestID, out overallStatus);

            if (!overallStatus.Equals("OK"))
            {
                throw new Exception("Not OK!");
            }

            return results;
        }

        public ObjectDefinition GetDefinition<T>()
        {
            string requestId;
            ObjectDefinitionRequest request = new ObjectDefinitionRequest();
            request.ObjectType = typeof(T).Name;
            var defs = _client.Describe(new ObjectDefinitionRequest[] { request }, out requestId);

            return defs[0];
        }
        
        private APIObject[] GetObjects(RetrieveRequest request)
        {
            APIObject[] results;
            string requestId;

            var response = _client.Retrieve(request, out requestId, out results);

            if (response.Equals("OK") || response.Equals("MoreDataAvailable"))
            {
                return results;
            }
            else
            {
                Debug.WriteLine(response);
                throw new Exception("Error");
            }
        }

        private SoapClient GetETClient()
        {
           // Create the binding
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Name = "UserNameSoapBinding";
            binding.CloseTimeout = TimeSpan.FromMinutes(5);
            binding.OpenTimeout = TimeSpan.FromMinutes(5);
            binding.ReceiveTimeout = TimeSpan.FromMinutes(5);
            binding.SendTimeout = TimeSpan.FromMinutes(5);

            //security
            binding.Security.Mode = BasicHttpSecurityMode.TransportWithMessageCredential;
            binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

            //Maxes
            binding.MaxReceivedMessageSize = 2147483647;
            binding.MaxBufferSize = 2147483647;
            binding.MaxBufferPoolSize = 2147483647;
            
            //readerquotas
            XmlDictionaryReaderQuotas readerQuotas = new XmlDictionaryReaderQuotas();
            readerQuotas.MaxArrayLength = 2147483647;
            readerQuotas.MaxStringContentLength = 2147483647;
            binding.ReaderQuotas = readerQuotas;

            string endPointAddress = ConfigurationManager.AppSettings["EndPoint"];
          
            EndpointAddress endpoint = new EndpointAddress(endPointAddress);

            SoapClient etFramework = new SoapClient(binding, endpoint);

            etFramework.ClientCredentials.UserName.UserName = USERNAME;
            etFramework.ClientCredentials.UserName.Password = PASSWORD;

            return etFramework;
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
