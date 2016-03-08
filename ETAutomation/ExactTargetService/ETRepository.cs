using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ExactTargetService.ExactTargetClient;
//using ExactTargetIntegration;

namespace ExactTargetService
{
    public class ETRepository
    {
        private ETClient _client = new ETClient();
        private static readonly string[] _defaultColumns = new string[] { "ID", "CreatedDate", "Client.ID", "EmailAddress", "PartnerKey", "SubscriberKey", "UnsubscribedDate", "Status", "EmailTypePreference" };
        //private static string[] _defaultSubscriberColumns = new string[] { "ID", "SubscriberKey" };

        public Subscriber GetSubscriberBySLXID(string slxId)
        {
            SimpleFilterPart sfp = new SimpleFilterPart();
            sfp.Property = "SubscrierKey";
            sfp.Value = new string[] { slxId };
            sfp.SimpleOperator = SimpleOperators.equals;

            return _client.Get<Subscriber>(sfp, _defaultColumns);
        }

        public Subscriber GetSubscriberByEmail(string email)
        {
            SimpleFilterPart sfp = new SimpleFilterPart();
            sfp.Property = "EmailAddress";
            sfp.Value = new string[] { email };

            return _client.Get<Subscriber>(sfp, _defaultColumns);
        }

        public bool Save<T>(T entity) where T : APIObject
        {
            try
            {
                var results = _client.Save<T>(entity, SaveAction.UpdateAdd);
                Debug.WriteLine(results.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public ExactTargetClient.List GetList(string name)
        {
            SimpleFilterPart sfp = new SimpleFilterPart();
            sfp.Property = "ListName";
            sfp.SimpleOperator = SimpleOperators.equals;
            sfp.Value = new string[] { name };

            var client = new ETClient();
            return client.Get<List>(sfp, new string[] { "ObjectID", "ListName", "ID" });
        }

        public ExactTargetClient.List GetList(int listID)
        {
            SimpleFilterPart sfp = new SimpleFilterPart();
            sfp.Property = "ID";
            sfp.SimpleOperator = SimpleOperators.equals;
            sfp.Value = new string[] { listID.ToString() };

            var client = new ETClient();
            return client.Get<List>(sfp, new string[] { "ObjectID", "ListName", "ID" });
        }

        public IEnumerable<ExactTargetClient.List> GetAllLists()
        {
            var client = new ETClient();
            return client.GetMany<List>(null, new string[] { "ObjectID", "ListName", "ID" });
        }

        /// <summary>
        /// Adds a subscriber to a list
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="list"></param>
        public void AddSubscriberToList(string email, ExactTargetClient.List list)
        {
            Subscriber sub = new Subscriber();
            sub.EmailAddress = email;

            SubscriberList sl = new SubscriberList();
            sl.ID = list.ID;
            sl.IDSpecified = true;
            sl.Status = SubscriberStatus.Active;
            sl.StatusSpecified = true;

            //sl.Action = "create";
            sub.Lists = new SubscriberList[] { sl };

            _client.Save(sub, SaveAction.UpdateAdd);
        }

        /// <summary>
        /// Removes a contact from a list
        /// </summary>
        /// <param name="email"></param>
        /// <param name="list"></param>
        public void RemoveFromList(string email, ExactTargetClient.List list)
        {
            Subscriber sub = new Subscriber();
            sub.EmailAddress = email;
            // Define the SubscriberList and set the status to Deleted
            SubscriberList subList = new SubscriberList();
            subList.ID = list.ID;
            subList.IDSpecified = true;
            subList.Status = SubscriberStatus.Deleted; 
            subList.StatusSpecified = true;

            subList.Action = "update";
            //Relate the SubscriberList defined to the Subscriber
            sub.Lists = new SubscriberList[] { subList };
            _client.Update(sub);
        }

        public IEnumerable<ListSubscriber> GetSubscribersOnList(ExactTargetClient.List list)
        {
            SimpleFilterPart filter = new SimpleFilterPart();
            filter.Property = "ListID";
            filter.SimpleOperator = SimpleOperators.equals;
            filter.Value = new string[] { list.ID.ToString() };

            return _client.GetMany<ListSubscriber>(filter, new string[] { "SubscriberKey", "Status" });
        }

    }
}
