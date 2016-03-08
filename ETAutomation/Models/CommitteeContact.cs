using System;

namespace Models
{
    public class CommitteeContact
    {
        public Guid ContactId { get; set; }
        public Guid PluginId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Account { get; set; }
        public string State { get; set; }
        public bool Nyc { get; set; }
    }
}
