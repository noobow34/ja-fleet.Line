using Microsoft.WindowsAzure.Storage.Table;

namespace NoobowNotifier.Models
{
    public class EventSourceLocation : EventSourceState
    {
        public string Location { get; set; }

        public EventSourceLocation() { }
    }
}