using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using ExactTargetService.ExactTargetClient;

namespace ExactTargetIntegration
{
    public class DynamicsExactTargetMap
    {
        internal static Subscriber ContactToSubscriber(CommitteeContact contact)
        {
            var etSubscriber = new Subscriber();
            
            etSubscriber.SubscriberKey = Guid.NewGuid().ToString();
            if (contact.Email.Contains(';'))
            {
                contact.Email = contact.Email.Remove(contact.Email.IndexOf(';'));
            }
            etSubscriber.EmailAddress = contact.Email;
            etSubscriber.PartnerKey = contact.ContactId.ToString();

            // Attributes
            etSubscriber.Attributes = new ExactTargetService.ExactTargetClient.Attribute[6];
            etSubscriber.Attributes[0] = new ExactTargetService.ExactTargetClient.Attribute() { Name = "First Name", Value = contact.FirstName};
            etSubscriber.Attributes[1] = new ExactTargetService.ExactTargetClient.Attribute() { Name = "Last Name", Value = contact.LastName };
            etSubscriber.Attributes[2] = new ExactTargetService.ExactTargetClient.Attribute() { Name = "Title", Value = contact.Title };
            etSubscriber.Attributes[3] = new ExactTargetService.ExactTargetClient.Attribute() { Name = "Facility", Value = contact.Account };
            etSubscriber.Attributes[4] = new ExactTargetService.ExactTargetClient.Attribute() { Name = "State", Value = contact.State };
            etSubscriber.Attributes[5] = new ExactTargetService.ExactTargetClient.Attribute() { Name = "NYC", Value = contact.Nyc.ToString() };

            // Properties
            etSubscriber.PartnerProperties = new APIProperty[6];
            etSubscriber.PartnerProperties[0] = new ExactTargetService.ExactTargetClient.APIProperty() { Name = "Full Name", Value = contact.FirstName};
            etSubscriber.PartnerProperties[1] = new ExactTargetService.ExactTargetClient.APIProperty() { Name = "Last Name", Value = contact.LastName};
            etSubscriber.PartnerProperties[2] = new ExactTargetService.ExactTargetClient.APIProperty() { Name = "Title", Value = contact.Title };
            etSubscriber.PartnerProperties[3] = new ExactTargetService.ExactTargetClient.APIProperty() { Name = "Facility", Value = contact.Account };
            etSubscriber.PartnerProperties[4] = new ExactTargetService.ExactTargetClient.APIProperty() { Name = "State", Value = contact.State };
            etSubscriber.PartnerProperties[5] = new ExactTargetService.ExactTargetClient.APIProperty() { Name = "Nyc", Value = contact.Nyc.ToString() };

            // Email Type Perference
            etSubscriber.EmailTypePreference = EmailType.HTML;
            etSubscriber.EmailTypePreferenceSpecified = true;
            
            return etSubscriber;
        }

        internal static List CommitteeToList(CommitteeContact committeeName)
        {
            var etList = new List();

            // The committee id
            etList.ObjectID = Guid.NewGuid().ToString();
            etList.PartnerKey = committeeName.PluginId.ToString();
            if (committeeName.Name.Length > 50)//ET List Names are a max of 50 Chars or an error will be thrown.
            {
                etList.ListName = committeeName.Name.Substring(0, 49);
            }
            else etList.ListName = committeeName.Name;
            
            return etList;
        }

    }
}
