using System;
using System.Collections.Generic;
using System.Linq;
using ExactTargetService;
using ExactTargetService.ExactTargetClient;
using System.Diagnostics;
using DAL;
using Models;

namespace ExactTargetIntegration
{
    public class CommitteeIntegration
    {
        public string CommitteeName { get; private set; }
        private IEnumerable<CommitteeContact> _contacts;
        private readonly Committee _committee = null;
        private readonly ETRepository _repo = new ETRepository();
        private readonly Logging _log = new Logging();

        public CommitteeIntegration(string committeeName)
        {
            CommitteeName = committeeName;
            _committee = Committee.AllCommittees().Single(x => x.Name == CommitteeName);
            _contacts = _committee.GetContacts();

            CleanUpAlwaysCCEmails();

        }

        /// <summary>
        /// Run the integration
        /// </summary>
        public void Run()
        {
            try
            {
                //Get existing ET list or create new  ET list if needed
                List list = GetExactTargetList(CommitteeName);

                //if the list was just created, the below list will be empty, but that is ok.
                var subscribersOnList = _repo.GetSubscribersOnList(list).ToList();

                //housekeeping
                _committee.UpdateCommittetoETListMap(CommitteeName, _committee.ListId, list.ListName, list.ID);
                _log.SaveETListInfo(list.ListName, list.ID, subscribersOnList.Count);
                MapContactToList(_contacts, list, subscribersOnList);
               // UnMapRemovedContacts(_contacts, list, subscribersOnList);
                
            }
            catch (Exception exception)
            {
                _log.LogMessage("General Error Running Application - CommitteeName: " + CommitteeName + exception.Message, exception.StackTrace, "None");
            }
        }

        private  List GetExactTargetList(string committeeName)
        {
            List list = null;
            if (_contacts.FirstOrDefault() != null)
            {
                //try to get the listid by using the mapping table.
                int ETListID = _committee.GetETListIdByCommitteeID();
                if (ETListID != 0) //found
                {
                    list = _repo.GetList(ETListID);
                    if (list != null)
                    {
                        _log.LogMessage(String.Format("List Found by ID, ETList Name: {0}, ET_ID:{1} ", list.ListName, list.ID), "None", "None");
                    }
                    else
                    {
                        _log.LogMessage(String.Format("Mapping Exists but list not found in ET. List may have been deleted in ET.  , CommitteeName: {0}, MapETListID:{1} ", CommitteeName, ETListID), "None", "Remove Entry from mapping table for deleted ET list.");
                    }
                }
                //The List was not found in the mapping table by ID, try to find it in ET by List Name -avoid duplicates
                else
                {
                    list = _repo.GetList(committeeName);
                    if (list != null)
                    {
                        _log.LogMessage(
                            String.Format("List found in ET but no correct mapping exists. Creating Mapping: {0}",
                                          CommitteeName), "None", "None");
                        _committee.AddNewCommitteeToMap(committeeName, _committee.ListId);
                    }
                }
                if (list == null) //not found in mapping table or found in mapping table but deleted in ET.  Either way we need a new list in ET.
                {
                    _log.LogMessage(String.Format("List not found in ET and no correct mapping exists. Creating List: {0}", CommitteeName), "None", "None");
                    _committee.AddNewCommitteeToMap(committeeName, _committee.ListId);
                    list = CreateList(_contacts.First());
                }
            }
            else _log.LogMessage(String.Format("Error Running List: {0}", CommitteeName), "None", "None");

            return list;
        }

