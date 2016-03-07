using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExactTargetService.ExactTargetClient;
using System.Runtime.Serialization;

namespace ExactTargetIntegration
{
    public class ExactTargetContact
    {
        const string _EMAILADDRESS = "Email Address",
                     _FACILITY = "Facility",
                     _TITLE = "Title",
                     _FIRSTNAME = "First Name",
                     _LASTNAME = "Last Name",
                     _STATE = "State",
                     _NYC = "NYC";

        public ExactTargetContact(Subscriber subscriber)
        {
            var gnyhaAttributes = subscriber.Attributes;
            if (CheckValidGNYHAAttributes(gnyhaAttributes))
            {
                Id = subscriber.ID;
                SubscriberKey = subscriber.SubscriberKey;
                EmailAddress = gnyhaAttributes.Single(x => x.Name == _EMAILADDRESS).Value;
                Facility = gnyhaAttributes.Single(x => x.Name == _FACILITY).Value;
                Title = gnyhaAttributes.Single(x => x.Name == _TITLE).Value;
                FirstName = gnyhaAttributes.Single(x => x.Name == _FIRSTNAME).Value;
                LastName = gnyhaAttributes.Single(x => x.Name == _LASTNAME).Value;
                State = gnyhaAttributes.Single(x => x.Name == _STATE).Value;
                NYC = gnyhaAttributes.Single(x => x.Name == _NYC).Value;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public ExactTargetContact() { }

        public int Id { get; set; }
        public string SubscriberKey { get; set; }
        public string EmailAddress { get; set; }
        public string Facility { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string State { get; set; }
        public string NYC { get; set; }

        public static bool CheckValidGNYHAAttributes(ExactTargetService.ExactTargetClient.Attribute[] gnyhaAttributes)
        {
            return    gnyhaAttributes.Any(x => x.Name == _EMAILADDRESS)
                   && gnyhaAttributes.Any(x => x.Name == _FACILITY)
                   && gnyhaAttributes.Any(x => x.Name == _TITLE)
                   && gnyhaAttributes.Any(x => x.Name == _FIRSTNAME)
                   && gnyhaAttributes.Any(x => x.Name == _LASTNAME)
                   && gnyhaAttributes.Any(x => x.Name == _STATE)
                   && gnyhaAttributes.Any(x => x.Name == _NYC);
        }
    }
}
