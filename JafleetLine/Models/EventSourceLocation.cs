using Microsoft.WindowsAzure.Storage.Table;

namespace jafleetline.Models
{
    public class EventSourceLocation : EventSourceState
    {
        public string Location { get; set; }

        public EventSourceLocation() { }
    }
}