        private void MapContactToList(IEnumerable<CommitteeContact> contacts, List list, List<ListSubscriber> subscribersOnList)
        {
            foreach (CommitteeContact contact in contacts)
            {
                Console.Clear();
                Console.WriteLine("Mapping Contact: {0} to list: {1}", contact.FirstName + " " + contact.LastName,
                                  contact.Name);
                ////TODO Add the emails in the semicolon separated list as if they were all subscribers to the distribution list.
                //if (contact.Email.Contains(';'))
                //{
                //    //TODO Rather than discarding the extra emails, split up the string and add each email to a list then map them as well. Do the same for unmapping.

                //    AlwaysCCEmails = contact.Email.Split(';').Select(p => p.Trim()).ToList();
                //    contact.Email = contact.Email.Remove(contact.Email.IndexOf(';'));
                //}
                try
                {
                    //get subscriber from the list not from the repo
                    var subOnList =
                        subscribersOnList.FirstOrDefault(
                            x => x.SubscriberKey.Trim().ToLower() == contact.Email.Trim().ToLower());

                    if (subOnList == null) //This email has not been subscribed to this list before, add them.
                    {
                        _log.LogETSubscriberChange(list.ListName, list.ID, "Subscriber Not Found", contact.Email,
                                                   "Add Subscriber to List", _committee.ListId, _committee.Name);
                        _repo.AddSubscriberToList(contact.Email.Trim().ToLower(), list);
                        subscribersOnList.Add(new ListSubscriber() { SubscriberKey = contact.Email.Trim().ToLower() });
                        //why do we do this?
                    }
                    //This email is on the list but status is Unsubscribed. No Re-sub as per Jordan.    
                    else if (subOnList.Status == SubscriberStatus.Unsubscribed)
                    {
                        _log.LogETSubscriberChange(list.ListName, list.ID, subOnList.Status.ToString(), contact.Email,
                                                   "Subscriber is in dynamics committee but unsubscribed in ET. We do not re-subscribe, ever.",
                                                   _committee.ListId, _committee.Name);
                    }
                }
                catch (TimeoutException timeoutException)
                {
                    _log.LogMessage(timeoutException.Message, timeoutException.StackTrace,
                                    string.Format("Timeout Error while mapping Contact: {0}, to List: {1}",
                                                  contact.Email, list.ListName, timeoutException.Message));
                }
                catch (Exception ex)
                {
                    _log.LogMessage(ex.Message, ex.StackTrace,
                                    String.Format("Error while mapping Contact: {0}, to List: {1}", contact.Email,
                                                  list.ListName));
                }
            }

            var contactList = contacts.ToList();
            //foreach (var contact in contactList)
            //{
            //    if (contact.Email.Contains(';'))
            //    {
            //        contact.Email = contact.Email.Remove(contact.Email.IndexOf(';'));
            //    }
            //}
            foreach (var subscriber in subscribersOnList)
            {
                //if the ET Subscriber is no longer on the dynamics list (still on the ET list - obviously)
                if (!contactList.Exists(x => x.Email.Trim().ToLower() == subscriber.SubscriberKey.Trim().ToLower()))
                {
                    //if the ET subscriber status is not Unsubscribed (we do not touch unsubscribes as per Jordan.)
                    if (subscriber.Status.ToString() != "Unsubscribed")
                    {
                        _log.LogETSubscriberChange(list.ListName, list.ID, subscriber.Status.ToString(), subscriber.SubscriberKey, "Update ET Status to Deleted (Removes email from list)", _committee.ListId, _committee.Name);
                        _repo.RemoveFromList(subscriber.SubscriberKey, list);
                    }
                    else if (subscriber.Status.ToString() == "Unsubscribed")
                    {
                        _log.LogETSubscriberChange(list.ListName, list.ID, subscriber.Status.ToString(), subscriber.SubscriberKey, "ET Subscriber with status of Unsubscribed no longer in Committee List.  No change is made.", _committee.ListId, _committee.Name);
                    }
                }
            }
        }


        /// <summary>
        /// Removes contacts from ETList
        /// </summary>
        /// <param name="contacts"></param>
        private void UnMapRemovedContacts(IEnumerable<CommitteeContact> contacts, ExactTargetService.ExactTargetClient.List list, IEnumerable<ListSubscriber> subscribersOnList)
        {
            var contactList = contacts.ToList();
            //foreach (var contact in contactList)
            //{
            //    if (contact.Email.Contains(';'))
            //    {
            //        contact.Email = contact.Email.Remove(contact.Email.IndexOf(';'));
            //    }
            //}
            //foreach ET Subscriber On ET List
            foreach (var subscriber in subscribersOnList)
            {
                //if the ET Subscriber is no longer on the dynamics list (still on the ET list - obviously)
                if (!contactList.Exists(x => x.Email.Trim().ToLower() == subscriber.SubscriberKey.Trim().ToLower()))
                {
                    //if the ET subscriber status is not Unsubscribed (we do not touch unsubscribes as per Jordan.)
                    if (subscriber.Status.ToString() != "Unsubscribed")
                    {
                        _log.LogETSubscriberChange(list.ListName, list.ID, subscriber.Status.ToString(), subscriber.SubscriberKey, "Update ET Status to Deleted (Removes email from list)", _committee.ListId, _committee.Name);
                        _repo.RemoveFromList(subscriber.SubscriberKey, list);
                    }
                    else if (subscriber.Status.ToString() == "Unsubscribed")
                    {
                        _log.LogETSubscriberChange(list.ListName, list.ID, subscriber.Status.ToString(), subscriber.SubscriberKey, "ET Subscriber with status of Unsubscribed no longer in Committee List.  No change is made.", _committee.ListId, _committee.Name);
                    }
                }
            }
        }

        private List CreateList(CommitteeContact committee)
        {
            var list = DynamicsExactTargetMap.CommitteeToList(committee);

            if (_repo.Save(list))
            {
                return list;
            }
            else
            {
                throw new Exception("Error Creating List");
            }
        }

        private void CleanUpAlwaysCCEmails()
        {
            List<CommitteeContact> tempContacts = _contacts.ToList();
            List<string> AlwaysCCEmails = new List<string>();
            foreach (CommitteeContact contact in _contacts)
            {
                if (contact.Email.Contains(';'))
                {
                   
                    AlwaysCCEmails = contact.Email.Split(';').Select(p => p.Trim()).ToList();
                    contact.Email = contact.Email.Remove(contact.Email.IndexOf(';'));
                }

                foreach (string email in AlwaysCCEmails)
                {
                    //some of the alwayscc emails are duplicates we only need them once.
                    if( email.Trim().Length > 0 )
                    if (!tempContacts.Exists(x => x.Email.Trim().ToLower() == email.Trim().ToLower()))
                    {
                        tempContacts.Add(new CommitteeContact{ Email = email.Trim() });
                    }
                }
            }
            _contacts = tempContacts;
        }
    }
}
