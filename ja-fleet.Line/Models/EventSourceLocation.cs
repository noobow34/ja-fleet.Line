using Microsoft.WindowsAzure.Storage.Table;

namespace jafleet.Line.Models
{
    public class EventSourceLocation : EventSourceState
    {
        public string Location { get; set; }

        public EventSourceLocation() { }
    }
}