namespace MassTransit.Scheduling
{
    using System;


    public class CancelScheduledRecurringMessageCommand :
        CancelScheduledRecurringMessage
    {
        public CancelScheduledRecurringMessageCommand()
        {
        }

        public CancelScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup)
        {
            CorrelationId = NewId.NextGuid();
            Timestamp = DateTime.UtcNow;

            ScheduleId = scheduleId;
            ScheduleGroup = scheduleGroup;
        }

        public Guid CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ScheduleId { get; set; }
        public string ScheduleGroup { get; set; }
    }
}
